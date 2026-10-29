import { defineStore } from 'pinia'
import { ref } from 'vue'
import { supabase } from '@/lib/supabase'

export interface SelectionProcess {
    id?: string
    title: string
    process_type: string
    description?: string
    order_index: number
    is_mandatory: boolean
    time_limit_minutes?: number | null
    passing_score?: number | null
    config?: Record<string, any>
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
    job_type: string
    work_mode: string
    experience_level: string | null
    department: string | null
    city: string | null
    country_code: string | null
    salary_min: number | null
    salary_max: number | null
    salary_currency: string
    show_salary: boolean
    status: string
    published_at: string | null
    application_deadline: string | null
    applications_count: number
    created_at: string
    selection_processes?: SelectionProcess[]
    organization?: any
}

export const useJobsStore = defineStore('jobs', () => {
    const organizationJobs = ref<Job[]>([])
    const currentJob = ref<Job | null>(null)
    const loading = ref(false)

    async function fetchOrganizationJobs(organizationId: string) {
        loading.value = true
        const { data, error } = await supabase
            .from('jobs')
            .select(`
        *,
        selection_processes:job_selection_processes (*)
      `)
            .eq('organization_id', organizationId)
            .is('deleted_at', null)
            .order('created_at', { ascending: false })

        if (!error) {
            organizationJobs.value = data || []
        }
        loading.value = false
    }

    async function fetchJob(id: string) {
        loading.value = true
        const { data, error } = await supabase
            .from('jobs')
            .select(`
        *,
        selection_processes:job_selection_processes (*),
        organization:organizations (id, name, slug, avatar_path, industry, city)
      `)
            .eq('id', id)
            .single()

        if (!error) {
            currentJob.value = data
        } else {
            currentJob.value = null
        }
        loading.value = false
    }

    async function fetchPublicJob(id: string) {
        // Only published jobs
        loading.value = true
        const { data, error } = await supabase
            .from('jobs')
            .select(`
        *,
        selection_processes:job_selection_processes (*),
        organization:organizations (id, name, slug, avatar_path, industry, city, website_url)
      `)
            .eq('id', id)
            .eq('status', 'published')
            .single()

        if (!error) {
            currentJob.value = data
        } else {
            currentJob.value = null
        }
        loading.value = false
    }

    return {
        organizationJobs,
        currentJob,
        loading,
        fetchOrganizationJobs,
        fetchJob,
        fetchPublicJob
    }
})