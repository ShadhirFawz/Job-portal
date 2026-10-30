export type ApplicationStatus =
  | 'applied'
  | 'under_review'
  | 'shortlisted'
  | 'interview'
  | 'offered'
  | 'hired'
  | 'rejected'
  | 'withdrawn'

export interface JobApplicantUser {
  id: string
  email: string
  first_name: string | null
  last_name: string | null
  display_name: string | null
  avatar_path: string | null
  headline: string | null
  current_job_title: string | null
  phone?: string | null
  city?: string | null
  country_code?: string | null
}

export interface JobApplication {
  id: string
  job_id: string
  user_id: string
  status: ApplicationStatus
  cover_letter: string | null
  resume_id: string | null
  custom_answers: Record<string, any>
  current_process_id: string | null
  score: number | null
  applied_at: string
  updated_at: string
  withdrawn_at?: string | null
  user?: JobApplicantUser
}

export interface CreateApplicationInput {
  job_id: string
  user_id: string
  cover_letter?: string | null
  custom_answers?: Record<string, any>
  status?: ApplicationStatus
}

export type ApplicationStatusFilter = 'all' | ApplicationStatus

