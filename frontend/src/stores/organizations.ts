import { defineStore } from 'pinia'
import { ref } from 'vue'
import { supabase } from '@/lib/supabase'
import { useAuthStore } from './auth'

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
    avatar_path: string | null
    cover_path: string | null
    has_physical_office: boolean
    country_code: string | null
    city: string | null
    address_line1: string | null
    latitude: number | null
    longitude: number | null
    industry: string | null
    company_size: string | null
    founded_year: number | null
    status: string
    created_by: string
    created_at: string
}

export const useOrganizationsStore = defineStore('organizations', () => {
    const myOrganizations = ref<Organization[]>([])
    const currentOrganization = ref<Organization | null>(null)
    const loading = ref(false)

    async function fetchMyOrganizations() {
        const auth = useAuthStore()
        if (!auth.user) return

        loading.value = true

        const { data, error } = await supabase
            .from('organization_members')
            .select(`
        role,
        status,
        organization:organizations (*)
      `)
            .eq('user_id', auth.user.id)
            .eq('status', 'active')

        if (error) {
            console.error(error)
            loading.value = false
            return
        }

        myOrganizations.value = (data || [])
            .map((item: any) => item.organization)
            .filter(Boolean)

        loading.value = false
    }

    async function fetchOrganization(id: string) {
        loading.value = true
        const { data, error } = await supabase
            .from('organizations')
            .select('*')
            .eq('id', id)
            .single()

        if (error) {
            console.error(error)
            currentOrganization.value = null
        } else {
            currentOrganization.value = data
        }
        loading.value = false
    }

    return {
        myOrganizations,
        currentOrganization,
        loading,
        fetchMyOrganizations,
        fetchOrganization
    }
})