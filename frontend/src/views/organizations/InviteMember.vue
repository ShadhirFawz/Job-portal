<script setup lang="ts">
import { ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { supabase } from '@/lib/supabase'
import { useAuthStore } from '@/stores/auth'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()

const orgId = route.params.id as string
const email = ref('')
const title = ref('')
const role = ref<'org_staff'>('org_staff')
const loading = ref(false)
const errorMsg = ref('')
const successMsg = ref('')

async function handleInvite() {
  loading.value = true
  errorMsg.value = ''
  successMsg.value = ''

  try {
    const trimmedEmail = email.value.trim().toLowerCase()
    if (!trimmedEmail) {
      throw new Error('Please enter a valid email address.')
    }

    if (auth.user && auth.user.email.toLowerCase() === trimmedEmail) {
      throw new Error('You cannot invite yourself to the organization.')
    }

    // 1. Find user by email (try RPC first for SECURITY DEFINER bypass, fallback to direct query)
    let user: { id: string; email: string } | null = null

    const { data: rpcData, error: rpcError } = await supabase
      .rpc('find_user_by_email', { search_email: trimmedEmail })

    if (!rpcError && rpcData && rpcData.length > 0) {
      user = rpcData[0]
    } else {
      const { data: userData, error: userError } = await supabase
        .from('users')
        .select('id, email')
        .ilike('email', trimmedEmail)
        .maybeSingle()

      if (userError) {
        throw new Error(`Failed to query users: ${userError.message}`)
      }
      user = userData
    }

    if (!user) {
      throw new Error('No user found with this email. They must register on the platform first.')
    }

    // 2. Check if already a member
    const { data: existing, error: existingError } = await supabase
      .from('organization_members')
      .select('id, status')
      .eq('organization_id', orgId)
      .eq('user_id', user.id)
      .maybeSingle()

    if (existingError) {
      throw new Error(`Failed to check existing membership: ${existingError.message}`)
    }

    if (existing) {
      if (existing.status === 'active') {
        throw new Error('This user is already an active member of the organization.')
      } else if (existing.status === 'pending') {
        throw new Error('This user already has a pending invitation for this organization.')
      } else if (existing.status === 'revoked' || existing.status === 'rejected') {
        // Re-activate as pending invite
        const { error: updateError } = await supabase
          .from('organization_members')
          .update({
            role: role.value,
            status: 'pending',
            title: title.value.trim() || null,
            invited_by: auth.user!.id,
            invited_at: new Date().toISOString(),
            revoked_at: null,
            revoked_by: null,
            rejection_reason: null
          })
          .eq('id', existing.id)

        if (updateError) throw updateError

        successMsg.value = 'Invitation sent successfully! The user is now listed in the Team tab.'
        email.value = ''
        title.value = ''
        return
      }
    }

    // 3. Create membership with pending status
    const { error: insertError } = await supabase
      .from('organization_members')
      .insert({
        organization_id: orgId,
        user_id: user.id,
        role: role.value,
        status: 'pending',
        title: title.value.trim() || null,
        invited_by: auth.user!.id,
        invited_at: new Date().toISOString()
      })

    if (insertError) throw insertError

    successMsg.value = 'Invitation sent successfully! The user is now listed in the Team tab.'
    email.value = ''
    title.value = ''

  } catch (err: any) {
    errorMsg.value = err.message
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="min-h-screen bg-gray-50 py-10 px-4">
    <div class="max-w-lg mx-auto">
      <div class="mb-6">
        <button @click="router.push(`/organizations/${orgId}?tab=team`)" class="text-sm text-indigo-600 hover:underline">
          ← Back to company team
        </button>
        <h1 class="text-2xl font-bold text-gray-900 mt-2">Invite Team Member</h1>
        <p class="text-gray-600 mt-1">Invite an existing user to join your organization as staff.</p>
      </div>

      <form @submit.prevent="handleInvite" class="bg-white shadow rounded-lg p-6 space-y-5">
        <div v-if="errorMsg" class="rounded-md bg-red-50 p-3 text-sm text-red-700">{{ errorMsg }}</div>
        
        <div v-if="successMsg" class="rounded-md bg-green-50 p-4 text-sm text-green-700 space-y-2">
          <p class="font-medium">{{ successMsg }}</p>
          <div class="pt-2 flex gap-3">
            <button
              type="button"
              @click="router.push(`/organizations/${orgId}?tab=team`)"
              class="inline-flex items-center px-3 py-1.5 border border-transparent text-xs font-semibold rounded text-white bg-green-600 hover:bg-green-700 shadow-sm"
            >
              View Team Members →
            </button>
          </div>
        </div>

        <div>
          <label class="block text-sm font-medium text-gray-700">Email address *</label>
          <input v-model="email" type="email" required
            class="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:ring-indigo-500 sm:text-sm"
            placeholder="colleague@company.com" />
        </div>

        <div>
          <label class="block text-sm font-medium text-gray-700">Title / Position</label>
          <input v-model="title" type="text"
            class="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:ring-indigo-500 sm:text-sm"
            placeholder="e.g. HR Manager, Technical Recruiter" />
        </div>

        <div>
          <label class="block text-sm font-medium text-gray-700">Role</label>
          <select v-model="role"
            class="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:ring-indigo-500 sm:text-sm">
            <option value="org_staff">Staff</option>
            <!-- We can allow promoting to admin later -->
          </select>
        </div>

        <button type="submit" :disabled="loading"
          class="w-full flex justify-center py-2.5 px-4 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-indigo-600 hover:bg-indigo-700 disabled:opacity-50">
          {{ loading ? 'Sending Invite...' : 'Send Invitation' }}
        </button>
      </form>
    </div>
  </div>
</template>
