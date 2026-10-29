<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { toast } from 'vue-sonner'
import { useAuthStore } from '@/stores/auth'
import { memberService } from '@/services/member.service'
import { profileService } from '@/services/profile.service'
import { getOrganizationImageUrl } from '@/composables/useOrganizationImage'
import type { UserInvitationItem } from '@/types/member'

const router = useRouter()
const auth = useAuthStore()

const invitations = ref<UserInvitationItem[]>([])
const loading = ref(true)
const actionLoading = ref<string | null>(null)

onMounted(fetchInvitations)

async function fetchInvitations() {
  if (!auth.user) return
  loading.value = true

  try {
    invitations.value = await memberService.fetchMyInvitations(auth.user.id)
  } catch (err: any) {
    console.error('Failed to load invitations:', err)
    toast.error('Failed to load invitations')
  } finally {
    loading.value = false
  }
}

async function acceptInvitation(invitation: UserInvitationItem) {
  actionLoading.value = invitation.id
  try {
    await memberService.acceptInvitation(invitation.id)

    // Upgrade user role to org_staff if currently candidate
    if (auth.user?.role === 'candidate') {
      await profileService.updateUserProfile(auth.user.id, { role: 'org_staff' } as any)
      await auth.fetchUserProfile()
    }

    toast.success(`Joined ${invitation.organization.name}!`)
    await fetchInvitations()
  } catch (err: any) {
    toast.error(err.message || 'Failed to accept invitation')
  } finally {
    actionLoading.value = null
  }
}

async function declineInvitation(invitation: UserInvitationItem) {
  if (!confirm('Decline this invitation?')) return

  actionLoading.value = invitation.id
  try {
    await memberService.declineInvitation(invitation.id)
    toast.success('Invitation declined')
    await fetchInvitations()
  } catch (err: any) {
    toast.error(err.message || 'Failed to decline invitation')
  } finally {
    actionLoading.value = null
  }
}
</script>

<template>
  <div class="min-h-screen bg-gray-50 py-10 px-4">
    <div class="max-w-3xl mx-auto">
      <h1 class="text-2xl font-bold text-gray-900 mb-2">Pending Invitations</h1>
      <p class="text-gray-600 mb-8">Organizations that have invited you to join their team.</p>

      <div v-if="loading" class="text-center py-12 text-gray-500">Loading invitations...</div>

      <div v-else-if="invitations.length === 0" class="bg-white rounded-lg shadow p-12 text-center">
        <p class="text-gray-500">You have no pending invitations.</p>
        <button @click="router.push('/')" class="mt-4 text-indigo-600 hover:underline text-sm">
          Back to Home
        </button>
      </div>

      <ul v-else class="space-y-4">
        <li
          v-for="inv in invitations"
          :key="inv.id"
          class="bg-white rounded-lg shadow p-5 flex flex-col sm:flex-row sm:items-center justify-between gap-4"
        >
          <div class="flex items-center gap-4">
            <div class="h-14 w-14 rounded-full bg-gray-200 overflow-hidden shrink-0">
              <img
                v-if="inv.organization?.avatar_path"
                :src="getOrganizationImageUrl(inv.organization.avatar_path) || ''"
                class="h-full w-full object-cover"
              />
            </div>
            <div>
              <h3 class="font-semibold text-gray-900">{{ inv.organization?.name }}</h3>
              <p v-if="inv.organization?.tagline" class="text-sm text-gray-500">{{ inv.organization.tagline }}</p>
              <p class="text-xs text-gray-400 mt-1">
                Invited as {{ inv.role === 'org_admin' ? 'Admin' : 'Staff' }}
                <span v-if="inv.title"> • {{ inv.title }}</span>
              </p>
            </div>
          </div>

          <div class="flex items-center gap-2">
            <button
              :disabled="actionLoading === inv.id"
              @click="acceptInvitation(inv)"
              class="px-4 py-2 text-sm font-medium text-white bg-green-600 hover:bg-green-700 rounded-md disabled:opacity-50"
            >
              Accept
            </button>
            <button
              :disabled="actionLoading === inv.id"
              @click="declineInvitation(inv)"
              class="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 hover:bg-gray-50 rounded-md disabled:opacity-50"
            >
              Decline
            </button>
          </div>
        </li>
      </ul>
    </div>
  </div>
</template>