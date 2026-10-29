export type JobStatus = 'draft' | 'published' | 'paused' | 'closed' | 'archived'
export type JobType = 'full_time' | 'part_time' | 'contract' | 'internship' | 'freelance' | 'temporary'
export type WorkMode = 'onsite' | 'remote' | 'hybrid'
export type ExperienceLevel = 'entry' | 'junior' | 'mid' | 'senior' | 'lead' | 'executive'
export type SelectionProcessType =
  | 'quiz'
  | 'screening'
  | 'coding'
  | 'screening_quiz'
  | 'screening_coding'
  | 'quiz_coding'
  | 'custom'

export interface SelectionProcess {
  id?: string
  job_id?: string
  title: string
  process_type: SelectionProcessType | string
  description?: string | null
  order_index: number
  is_mandatory: boolean
  time_limit_minutes?: number | null
  passing_score?: number | null
  config?: Record<string, any>
  created_at?: string
  updated_at?: string
}

export interface Job {
  id: string
  organization_id: string
  title: string
  slug: string | null
  description: string | null
  short_description: string | null
  responsibilities: string | null
  requirements: string | null
  benefits: string | null
  nice_to_have: string | null
  what_we_offer: string | null
  tech_stacks: string[]
  has_custom_hiring_process: boolean
  how_to_apply: string | null
  is_selection_process_public: boolean
  job_type: JobType | string
  work_mode: WorkMode | string
  experience_level: ExperienceLevel | string | null
  department: string | null
  category: string | null
  city: string | null
  country_code: string | null
  state_province?: string | null
  is_remote_friendly?: boolean
  salary_min: number | null
  salary_max: number | null
  salary_currency: string
  salary_period?: string
  show_salary: boolean
  status: JobStatus | string
  published_at: string | null
  closed_at?: string | null
  application_deadline: string | null
  applications_count: number
  views_count?: number
  created_by?: string
  updated_by?: string | null
  created_at: string
  updated_at: string
  selection_processes?: SelectionProcess[]
  organization?: any
}

