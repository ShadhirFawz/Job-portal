export type AppRole = 'super_admin' | 'org_admin' | 'org_staff' | 'candidate'

export interface AppUser {
  id: string
  email: string
  first_name: string | null
  last_name: string | null
  display_name: string | null
  username?: string | null
  avatar_path: string | null
  cover_path: string | null
  phone: string | null
  headline: string | null
  current_job_title: string | null
  current_company: string | null
  bio: string | null
  website_url?: string | null
  linkedin_url?: string | null
  github_url?: string | null
  portfolio_url?: string | null
  country_code: string | null
  state_province?: string | null
  city: string | null
  years_of_experience?: number | null
  role: AppRole
  status: string
  is_profile_complete: boolean
  created_at?: string
  updated_at?: string
}

export interface ProfileUpdateInput {
  first_name?: string
  last_name?: string
  display_name?: string
  username?: string | null
  phone?: string | null
  headline?: string | null
  current_job_title?: string | null
  current_company?: string | null
  bio?: string | null
  website_url?: string | null
  linkedin_url?: string | null
  github_url?: string | null
  portfolio_url?: string | null
  country_code?: string | null
  state_province?: string | null
  city?: string | null
  years_of_experience?: number | null
  avatar_path?: string | null
  cover_path?: string | null
  is_profile_complete?: boolean
}

export interface UsernameCheckResult {
  username: string
  is_available: boolean
  message: string
}

