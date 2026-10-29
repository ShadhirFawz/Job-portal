import { supabase } from '@/lib/supabase'
import type { AppUser, ProfileUpdateInput } from '@/types/user'

export const profileService = {
  /**
   * Fetch user profile from public.users table.
   */
  async getUserProfile(userId: string): Promise<AppUser | null> {
    const { data, error } = await supabase
      .from('users')
      .select('*')
      .eq('id', userId)
      .maybeSingle()

    if (error) {
      console.error('Error fetching user profile:', error)
      return null
    }

    return data as AppUser
  },

  /**
   * Upsert or update a user profile.
   */
  async updateUserProfile(userId: string, input: ProfileUpdateInput): Promise<AppUser> {
    const { data, error } = await supabase
      .from('users')
      .update(input)
      .eq('id', userId)
      .select()
      .single()

    if (error) throw error
    return data as AppUser
  },
}

