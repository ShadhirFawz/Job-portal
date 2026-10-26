<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useOrganizationsStore } from '@/stores/organizations'
import { getOrganizationImageUrl } from '@/composables/useOrganizationImage'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const orgStore = useOrganizationsStore()

const activeTab = ref<'overview' | 'team' | 'jobs' | 'settings'>('overview')
const members = ref<any[]>([])
const loadingMembers = ref(false)
const isOrgAdmin = ref(false)

const org = computed(() => orgStore.currentOrganization)

onMounted(async () => {
  const id = route.params.id as string
  await orgStore.fetchOrganization(id)
  await checkIfOrgAdmin(id)
  if (activeTab.value === 'team') {
    await fetchMembers(id)
  }
})

async function checkIfOrgAdmin(orgId: string) {
  if (!auth.user) return

  const { data } = await supabase
    .from('organization_members')
    .select('role, status')
    .eq('organization_id', orgId)
    .eq('user_id', auth.user.id)
    .eq('status', 'active')
    .single()

  isOrgAdmin.value = data?.role === 'org_admin'
}

async function fetchMembers(orgId: string) {
  loadingMembers.value = true
  const { data, error } = await supabase
    .from('organization_members')
    .select(`
      id,
      role,
      status,
      title,
      department,
      joined_at,
      invited_at,
      user:users (
        id,
        email,
        first_name,
        last_name,
        display_name,
        avatar_path
      )
    `)
    .eq('organization_id', orgId)
    .order('created_at', { ascending: true })

  if (!error) {
    members.value = data || []
  }
  loadingMembers.value = false
}

function switchTab(tab: typeof activeTab.value) {
  activeTab.value = tab
  if (tab === 'team' && org.value) {
    fetchMembers(org.value.id)
  }
}
</script>

