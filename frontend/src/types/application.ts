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

export interface JobApplicationResume {
  id: string
  title: string
  file_name: string | null
  file_path: string | null
  file_size: number | null
  file_type: string | null
}

export interface CandidateApplicationOrg {
  id: string
  name: string
  slug?: string | null
  avatar_path?: string | null
}

export interface CandidateApplicationJob {
  id: string
  title: string
  slug?: string | null
  job_type: string
  work_mode: string
  city?: string | null
  country_code?: string | null
  status: string
  application_deadline?: string | null
  organization?: CandidateApplicationOrg | null
}

export interface CandidateSelectionStage {
  id: string
  title: string
  slug?: string | null
  process_type: string
  order_index: number
  is_mandatory: boolean
  time_limit_minutes?: number | null
  passing_score?: number | null
  question_bank_id?: string | null
}

export interface CandidateQuizAttemptSummary {
  id: string
  selection_process_id: string
  application_id?: string | null
  status: 'not_started' | 'in_progress' | 'submitted' | 'graded' | 'expired'
  score: number
  max_score: number
  percentage: number
  passed?: boolean | null
  requires_manual_review?: boolean
  started_at: string
  submitted_at?: string | null
  reviewed_at?: string | null
  passing_score?: number | null
  time_limit_minutes?: number | null
  stage_title?: string | null
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
  resume?: JobApplicationResume | null
  slug: string
  job?: CandidateApplicationJob | null
  selection_stages?: CandidateSelectionStage[]
  quiz_attempts?: CandidateQuizAttemptSummary[]
}

export interface CreateApplicationInput {
  job_id: string
  user_id: string
  cover_letter?: string | null
  resume_id?: string | null
  custom_answers?: Record<string, any>
  status?: ApplicationStatus
}

export type ApplicationStatusFilter = 'all' | ApplicationStatus
