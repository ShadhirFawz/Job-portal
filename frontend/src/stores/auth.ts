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
    role: AppRole
    status: string
    is_profile_complete: boolean
    // add more fields later as needed
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