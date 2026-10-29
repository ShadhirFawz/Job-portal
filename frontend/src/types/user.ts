export type AppRole = 'super_admin' | 'org_admin' | 'org_staff' | 'candidate'

export interface AppUser {
  id: string
  email: string
  first_name: string | null
  last_name: string | null
  display_name: string | null
  avatar_path: string | null
  cover_path: string | null
  phone: string | null
  headline: string | null
  current_job_title: string | null
  current_company: string | null
  bio: string | null
  country_code: string | null
  city: string | null
  role: AppRole
  status: string
  is_profile_complete: boolean
}

export interface ProfileUpdateInput {
  first_name?: string
  last_name?: string
  display_name?: string
  phone?: string | null
  headline?: string | null
  current_job_title?: string | null
  current_company?: string | null
  bio?: string | null
  country_code?: string | null
  city?: string | null
  avatar_path?: string | null
  cover_path?: string | null
  is_profile_complete?: boolean
}

