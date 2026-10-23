import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { supabase } from '@/lib/supabase'
import type { User, Session } from '@supabase/supabase-js'

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

export const useAuthStore = defineStore('auth', () => {
    const session = ref<Session | null>(null)
    const user = ref<AppUser | null>(null)
    const loading = ref(true)

    const isAuthenticated = computed(() => !!session.value)
    const role = computed(() => user.value?.role ?? null)

    async function init() {
        loading.value = true
        const { data } = await supabase.auth.getSession()
        session.value = data.session

        if (data.session) {
            await fetchUserProfile()
        }

        // Listen for auth changes
        supabase.auth.onAuthStateChange(async (_event, newSession) => {
            session.value = newSession
            if (newSession) {
                await fetchUserProfile()
            } else {
                user.value = null
            }
        })

        loading.value = false
    }

    async function fetchUserProfile() {
        if (!session.value?.user) return

        const { data, error } = await supabase
            .from('users')
            .select('*')
            .eq('id', session.value.user.id)
            .single()

        if (error) {
            console.error('Failed to fetch user profile', error)
            // Fallback for newly created users where row in public.users is not yet present
            if (session.value?.user) {
                const su = session.value.user
                user.value = {
                    id: su.id,
                    email: su.email || '',
                    first_name: su.user_metadata?.first_name || null,
                    last_name: su.user_metadata?.last_name || null,
                    display_name: su.user_metadata?.display_name || null,
                    avatar_path: null,
                    cover_path: null,
                    phone: su.phone || null,
                    headline: null,
                    current_job_title: null,
                    current_company: null,
                    bio: null,
                    country_code: null,
                    city: null,
                    role: 'candidate',
                    status: 'active',
                    is_profile_complete: false
                }
            }
            return
        }

        user.value = data as AppUser
    }

    async function signUp(email: string, password: string, meta: { first_name?: string; last_name?: string }) {
        const { data, error } = await supabase.auth.signUp({
            email,
            password,
            options: {
                data: meta,
                emailRedirectTo: `${window.location.origin}/auth/callback`
            }
        })
        return { data, error }
    }

    async function signIn(email: string, password: string) {
        const { data, error } = await supabase.auth.signInWithPassword({ email, password })
        if (!error && data.session) {
            session.value = data.session
            await fetchUserProfile()
        }
        return { data, error }
    }

    async function signOut() {
        await supabase.auth.signOut()
        user.value = null
        session.value = null
    }

    return {
        session,
        user,
        loading,
        isAuthenticated,
        role,
        init,
        fetchUserProfile,
        signUp,
        signIn,
        signOut
    }
})