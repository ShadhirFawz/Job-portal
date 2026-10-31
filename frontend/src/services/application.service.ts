import { supabase } from '@/lib/supabase'
import type {
  JobApplication,
  CreateApplicationInput,
  ApplicationStatus,
} from '@/types/application'

export const applicationService = {
  /**
   * Check whether a specific user has already submitted an application for a given job.
   */
  async checkUserApplied(jobId: string, userId: string): Promise<boolean> {
    if (!jobId || !userId) return false

    const { data, error } = await supabase
      .from('job_applications')
      .select('id')
      .eq('job_id', jobId)
      .eq('user_id', userId)
      .maybeSingle()

    if (error) {
      console.error('Error checking if user applied:', error.message)
      return false
    }

    return !!data
  },

  /**
   * Fetch a user's existing application for a job if one exists.
   */
  async fetchUserApplication(
    jobId: string,
    userId: string
  ): Promise<JobApplication | null> {
    if (!jobId || !userId) return null

    const { data, error } = await supabase
      .from('job_applications')
      .select('*')
      .eq('job_id', jobId)
      .eq('user_id', userId)
      .maybeSingle()

    if (error) {
      console.error('Error fetching user application:', error.message)
      return null
    }

    return (data as JobApplication) || null
  },

  /**
   * Submit an application for a job.
   * Inserts into `job_applications` and executes the `increment_job_applications_count` RPC.
   */
  async applyForJob(input: CreateApplicationInput): Promise<JobApplication> {
    // 1. Insert application record
    const { data, error } = await supabase
      .from('job_applications')
      .insert({
        job_id: input.job_id,
        user_id: input.user_id,
        cover_letter: input.cover_letter?.trim() || null,
        custom_answers: input.custom_answers || {},
        status: input.status || 'applied',
      })
      .select()
      .single()

    if (error) {
      if (error.code === '23505') {
        throw new Error('You have already applied to this job.')
      }
      throw error
    }

    // 2. Increment job applications count
    try {
      const { error: rpcError } = await supabase.rpc(
        'increment_job_applications_count',
        { job_id: input.job_id }
      )

      if (rpcError) {
        console.warn('increment_job_applications_count RPC failed, falling back to manual increment:', rpcError.message)
        const { data: job } = await supabase
          .from('jobs')
          .select('applications_count')
          .eq('id', input.job_id)
          .single()

        if (job) {
          await supabase
            .from('jobs')
            .update({ applications_count: (job.applications_count || 0) + 1 })
            .eq('id', input.job_id)
        }
      }
    } catch (countErr) {
      console.warn('Could not increment applications count:', countErr)
    }

    return data as JobApplication
  },

  /**
   * Fetch all applications for a specific job (for OrgAdmin and OrgStaff).
   * Includes joined user profile information.
   */
  async fetchApplicationsForJob(jobId: string): Promise<JobApplication[]> {
    if (!jobId) return []

    // Attempt direct FK relationship join first
    let { data, error } = await supabase
      .from('job_applications')
      .select(`
        id,
        job_id,
        user_id,
        status,
        cover_letter,
        resume_id,
        custom_answers,
        current_process_id,
        score,
        applied_at,
        updated_at,
        withdrawn_at,
        user:users!user_id (
          id,
          email,
          first_name,
          last_name,
          display_name,
          avatar_path,
          headline,
          current_job_title,
          phone,
          city,
          country_code
        )
      `)
      .eq('job_id', jobId)
      .order('applied_at', { ascending: false })

    // Fallback if joined FK fails due to PostgREST schema cache
    if (error) {
      console.warn('Direct FK application join failed, fetching users separately:', error.message)
      const { data: rawApps, error: rawError } = await supabase
        .from('job_applications')
        .select('*')
        .eq('job_id', jobId)
        .order('applied_at', { ascending: false })

      if (rawError) throw rawError

      if (rawApps && rawApps.length > 0) {
        const userIds = [...new Set(rawApps.map((a: any) => a.user_id).filter(Boolean))]
        let usersMap = new Map<string, any>()

        if (userIds.length > 0) {
          const { data: usersData } = await supabase
            .from('users')
            .select('id, email, first_name, last_name, display_name, avatar_path, headline, current_job_title, phone, city, country_code')
            .in('id', userIds)

          usersMap = new Map((usersData || []).map((u: any) => [u.id, u]))
        }

        data = rawApps.map((a: any) => ({
          ...a,
          user: usersMap.get(a.user_id) || null,
        }))
      } else {
        data = []
      }
    }

    return (data || []) as unknown as JobApplication[]
  },

  /**
   * Update the status of an application.
   */
  async updateApplicationStatus(
    applicationId: string,
    status: ApplicationStatus
  ): Promise<void> {
    const { error } = await supabase
      .from('job_applications')
      .update({
        status,
        updated_at: new Date().toISOString(),
      })
      .eq('id', applicationId)

    if (error) throw error
  },
}

