import { api } from '@/lib/api'
import type { Job, JobFilterParams, PagedJobsResponse } from '@/types/job'

export const jobService = {
  /**
   * Fetch published jobs with server-side filtering and pagination via .NET backend API.
   */
  async fetchPublishedJobs(params: JobFilterParams = {}): Promise<PagedJobsResponse> {
    try {
      const response = await api.get<PagedJobsResponse>('jobs', {
        q: params.q?.trim() || undefined,
        type: params.type || undefined,
        mode: params.mode || undefined,
        level: params.level || undefined,
        location: params.location?.trim() || undefined,
        department: params.department?.trim() || undefined,
        page: params.page && params.page > 0 ? params.page : 1,
        pageSize: params.pageSize && params.pageSize > 0 ? params.pageSize : 12,
      })

      return {
        items: response.items || [],
        totalCount: response.totalCount ?? 0,
        page: response.page ?? 1,
        pageSize: response.pageSize ?? 12,
        totalPages: response.totalPages ?? 0,
      }
    } catch (error) {
      console.error('Error fetching jobs via API:', error)
      return {
        items: [],
        totalCount: 0,
        page: 1,
        pageSize: 12,
        totalPages: 0,
      }
    }
  },

  /**
   * Fetch a single job by ID via .NET backend API.
   */
  async fetchJobById(id: string, publicOnly = false): Promise<Job | null> {
    if (!id) return null
    try {
      return await api.get<Job>(`jobs/${id}`, { publicOnly })
    } catch (error) {
      console.error('Error fetching job by ID via API:', error)
      return null
    }
  },

  /**
   * Fetch all jobs for a specific organization via .NET backend API.
   */
  async fetchOrganizationJobs(organizationId: string): Promise<Job[]> {
    if (!organizationId) return []
    try {
      return await api.get<Job[]>(`jobs/organization/${organizationId}`)
    } catch (error) {
      console.error('Error fetching organization jobs via API:', error)
      return []
    }
  },

  /**
   * Update an existing job posting (only allowed by the job post creator/owner).
   */
  async updateJob(id: string, userId: string, payload: UpdateJobPayload): Promise<Job> {
    return await api.put<Job>(`jobs/${id}`, payload, { userId })
  },

  /**
   * Publish a draft job posting (only allowed by the job post creator/owner).
   */
  async publishJob(id: string, userId: string): Promise<Job> {
    return await api.post<Job>(`jobs/${id}/publish`, {}, { userId })
  },
}

