export type OrganizationStatus =
  | 'pending_verification'
  | 'active'
  | 'suspended'
  | 'rejected'
  | 'deleted'

export interface Organization {
  id: string
  name: string
  slug: string | null
  legal_name: string | null
  description: string | null
  tagline: string | null
  official_email: string
  official_email_verified: boolean
  phone: string | null
  website_url: string | null
  support_email: string | null
  avatar_path: string | null
  cover_path: string | null
  has_physical_office: boolean
  country_code: string | null
  state_province: string | null
  city: string | null
  postal_code: string | null
  address_line1: string | null
  address_line2: string | null
  latitude: number | null
  longitude: number | null
  industry: string | null
  company_size: string | null
  founded_year: number | null
  company_type: string | null
  linkedin_url: string | null
  twitter_url: string | null
  facebook_url: string | null
  github_url: string | null
  glassdoor_url: string | null
  status: OrganizationStatus
  created_by: string
  created_at: string
  updated_at?: string
}

export interface OrganizationCreateInput {
  name: string
  legal_name?: string | null
  tagline?: string | null
  description?: string | null
  official_email: string
  phone?: string | null
  website_url?: string | null
  industry?: string | null
  company_size?: string | null
  founded_year?: number | null
  has_physical_office?: boolean
  country_code?: string | null
  city?: string | null
  address_line1?: string | null
}

export interface OrganizationUpdateInput {
  name?: string
  legal_name?: string | null
  tagline?: string | null
  description?: string | null
  official_email?: string
  official_email_verified?: boolean
  support_email?: string | null
  phone?: string | null
  website_url?: string | null
  industry?: string | null
  company_size?: string | null
  founded_year?: number | null
  company_type?: string | null
  has_physical_office?: boolean
  country_code?: string | null
  state_province?: string | null
  city?: string | null
  postal_code?: string | null
  address_line1?: string | null
  address_line2?: string | null
  linkedin_url?: string | null
  twitter_url?: string | null
  facebook_url?: string | null
  github_url?: string | null
  glassdoor_url?: string | null
  avatar_path?: string | null
  cover_path?: string | null
  updated_at?: string
}

