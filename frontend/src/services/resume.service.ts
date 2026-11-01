import { api } from '@/lib/api'
import type { Resume, CreateResumeInput, UpdateResumeInput } from '@/types/resume'

export const resumeService = {
  /**
   * Fetch all active (non-deleted) resumes for a user via .NET backend API.
   */
  async fetchUserResumes(userId: string): Promise<Resume[]> {
    if (!userId) return []
    try {
      return await api.get<Resume[]>(`resumes/user/${userId}`)
    } catch (error: any) {
      console.error('Error fetching resumes via API:', error.message)
      throw error
    }
  },

  /**
   * Fetch single resume by ID via .NET backend API.
   */
  async fetchResumeById(resumeId: string): Promise<Resume | null> {
    if (!resumeId) return null
    try {
      return await api.get<Resume>(`resumes/${resumeId}`)
    } catch (error: any) {
      console.error('Error fetching resume by id via API:', error.message)
      return null
    }
  },

  /**
   * Insert a new resume record via .NET backend API.
   */
  async createResume(input: CreateResumeInput): Promise<Resume> {
    return await api.post<Resume>('resumes', {
      user_id: input.user_id,
      title: input.title.trim(),
      file_path: input.file_path,
      file_name: input.file_name,
      file_size: input.file_size,
      file_type: input.file_type,
      is_primary: input.is_primary ?? false,
    })
  },

  /**
   * Update resume fields via .NET backend API.
   */
  async updateResume(resumeId: string, input: UpdateResumeInput): Promise<Resume> {
    return await api.put<Resume>(`resumes/${resumeId}`, input)
  },

  /**
   * Mark a resume as the user's primary resume via .NET backend API.
   */
  async setPrimaryResume(resumeId: string): Promise<void> {
    await api.put<{ success: boolean }>(`resumes/${resumeId}/primary`)
  },

  /**
   * Soft-delete a resume and remove file via .NET backend API.
   */
  async deleteResume(resume: Pick<Resume, 'id' | 'file_path'>): Promise<void> {
    await api.delete<{ success: boolean }>(`resumes/${resume.id}`)
  },

  /**
   * Obtain a download / preview signed URL via .NET backend API.
   */
  async getDownloadUrl(filePath: string, expiresIn = 3600): Promise<string | null> {
    try {
      const response = await api.get<{ signedUrl: string }>('resumes/download-url', {
        filePath,
        expiresIn,
      })
      return response.signedUrl || null
    } catch (error) {
      console.error('Error getting download URL via API:', error)
      return null
    }
  },
}
