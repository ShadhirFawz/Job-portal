import { api } from '@/lib/api'
import { supabase } from '@/lib/supabase'
import type { AppUser, ProfileUpdateInput, UsernameCheckResult } from '@/types/user'
import type { Resume } from '@/types/resume'

const RESERVED_USERNAMES = new Set([
  'admin', 'administrator', 'api', 'auth', 'login', 'register', 'logout',
  'profile', 'profiles', 'user', 'users', 'organization', 'organizations',
  'job', 'jobs', 'resume', 'resumes', 'assessment', 'assessments', 'invitation', 'invitations',
  'setting', 'settings', 'support', 'help', 'about', 'terms', 'privacy', 'dashboard', 'home', 'u'
])

export const profileService = {
  /**
   * Fetch user profile by ID via .NET API with Supabase fallback.
   */
  async getUserProfile(userId: string): Promise<AppUser | null> {
    if (!userId) return null
    try {
      return await api.get<AppUser>(`profiles/${userId}`)
    } catch (apiErr) {
      console.warn('API getUserProfile failed, falling back to Supabase:', apiErr)
    }

    try {
      const { data, error } = await supabase
        .from('users')
        .select('*')
        .eq('id', userId)
        .maybeSingle()

      if (error) {
        console.error('Supabase getUserProfile error:', error)
        return null
      }
      return data as AppUser | null
    } catch (err) {
      console.error('Failed to fetch user profile:', err)
      return null
    }
  },

  /**
   * Fetch user profile by public username via .NET API with Supabase fallback.
   */
  async getUserProfileByUsername(username: string): Promise<AppUser | null> {
    if (!username) return null
    const clean = username.trim().toLowerCase()
    try {
      return await api.get<AppUser>(`profiles/username/${clean}`)
    } catch (apiErr) {
      console.warn('API getUserProfileByUsername failed, falling back to Supabase:', apiErr)
    }

    try {
      const { data, error } = await supabase
        .from('users')
        .select('*')
        .ilike('username', clean)
        .maybeSingle()

      if (error) {
        console.error('Supabase getUserProfileByUsername error:', error)
        return null
      }
      return data as AppUser | null
    } catch (err) {
      console.error('Failed to fetch profile by username:', err)
      return null
    }
  },

  /**
   * Real-time live username availability check with client validation and API/Supabase lookup.
   */
  async checkUsernameAvailability(username: string, currentUserId?: string): Promise<UsernameCheckResult> {
    if (!username || username.trim().length === 0) {
      return { username: '', is_available: false, message: 'Username is required.' }
    }

    const clean = username.trim().toLowerCase()

    if (clean.length < 3) {
      return { username: clean, is_available: false, message: 'Username must be at least 3 characters.' }
    }

    if (clean.length > 30) {
      return { username: clean, is_available: false, message: 'Username cannot exceed 30 characters.' }
    }

    if (!/^[a-z0-9_]+$/.test(clean)) {
      return { username: clean, is_available: false, message: 'Only letters, numbers, and underscores are allowed.' }
    }

    if (RESERVED_USERNAMES.has(clean)) {
      return { username: clean, is_available: false, message: 'This username is reserved and cannot be used.' }
    }

    // Try API first
    try {
      const result = await api.get<UsernameCheckResult>('profiles/check-username', {
        username: clean,
        currentUserId: currentUserId || undefined
      })
      if (result) return result
    } catch (apiErr) {
      console.warn('API check-username failed, using Supabase fallback:', apiErr)
    }

    // Fallback direct Supabase check
    try {
      const { data, error } = await supabase
        .from('users')
        .select('id, username')
        .ilike('username', clean)
        .maybeSingle()

      if (error && error.code !== 'PGRST116') {
        console.error('Supabase username check error:', error)
      }

      if (data) {
        if (currentUserId && data.id === currentUserId) {
          return { username: clean, is_available: true, message: 'This is your current username.' }
        }
        return { username: clean, is_available: false, message: 'Username is already taken.' }
      }

      return { username: clean, is_available: true, message: 'Username is available!' }
    } catch (err) {
      console.error('Error checking username availability:', err)
      return { username: clean, is_available: true, message: 'Username appears available.' }
    }
  },

  /**
   * Update user profile via .NET API with Supabase fallback.
   */
  async updateUserProfile(userId: string, input: ProfileUpdateInput): Promise<AppUser> {
    try {
      return await api.put<AppUser>(`profiles/${userId}`, input)
    } catch (apiErr) {
      console.warn('API updateUserProfile failed, updating via Supabase:', apiErr)
    }

    const payload: Record<string, any> = {
      ...input,
      updated_at: new Date().toISOString()
    }
    if (payload.username) {
      payload.username = String(payload.username).trim().toLowerCase()
    }

    const { data, error } = await supabase
      .from('users')
      .update(payload)
      .eq('id', userId)
      .select('*')
      .single()

    if (error) throw error
    return data as AppUser
  },

  /**
   * Fetch the primary resume for a user.
   */
  async getPrimaryResume(userId: string): Promise<Resume | null> {
    if (!userId) return null
    try {
      // 1. Try finding explicitly marked primary resume
      const { data: primaryData, error: primaryErr } = await supabase
        .from('resumes')
        .select('*')
        .eq('user_id', userId)
        .eq('is_primary', true)
        .is('deleted_at', null)
        .order('created_at', { ascending: false })
        .limit(1)
        .maybeSingle()

      if (!primaryErr && primaryData) {
        return primaryData as Resume
      }

      // 2. If no resume is flagged as primary, fall back to the most recently uploaded resume
      const { data: latestData, error: latestErr } = await supabase
        .from('resumes')
        .select('*')
        .eq('user_id', userId)
        .is('deleted_at', null)
        .order('created_at', { ascending: false })
        .limit(1)
        .maybeSingle()

      if (!latestErr && latestData) {
        return latestData as Resume
      }

      return null
    } catch (err) {
      console.error('Failed to fetch primary resume:', err)
      return null
    }
  },

  /**
   * Create a signed URL for resume preview / download.
   */
  async getResumeDownloadUrl(filePath: string): Promise<string | null> {
    if (!filePath) return null
    try {
      const { data, error } = await supabase.storage
        .from('resumes')
        .createSignedUrl(filePath, 3600)

      if (error) {
        console.error('Storage createSignedUrl error:', error)
        return null
      }
      return data?.signedUrl || null
    } catch (err) {
      console.error('Failed to create resume signed URL:', err)
      return null
    }
  }
}
