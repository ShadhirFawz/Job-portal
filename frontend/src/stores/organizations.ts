import { defineStore } from 'pinia'
import { ref } from 'vue'
import { useAuthStore } from './auth'
import { organizationService } from '@/services/organization.service'
import type { Organization } from '@/types/organization'

export type { Organization }

export const useOrganizationsStore = defineStore('organizations', () => {
  const myOrganizations = ref<Organization[]>([])
  const currentOrganization = ref<Organization | null>(null)
  const loading = ref(false)

  async function fetchMyOrganizations() {
    const auth = useAuthStore()
    if (!auth.user) return

    loading.value = true
    try {
      myOrganizations.value = await organizationService.getMyOrganizations(auth.user.id)
    } finally {
      loading.value = false
    }
  }

  async function fetchOrganization(id: string) {
    loading.value = true
    try {
      currentOrganization.value = await organizationService.getOrganization(id)
    } finally {
      loading.value = false
    }
  }

  return {
    myOrganizations,
    currentOrganization,
    loading,
    fetchMyOrganizations,
    fetchOrganization,
  }
})
