import { api } from '@/lib/api'
import type {
  JobApplication,
  CreateApplicationInput,
  ApplicationStatus,
} from '@/types/application'

export const applicationService = {
  /**
   * Check whether a specific user has already submitted an application for a given job via .NET backend API.
   */
  async checkUserApplied(jobId: string, userId: string): Promise<boolean> {
    if (!jobId || !userId) return false
    try {
      const response = await api.get<{ applied: boolean }>('applications/check', {
        jobId,
        userId,
      })
      return response.applied
    } catch (error: any) {
      console.error('Error checking if user applied via API:', error.message)
      return false
    }
  },

  /**
   * Fetch a user's existing application for a job if one exists via .NET backend API.
   */
  async fetchUserApplication(
    jobId: string,
    userId: string
  ): Promise<JobApplication | null> {
    if (!jobId || !userId) return null
    try {
      return await api.get<JobApplication>('applications/user', { jobId, userId })
    } catch (error: any) {
      console.error('Error fetching user application via API:', error.message)
      return null
    }
  },

  /**
   * Submit an application for a job via .NET backend API.
   */
  async applyForJob(input: CreateApplicationInput): Promise<JobApplication> {
    return await api.post<JobApplication>('applications', {
      job_id: input.job_id,
      user_id: input.user_id,
      cover_letter: input.cover_letter?.trim() || null,
      resume_id: input.resume_id || null,
      custom_answers: input.custom_answers || {},
      status: input.status || 'applied',
    })
  },

  /**
   * Fetch all applications for a specific job (for OrgAdmin and OrgStaff) via .NET backend API.
   */
  async fetchApplicationsForJob(jobId: string): Promise<JobApplication[]> {
    if (!jobId) return []
    try {
      return await api.get<JobApplication[]>(`applications/job/${jobId}`)
    } catch (error: any) {
      console.error('Error fetching applications via API:', error.message)
      return []
    }
  },

  /**
   * Update the status of an application via .NET backend API.
   */
  async updateApplicationStatus(
    applicationId: string,
    status: ApplicationStatus
  ): Promise<void> {
    await api.put<{ success: boolean }>(`applications/${applicationId}/status`, {
      status,
    })
  },
}