<template>
  <div v-if="orgStore.loading" class="p-10 text-center">Loading company...</div>

  <div v-else-if="org" class="min-h-screen bg-gray-50">
    <!-- Cover -->
    <div class="h-48 bg-linear-to-r from-indigo-500 to-purple-600 relative">
      <img
        v-if="org.cover_path"
        :src="getOrganizationImageUrl(org.cover_path) || ''"
        class="w-full h-full object-cover"
      />
    </div>

    <div class="max-w-6xl mx-auto px-4 sm:px-6 -mt-16 pb-16">
      <!-- Header Card -->
      <div class="bg-white rounded-lg shadow p-6 flex flex-col sm:flex-row items-start gap-6">
        <div class="h-24 w-24 rounded-full ring-4 ring-white bg-gray-200 overflow-hidden shrink-0">
          <img
            v-if="org.avatar_path"
            :src="getOrganizationImageUrl(org.avatar_path) || ''"
            class="h-full w-full object-cover"
          />
        </div>

        <div class="flex-1 min-w-0">
          <div class="flex flex-col sm:flex-row sm:items-start sm:justify-between gap-3">
            <div>
              <h1 class="text-2xl font-bold text-gray-900 truncate">{{ org.name }}</h1>
              <p v-if="org.tagline" class="text-gray-600 mt-1">{{ org.tagline }}</p>
              <div class="mt-2 flex flex-wrap items-center gap-x-3 gap-y-1 text-sm text-gray-500">
                <span v-if="org.industry">{{ org.industry }}</span>
                <span v-if="org.company_size">• {{ org.company_size }} employees</span>
                <span v-if="org.city">• {{ org.city }}</span>
                <span v-if="org.website_url">
                  • <a :href="org.website_url" target="_blank" class="text-indigo-600 hover:underline">Website</a>
                </span>
              </div>
            </div>

            <div class="flex items-center gap-2">
              <span
                class="px-3 py-1 text-xs font-medium rounded-full"
                :class="{
                  'bg-green-100 text-green-800': org.status === 'active',
                  'bg-yellow-100 text-yellow-800': org.status === 'pending_verification',
                  'bg-red-100 text-red-800': org.status === 'suspended' || org.status === 'rejected'
                }"
              >
                {{ org.status.replace('_', ' ') }}
              </span>
            </div>
          </div>
        </div>
      </div>

      <!-- Tabs -->
      <div class="mt-6 border-b border-gray-200">
        <nav class="flex gap-6">
          <button
            v-for="tab in [
              { id: 'overview', label: 'Overview' },
              { id: 'team', label: 'Team' },
              { id: 'jobs', label: 'Jobs' },
              { id: 'settings', label: 'Settings' }
            ]"
            :key="tab.id"
            @click="switchTab(tab.id as any)"
            class="pb-3 text-sm font-medium border-b-2 transition-colors"
            :class="activeTab === tab.id
              ? 'border-indigo-600 text-indigo-600'
              : 'border-transparent text-gray-500 hover:text-gray-700'"
          >
            {{ tab.label }}
          </button>
        </nav>
      </div>

      <!-- Tab Content -->
      <div class="mt-6">
        <!-- OVERVIEW -->
        <div v-if="activeTab === 'overview'" class="bg-white rounded-lg shadow p-6">
          <h2 class="text-lg font-semibold mb-4">About</h2>
          <p class="text-gray-700 whitespace-pre-line">{{ org.description || 'No description provided.' }}</p>

          <div v-if="org.status === 'pending_verification'" class="mt-6 p-4 bg-yellow-50 border border-yellow-200 rounded-lg">
            <h3 class="font-medium text-yellow-800">Email Verification Required</h3>
            <p class="text-sm text-yellow-700 mt-1">
              Please verify the official email <strong>{{ org.official_email }}</strong> to activate your company and start posting jobs.
            </p>
            <button
              class="mt-3 inline-flex items-center px-4 py-2 text-sm font-medium rounded-md text-white bg-yellow-600 hover:bg-yellow-700"
              @click="activeTab = 'settings'"
            >
              Go to Verification
            </button>
          </div>
        </div>

        <!-- TEAM -->
        <div v-else-if="activeTab === 'team'" class="bg-white rounded-lg shadow">
          <div class="p-6 border-b border-gray-200 flex items-center justify-between">
            <h2 class="text-lg font-semibold">Team Members</h2>
            <button
              v-if="isOrgAdmin"
              class="inline-flex items-center px-4 py-2 text-sm font-medium rounded-md text-white bg-indigo-600 hover:bg-indigo-700"
              @click="$router.push(`/organizations/${org.id}/invite`)"
            >
              + Invite Member
            </button>
          </div>

          <div v-if="loadingMembers" class="p-8 text-center text-gray-500">Loading members...</div>

          <ul v-else class="divide-y divide-gray-200">
            <li v-for="member in members" :key="member.id" class="p-4 flex items-center justify-between">
              <div class="flex items-center gap-4">
                <div class="h-10 w-10 rounded-full bg-gray-200 overflow-hidden">
                  <img
                    v-if="member.user?.avatar_path"
                    :src="getOrganizationImageUrl(member.user.avatar_path) || ''"
                    class="h-full w-full object-cover"
                  />
                </div>
                <div>
                  <p class="font-medium text-gray-900">
                    {{ member.user?.display_name || member.user?.first_name || member.user?.email }}
                  </p>
                  <p class="text-sm text-gray-500">
                    {{ member.title || member.role }} 
                    <span v-if="member.department">• {{ member.department }}</span>
                  </p>
                </div>
              </div>

              <div class="flex items-center gap-3">
                <span
                  class="px-2.5 py-0.5 text-xs font-medium rounded-full"
                  :class="{
                    'bg-green-100 text-green-800': member.status === 'active',
                    'bg-yellow-100 text-yellow-800': member.status === 'pending',
                    'bg-red-100 text-red-800': member.status === 'revoked' || member.status === 'rejected'
                  }"
                >
                  {{ member.status }}
                </span>
                <span class="text-xs text-gray-500 capitalize">{{ member.role.replace('_', ' ') }}</span>
              </div>
            </li>
          </ul>
        </div>

        <!-- JOBS (placeholder) -->
        <div v-else-if="activeTab === 'jobs'" class="bg-white rounded-lg shadow p-6">
          <h2 class="text-lg font-semibold mb-2">Job Postings</h2>
          <p class="text-gray-600">Job management will be implemented in the next module.</p>
        </div>

        <!-- SETTINGS -->
        <div v-else-if="activeTab === 'settings'" class="bg-white rounded-lg shadow p-6 space-y-6">
          <div>
            <h2 class="text-lg font-semibold mb-4">Company Settings</h2>
          </div>

          <!-- Email Verification Section -->
          <div class="border rounded-lg p-5">
            <h3 class="font-medium text-gray-900">Official Email Verification</h3>
            <p class="text-sm text-gray-600 mt-1">
              Current official email: <strong>{{ org.official_email }}</strong>
            </p>

            <div v-if="org.official_email_verified" class="mt-3 flex items-center gap-2 text-green-700">
              <svg class="h-5 w-5" fill="currentColor" viewBox="0 0 20 20">
                <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd" />
              </svg>
              Email verified
            </div>

            <div v-else class="mt-4">
              <p class="text-sm text-yellow-700 mb-3">
                This email is not yet verified. Verification is required before you can post jobs.
              </p>
              <button
                class="inline-flex items-center px-4 py-2 text-sm font-medium rounded-md text-white bg-indigo-600 hover:bg-indigo-700"
                @click="sendVerificationEmail"
              >
                Send Verification Email
              </button>
            </div>
          </div>

          <div class="text-sm text-gray-500">
            More settings (edit profile, danger zone, etc.) will be added later.
          </div>
        </div>
      </div>
    </div>
  </div>

  <div v-else class="p-10 text-center text-red-600">
    Organization not found or you don't have access.
  </div>
</template>

<script lang="ts">
// Temporary simple verification (we will improve it)
import { supabase } from '@/lib/supabase'

async function sendVerificationEmail() {
  // For now we just show an alert.
  // Real implementation will use a verification token table + email.
  alert('Verification email feature will be fully implemented next. For development you can manually set official_email_verified = true in the database.')
}
</script>