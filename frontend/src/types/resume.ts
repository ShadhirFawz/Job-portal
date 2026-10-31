export interface Resume {
  id: string
  user_id: string
  title: string
  is_primary: boolean
  file_path: string | null
  file_name: string | null
  file_size: number | null
  file_type: string | null
  summary?: string | null
  skills?: string[] | null
  experience?: Array<{
    company?: string
    title?: string
    start?: string
    end?: string
    description?: string
  }> | null
  education?: Array<{
    institution?: string
    degree?: string
    field?: string
    year?: string
  }> | null
  achievements?: string[] | null
  languages?: string[] | null
  certifications?: any[] | null
  is_public: boolean
  last_used_at?: string | null
  created_at: string
  updated_at: string
  deleted_at?: string | null
}

export interface CreateResumeInput {
  user_id: string
  title: string
  file_path: string
  file_name: string
  file_size: number
  file_type: string
  is_primary?: boolean
}

export interface UpdateResumeInput {
  title?: string
  is_primary?: boolean
  summary?: string | null
  skills?: string[] | null
}

