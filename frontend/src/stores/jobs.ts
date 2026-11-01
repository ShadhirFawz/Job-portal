import { defineStore } from 'pinia'
import { ref } from 'vue'
import { jobService } from '@/services/job.service'
import type { Job, SelectionProcess } from '@/types/job'

export type { Job, SelectionProcess }

export const useJobsStore = defineStore('jobs', () => {
  const organizationJobs = ref<Job[]>([])
  const currentJob = ref<Job | null>(null)
  const loading = ref(false)

  async function fetchOrganizationJobs(organizationId: string) {
    loading.value = true
    organizationJobs.value = await jobService.fetchOrganizationJobs(organizationId)
    loading.value = false
  }

  async function fetchJob(id: string) {
    loading.value = true
    currentJob.value = await jobService.fetchJobById(id, false)
    loading.value = false
  }

  async function fetchPublicJob(id: string) {
    loading.value = true
    currentJob.value = await jobService.fetchJobById(id, true)
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
