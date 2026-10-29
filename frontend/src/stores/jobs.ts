import { defineStore } from 'pinia'
import { ref } from 'vue'
import { supabase } from '@/lib/supabase'
import type { Job, SelectionProcess } from '@/types/job'

export type { Job, SelectionProcess }

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
    fetchPublicJob,
  }
})
