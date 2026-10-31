import { supabase } from '@/lib/supabase'
import type { Resume, CreateResumeInput, UpdateResumeInput } from '@/types/resume'
import { getResumeSignedUrl, deleteResumeFile } from '@/composables/useResumeUpload'

export const resumeService = {
  /**
   * Fetch all active (non-deleted) resumes for a user.
   */
  async fetchUserResumes(userId: string): Promise<Resume[]> {
    if (!userId) return []

    const { data, error } = await supabase
      .from('resumes')
      .select('*')
      .eq('user_id', userId)
      .is('deleted_at', null)
      .order('is_primary', { ascending: false })
      .order('created_at', { ascending: false })

    if (error) {
      console.error('Error fetching resumes:', error.message)
      throw error
    }

    return (data || []) as Resume[]
  },

  /**
   * Fetch single resume by ID.
   */
  async fetchResumeById(resumeId: string): Promise<Resume | null> {
    if (!resumeId) return null

    const { data, error } = await supabase
      .from('resumes')
      .select('*')
      .eq('id', resumeId)
      .maybeSingle()

    if (error) {
      console.error('Error fetching resume by id:', error.message)
      return null
    }

    return (data as Resume) || null
  },

  /**
   * Insert a new resume record.
   */
  async createResume(input: CreateResumeInput): Promise<Resume> {
    const { data, error } = await supabase
      .from('resumes')
      .insert({
        user_id: input.user_id,
        title: input.title.trim(),
        file_path: input.file_path,
        file_name: input.file_name,
        file_size: input.file_size,
        file_type: input.file_type,
        is_primary: input.is_primary ?? false,
      })
      .select()
      .single()

    if (error) {
      console.error('Error creating resume:', error.message)
      throw error
    }

    return data as Resume
  },

  /**
   * Update resume fields (e.g. title, summary, skills).
   */
  async updateResume(resumeId: string, input: UpdateResumeInput): Promise<Resume> {
    const { data, error } = await supabase
      .from('resumes')
      .update({
        ...input,
        updated_at: new Date().toISOString(),
      })
      .eq('id', resumeId)
      .select()
      .single()

    if (error) {
      console.error('Error updating resume:', error.message)
      throw error
    }

    return data as Resume
  },

  /**
   * Mark a resume as the user's primary resume.
   * Note: The database trigger `trigger_single_primary_resume` resets other resumes.
   */
  async setPrimaryResume(resumeId: string): Promise<void> {
    const { error } = await supabase
      .from('resumes')
      .update({ is_primary: true })
      .eq('id', resumeId)

    if (error) {
      console.error('Error setting primary resume:', error.message)
      throw error
    }
  },

  /**
   * Soft-delete a resume and remove its underlying file from storage.
   */
  async deleteResume(resume: Pick<Resume, 'id' | 'file_path'>): Promise<void> {
    // 1. Soft delete database record
    const { error } = await supabase
      .from('resumes')
      .update({
        deleted_at: new Date().toISOString(),
        is_primary: false,
      })
      .eq('id', resume.id)

    if (error) {
      console.error('Error deleting resume:', error.message)
      throw error
    }

    // 2. Remove file from storage if present
    if (resume.file_path) {
      await deleteResumeFile(resume.file_path)
    }
  },

  /**
   * Obtain a download / preview signed URL for a stored resume.
   */
  async getDownloadUrl(filePath: string, expiresIn = 3600): Promise<string | null> {
    return getResumeSignedUrl(filePath, expiresIn)
  },
}

