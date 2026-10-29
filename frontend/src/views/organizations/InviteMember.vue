<script setup lang="ts">
import { ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { memberService } from '@/services/member.service'

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

    await memberService.inviteMember({
      organizationId: orgId,
      email: trimmedEmail,
      role: role.value,
      title: title.value.trim() || null,
      inviterId: auth.user!.id,
    })

    successMsg.value = 'Invitation sent successfully! The user is now listed in the Team tab.'
    email.value = ''
    title.value = ''
  } catch (err: any) {
    errorMsg.value = err.message || 'Failed to send invitation. Please try again.'
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
