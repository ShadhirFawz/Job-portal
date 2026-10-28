<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { supabase } from '@/lib/supabase'
import { useAuthStore } from '@/stores/auth'
import { useOrganizationsStore } from '@/stores/organizations'
import { getOrganizationImageUrl } from '@/composables/useOrganizationImage'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const orgStore = useOrganizationsStore()

const activeTab = ref<'overview' | 'team' | 'jobs' | 'settings'>(
  (route.query.tab as any) || 'overview'
)

// Team management state
const members = ref<any[]>([])
const loadingMembers = ref(false)
const membersError = ref('')
const isOrgAdmin = ref(false)
const actionLoading = ref<string | null>(null)
const selectedFilter = ref<'all' | 'active' | 'pending' | 'revoked'>('all')
const searchQuery = ref('')

// Quick Edit Modal state
const editingMember = ref<any | null>(null)
const editTitle = ref('')
const editDepartment = ref('')
const savingEdit = ref(false)

const org = computed(() => orgStore.currentOrganization)

// Sync tab changes to URL query param
watch(activeTab, (newTab) => {
  if (route.query.tab !== newTab) {
    router.replace({ query: { ...route.query, tab: newTab } })
  }
})

// Sync query changes from router back/forward to activeTab
watch(
  () => route.query.tab,
  (newTab) => {
    if (newTab && ['overview', 'team', 'jobs', 'settings'].includes(newTab as string)) {
      activeTab.value = newTab as any
      if (newTab === 'team' && members.value.length === 0) {
        fetchMembers(route.params.id as string)
      }
    }
  }
)

onMounted(async () => {
  const id = route.params.id as string
  await orgStore.fetchOrganization(id)
  await checkIfOrgAdmin(id)

  if (activeTab.value === 'team') {
    await fetchMembers(id)
  }
})

async function checkIfOrgAdmin(orgId: string) {
  if (!auth.user) {
    isOrgAdmin.value = false
    return
  }

  // 1. Creator of organization is automatically org admin
  if (org.value && org.value.created_by === auth.user.id) {
    isOrgAdmin.value = true
    return
  }

  // 2. Platform super admin has admin controls
  if (auth.user.role === 'super_admin') {
    isOrgAdmin.value = true
    return
  }

  // 3. Check active membership with org_admin role
  const { data } = await supabase
    .from('organization_members')
    .select('role, status')
    .eq('organization_id', orgId)
    .eq('user_id', auth.user.id)
    .eq('status', 'active')
    .maybeSingle()

  isOrgAdmin.value = data?.role === 'org_admin'
}

async function fetchMembers(orgId: string) {
  if (!orgId) return
  loadingMembers.value = true
  membersError.value = ''

  try {
    // 1. Primary query: Disambiguated PostgREST foreign key joins
    let { data, error } = await supabase
      .from('organization_members')
      .select(`
        id,
        organization_id,
        user_id,
        role,
        status,
        title,
        department,
        joined_at,
        invited_at,
        invited_by,
        user:users!user_id (
          id,
          email,
          first_name,
          last_name,
          display_name,
          avatar_path
        ),
        inviter:users!invited_by (
          id,
          email,
          display_name,
          first_name,
          last_name
        )
      `)
      .eq('organization_id', orgId)
      .order('created_at', { ascending: true })

    // 2. Defensive fallback: If PostgREST foreign key embedding fails (e.g. ambiguity or schema caching),
    // fetch members directly and hydrate user profiles manually.
    if (error) {
      console.warn('FK embed query returned error, trying fallback hydration:', error)
      const { data: rawMembers, error: rawError } = await supabase
        .from('organization_members')
        .select('*')
        .eq('organization_id', orgId)
        .order('created_at', { ascending: true })

      if (rawError) throw rawError

      if (rawMembers && rawMembers.length > 0) {
        const userIds = [...new Set(rawMembers.map((m: any) => m.user_id).filter(Boolean))]
        const inviterIds = [...new Set(rawMembers.map((m: any) => m.invited_by).filter(Boolean))]
        const allUserIds = [...new Set([...userIds, ...inviterIds])]

        let usersMap = new Map()
        if (allUserIds.length > 0) {
          const { data: usersData } = await supabase
            .from('users')
            .select('id, email, first_name, last_name, display_name, avatar_path')
            .in('id', allUserIds)

          usersMap = new Map((usersData || []).map((u: any) => [u.id, u]))
        }

        data = rawMembers.map((m: any) => ({
          ...m,
          user: usersMap.get(m.user_id) || null,
          inviter: usersMap.get(m.invited_by) || null
        }))
      } else {
        data = []
      }
    }

    members.value = data || []
  } catch (err: any) {
    console.error('Error fetching members:', err)
    membersError.value = err.message || 'Failed to load team members'
  } finally {
    loadingMembers.value = false
  }
}

// Counts for filter pills
const counts = computed(() => {
  const all = members.value.length
  const active = members.value.filter((m: { status: string }) => m.status === 'active').length
  const pending = members.value.filter((m: { status: string }) => m.status === 'pending').length
  const revoked = members.value.filter((m: { status: string }) => m.status === 'revoked' || m.status === 'rejected').length
  return { all, active, pending, revoked, total: all }
})

// Filtered members by search and status filter
const filteredMembers = computed(() => {
  let list = members.value

  // Status filter
  if (selectedFilter.value === 'active') {
    list = list.filter((m: { status: string }) => m.status === 'active')
  } else if (selectedFilter.value === 'pending') {
    list = list.filter((m: { status: string }) => m.status === 'pending')
  } else if (selectedFilter.value === 'revoked') {
    list = list.filter((m: { status: string }) => m.status === 'revoked' || m.status === 'rejected')
  }

  // Search filter
  const query = searchQuery.value.trim().toLowerCase()
  if (query) {
    list = list.filter((m: { user: { display_name: any; first_name: any; last_name: any; email: any }; title: any; department: any; role: any }) => {
      const name = (m.user?.display_name || `${m.user?.first_name || ''} ${m.user?.last_name || ''}`).toLowerCase()
      const email = (m.user?.email || '').toLowerCase()
      const title = (m.title || '').toLowerCase()
      const dept = (m.department || '').toLowerCase()
      const role = (m.role || '').toLowerCase()
      return (
        name.includes(query) ||
        email.includes(query) ||
        title.includes(query) ||
        dept.includes(query) ||
        role.includes(query)
      )
    })
  }

  return list
})

// Organization Admin Actions
async function approveMember(member: any) {
  actionLoading.value = member.id
  try {
    const { error } = await supabase
      .from('organization_members')
      .update({
        status: 'active',
        joined_at: new Date().toISOString()
      })
      .eq('id', member.id)

    if (error) throw error

    await fetchMembers(route.params.id as string)
  } catch (err: any) {
    alert(`Failed to approve member: ${err.message}`)
  } finally {
    actionLoading.value = null
  }
}

async function revokeMember(member: any) {
  const isPending = member.status === 'pending'
  const promptText = isPending
    ? 'Are you sure you want to cancel this invitation?'
    : 'Are you sure you want to remove this member from the organization?'

  if (!confirm(promptText)) return

  actionLoading.value = member.id
  try {
    const { error } = await supabase
      .from('organization_members')
      .update({
        status: 'revoked',
        revoked_at: new Date().toISOString(),
        revoked_by: auth.user?.id
      })
      .eq('id', member.id)

    if (error) throw error

    await fetchMembers(route.params.id as string)
  } catch (err: any) {
    alert(`Failed to revoke membership: ${err.message}`)
  } finally {
    actionLoading.value = null
  }
}

async function reinviteMember(member: any) {
  actionLoading.value = member.id
  try {
    const { error } = await supabase
      .from('organization_members')
      .update({
        status: 'pending',
        invited_at: new Date().toISOString(),
        invited_by: auth.user?.id,
        revoked_at: null,
        revoked_by: null,
        rejection_reason: null
      })
      .eq('id', member.id)

    if (error) throw error

    await fetchMembers(route.params.id as string)
  } catch (err: any) {
    alert(`Failed to re-invite member: ${err.message}`)
  } finally {
    actionLoading.value = null
  }
}

async function deleteMemberRecord(member: any) {
  if (!confirm('Are you sure you want to completely remove this record?')) return

  actionLoading.value = member.id
  try {
    const { error } = await supabase
      .from('organization_members')
      .delete()
      .eq('id', member.id)

    if (error) throw error

    await fetchMembers(route.params.id as string)
  } catch (err: any) {
    alert(`Failed to delete member record: ${err.message}`)
  } finally {
    actionLoading.value = null
  }
}

async function toggleRole(member: any) {
  const newRole = member.role === 'org_admin' ? 'org_staff' : 'org_admin'
  const roleName = newRole === 'org_admin' ? 'Administrator' : 'Staff'

  // Safety check: Don't allow org creator to be demoted
  if (member.user_id === org.value?.created_by && newRole === 'org_staff') {
    alert('The organization creator must remain an Administrator.')
    return
  }

  if (!confirm(`Are you sure you want to change this member's role to ${roleName}?`)) return

  actionLoading.value = member.id
  try {
    const { error } = await supabase
      .from('organization_members')
      .update({ role: newRole })
      .eq('id', member.id)

    if (error) throw error

    await fetchMembers(route.params.id as string)
  } catch (err: any) {
    alert(`Failed to change role: ${err.message}`)
  } finally {
    actionLoading.value = null
  }
}

function openEditModal(member: any) {
  editingMember.value = member
  editTitle.value = member.title || ''
  editDepartment.value = member.department || ''
}

function closeEditModal() {
  editingMember.value = null
  editTitle.value = ''
  editDepartment.value = ''
}

async function saveMemberEdit() {
  if (!editingMember.value) return
  savingEdit.value = true

  try {
    const { error } = await supabase
      .from('organization_members')
      .update({
        title: editTitle.value.trim() || null,
        department: editDepartment.value.trim() || null
      })
      .eq('id', editingMember.value.id)

    if (error) throw error

    await fetchMembers(route.params.id as string)
    closeEditModal()
  } catch (err: any) {
    alert(`Failed to save details: ${err.message}`)
  } finally {
    savingEdit.value = false
  }
}

function switchTab(tab: typeof activeTab.value) {
  activeTab.value = tab
  if (tab === 'team') {
    fetchMembers((route.params.id as string) || org.value?.id || '')
  }
}

function getMemberDisplayName(member: any) {
  if (member.user?.display_name) return member.user.display_name
  if (member.user?.first_name || member.user?.last_name) {
    return `${member.user.first_name || ''} ${member.user.last_name || ''}`.trim()
  }
  if (member.user?.email) return member.user.email
  return 'Registered User'
}

function formatDate(isoString: string | null) {
  if (!isoString) return ''
  return new Date(isoString).toLocaleDateString(undefined, {
    year: 'numeric',
    month: 'short',
    day: 'numeric'
  })
}
</script>

<template>
  <div v-if="orgStore.loading" class="p-10 text-center text-gray-500">Loading company...</div>

  <div v-else-if="org" class="min-h-screen bg-gray-50 pb-16">
    <!-- Cover -->
    <div class="h-48 bg-linear-to-r from-indigo-500 to-purple-600 relative">
      <img
        v-if="org.cover_path"
        :src="getOrganizationImageUrl(org.cover_path) || ''"
        class="w-full h-full object-cover"
      />
    </div>

    <div class="max-w-6xl mx-auto px-4 sm:px-6 -mt-16">
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

      <!-- Tabs Navigation -->
      <div class="mt-6 border-b border-gray-200">
        <nav class="flex gap-6">
          <button
            @click="switchTab('overview')"
            class="pb-3 text-sm font-medium border-b-2 transition-colors"
            :class="activeTab === 'overview' ? 'border-indigo-600 text-indigo-600' : 'border-transparent text-gray-500 hover:text-gray-700'"
          >
            Overview
          </button>

          <button
            @click="switchTab('team')"
            class="pb-3 text-sm font-medium border-b-2 transition-colors flex items-center gap-2"
            :class="activeTab === 'team' ? 'border-indigo-600 text-indigo-600' : 'border-transparent text-gray-500 hover:text-gray-700'"
          >
            <span>Team</span>
            <span
              v-if="counts.pending > 0"
              class="px-1.5 py-0.5 text-xs font-bold rounded-full bg-amber-500 text-white"
              title="Pending invitations"
            >
              {{ counts.pending }}
            </span>
            <span
              v-else-if="counts.total > 0"
              class="px-2 py-0.5 text-xs rounded-full bg-gray-100 text-gray-600"
            >
              {{ counts.total }}
            </span>
          </button>

          <button
            @click="switchTab('jobs')"
            class="pb-3 text-sm font-medium border-b-2 transition-colors"
            :class="activeTab === 'jobs' ? 'border-indigo-600 text-indigo-600' : 'border-transparent text-gray-500 hover:text-gray-700'"
          >
            Jobs
          </button>

          <button
            @click="switchTab('settings')"
            class="pb-3 text-sm font-medium border-b-2 transition-colors"
            :class="activeTab === 'settings' ? 'border-indigo-600 text-indigo-600' : 'border-transparent text-gray-500 hover:text-gray-700'"
          >
            Settings
          </button>
        </nav>
      </div>

      <!-- Tab Content Area -->
      <div class="mt-6">
        <!-- OVERVIEW TAB -->
        <div v-if="activeTab === 'overview'" class="bg-white rounded-lg shadow p-6">
          <h2 class="text-lg font-semibold mb-4 text-gray-900">About</h2>
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

        <!-- TEAM TAB -->
        <div v-else-if="activeTab === 'team'" class="space-y-6">
          <!-- Pending Invites Alert Banner (if any) -->
          <div
            v-if="isOrgAdmin && counts.pending > 0"
            class="p-4 bg-amber-50 border border-amber-200 rounded-lg flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3 shadow-xs"
          >
            <div class="flex items-center gap-3">
              <span class="flex h-3 w-3 relative">
                <span class="animate-ping absolute inline-flex h-full w-full rounded-full bg-amber-400 opacity-75"></span>
                <span class="relative inline-flex rounded-full h-3 w-3 bg-amber-500"></span>
              </span>
              <div>
                <p class="text-sm font-semibold text-amber-900">
                  {{ counts.pending }} pending team member invitation{{ counts.pending > 1 ? 's' : '' }}
                </p>
                <p class="text-xs text-amber-700">
                  Review and approve staff members below so they can start managing company tasks.
                </p>
              </div>
            </div>

            <button
              @click="selectedFilter = 'pending'"
              class="self-start sm:self-auto px-3 py-1.5 text-xs font-semibold rounded bg-amber-600 hover:bg-amber-700 text-white shadow-xs"
            >
              Filter Pending Invites
            </button>
          </div>

          <!-- Team Main Card -->
          <div class="bg-white rounded-lg shadow overflow-hidden">
            <!-- Team Card Header -->
            <div class="p-6 border-b border-gray-200 flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
              <div>
                <h2 class="text-lg font-bold text-gray-900">Team Members & Staff</h2>
                <p class="text-sm text-gray-500 mt-0.5">
                  Manage organization administrators, recruiters, and staff members.
                </p>
              </div>

              <div class="flex items-center gap-3">
                <button
                  v-if="isOrgAdmin"
                  @click="$router.push(`/organizations/${org.id}/invite`)"
                  class="inline-flex items-center gap-1.5 px-4 py-2 text-sm font-semibold rounded-md text-white bg-indigo-600 hover:bg-indigo-700 shadow-sm"
                >
                  <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
                  </svg>
                  Invite Member
                </button>

                <button
                  @click="fetchMembers(org.id)"
                  :disabled="loadingMembers"
                  class="p-2 text-gray-500 hover:text-gray-700 rounded-md hover:bg-gray-100 disabled:opacity-50"
                  title="Refresh team list"
                >
                  <svg
                    class="h-5 w-5"
                    :class="{ 'animate-spin': loadingMembers }"
                    fill="none"
                    viewBox="0 0 24 24"
                    stroke="currentColor"
                  >
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
                  </svg>
                </button>
              </div>
            </div>

            <!-- Filter Controls Bar -->
            <div class="px-6 py-3 bg-gray-50 border-b border-gray-200 flex flex-col sm:flex-row items-stretch sm:items-center justify-between gap-3">
              <!-- Filter Tabs -->
              <div class="flex flex-wrap items-center gap-1.5">
                <button
                  @click="selectedFilter = 'all'"
                  class="px-3 py-1.5 text-xs font-medium rounded-full transition-colors"
                  :class="selectedFilter === 'all' ? 'bg-indigo-600 text-white shadow-xs' : 'text-gray-600 hover:bg-gray-200'"
                >
                  All ({{ counts.all }})
                </button>

                <button
                  @click="selectedFilter = 'active'"
                  class="px-3 py-1.5 text-xs font-medium rounded-full transition-colors"
                  :class="selectedFilter === 'active' ? 'bg-indigo-600 text-white shadow-xs' : 'text-gray-600 hover:bg-gray-200'"
                >
                  Active Staff ({{ counts.active }})
                </button>

                <button
                  @click="selectedFilter = 'pending'"
                  class="px-3 py-1.5 text-xs font-medium rounded-full transition-colors flex items-center gap-1.5"
                  :class="selectedFilter === 'pending' ? 'bg-amber-600 text-white shadow-xs' : 'text-gray-600 hover:bg-gray-200'"
                >
                  <span>Pending Invites ({{ counts.pending }})</span>
                  <span
                    v-if="counts.pending > 0 && selectedFilter !== 'pending'"
                    class="h-2 w-2 rounded-full bg-amber-500"
                  ></span>
                </button>

                <button
                  v-if="counts.revoked > 0"
                  @click="selectedFilter = 'revoked'"
                  class="px-3 py-1.5 text-xs font-medium rounded-full transition-colors"
                  :class="selectedFilter === 'revoked' ? 'bg-indigo-600 text-white shadow-xs' : 'text-gray-600 hover:bg-gray-200'"
                >
                  Inactive / Revoked ({{ counts.revoked }})
                </button>
              </div>

              <!-- Search Input -->
              <div class="relative w-full sm:w-64">
                <input
                  v-model="searchQuery"
                  type="text"
                  placeholder="Search by name, email, role..."
                  class="w-full text-xs rounded-md border border-gray-300 pl-8 pr-7 py-1.5 focus:ring-1 focus:ring-indigo-500 bg-white"
                />
                <svg
                  class="h-4 w-4 absolute left-2.5 top-2 text-gray-400"
                  fill="none"
                  viewBox="0 0 24 24"
                  stroke="currentColor"
                >
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                </svg>
                <button
                  v-if="searchQuery"
                  @click="searchQuery = ''"
                  class="absolute right-2 top-2 text-gray-400 hover:text-gray-600 text-xs font-bold"
                >
                  ×
                </button>
              </div>
            </div>

            <!-- Error Banner -->
            <div v-if="membersError" class="p-4 bg-red-50 border-b border-red-200 text-sm text-red-700 flex items-center justify-between">
              <div>
                <strong>Error loading members:</strong> {{ membersError }}
              </div>
              <button
                @click="fetchMembers(org.id)"
                class="px-3 py-1 text-xs font-medium text-red-700 bg-red-100 hover:bg-red-200 rounded"
              >
                Retry
              </button>
            </div>

            <!-- Loading State -->
            <div v-if="loadingMembers" class="p-12 text-center text-gray-500">
              <div class="inline-block animate-spin rounded-full h-8 w-8 border-b-2 border-indigo-600 mb-2"></div>
              <p class="text-sm">Loading team members...</p>
            </div>

            <!-- Empty State -->
            <div v-else-if="filteredMembers.length === 0" class="p-12 text-center">
              <svg class="mx-auto h-12 w-12 text-gray-300" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z" />
              </svg>
              <h3 class="mt-3 text-sm font-medium text-gray-900">
                {{ searchQuery ? 'No members match your search' : 'No members found in this view' }}
              </h3>
              <p class="mt-1 text-xs text-gray-500 max-w-sm mx-auto">
                {{ searchQuery ? 'Try adjusting your search terms or filters.' : 'Get started by inviting colleagues to collaborate in this organization.' }}
              </p>
              <div class="mt-4">
                <button
                  v-if="isOrgAdmin && !searchQuery"
                  @click="$router.push(`/organizations/${org.id}/invite`)"
                  class="inline-flex items-center px-4 py-2 text-xs font-semibold rounded-md text-white bg-indigo-600 hover:bg-indigo-700 shadow-sm"
                >
                  + Invite First Team Member
                </button>
                <button
                  v-else-if="searchQuery"
                  @click="searchQuery = ''; selectedFilter = 'all'"
                  class="inline-flex items-center px-3 py-1.5 text-xs font-medium text-indigo-600 hover:underline"
                >
                  Clear search filters
                </button>
              </div>
            </div>

            <!-- Members List -->
            <ul v-else class="divide-y divide-gray-200">
              <li
                v-for="member in filteredMembers"
                :key="member.id"
                class="p-5 flex flex-col md:flex-row md:items-center md:justify-between gap-4 hover:bg-gray-50 transition-colors"
              >
                <!-- Member Info Section -->
                <div class="flex items-start gap-4 min-w-0">
                  <!-- Avatar -->
                  <div class="h-11 w-11 rounded-full bg-linear-to-tr from-indigo-500 to-purple-600 overflow-hidden shrink-0 flex items-center justify-center text-white font-bold text-sm shadow-xs">
                    <img
                      v-if="member.user?.avatar_path"
                      :src="getOrganizationImageUrl(member.user.avatar_path) || ''"
                      class="h-full w-full object-cover"
                    />
                    <span v-else>
                      {{ getMemberDisplayName(member).charAt(0).toUpperCase() }}
                    </span>
                  </div>

                  <!-- Details -->
                  <div class="min-w-0">
                    <div class="flex flex-wrap items-center gap-2">
                      <p class="font-semibold text-gray-900 text-sm truncate">
                        {{ getMemberDisplayName(member) }}
                      </p>
                      <span
                        v-if="member.user_id === auth.user?.id"
                        class="px-1.5 py-0.2 text-[10px] font-bold rounded bg-gray-100 text-gray-600"
                      >
                        (You)
                      </span>
                      <span
                        v-if="member.user_id === org.created_by"
                        class="px-1.5 py-0.2 text-[10px] font-bold rounded bg-purple-100 text-purple-700"
                        title="Organization Creator"
                      >
                        Creator
                      </span>
                    </div>

                    <p class="text-xs text-gray-500 truncate mt-0.5">
                      {{ member.user?.email || 'No email attached' }}
                    </p>

                    <div class="mt-1 flex flex-wrap items-center gap-x-3 gap-y-1 text-xs text-gray-500">
                      <span v-if="member.title" class="font-medium text-gray-700">
                        {{ member.title }}
                      </span>
                      <span v-if="member.department" class="text-gray-500">
                        • {{ member.department }}
                      </span>

                      <!-- Date information -->
                      <span v-if="member.status === 'active' && member.joined_at" class="text-gray-400">
                        • Joined {{ formatDate(member.joined_at) }}
                      </span>
                      <span v-else-if="member.status === 'pending' && member.invited_at" class="text-amber-700 font-medium">
                        • Invited {{ formatDate(member.invited_at) }}
                      </span>
                      <span v-if="member.inviter?.display_name || member.inviter?.email" class="text-gray-400">
                        by {{ member.inviter?.display_name || member.inviter?.email }}
                      </span>
                    </div>
                  </div>
                </div>

                <!-- Status & Action Controls -->
                <div class="flex flex-wrap items-center justify-between md:justify-end gap-3 shrink-0 pt-2 md:pt-0 border-t md:border-t-0 border-gray-100">
                  <!-- Badges: Role & Status -->
                  <div class="flex items-center gap-2">
                    <!-- Role badge -->
                    <span
                      class="px-2.5 py-1 text-xs font-semibold rounded-full"
                      :class="member.role === 'org_admin' ? 'bg-indigo-100 text-indigo-800' : 'bg-blue-50 text-blue-700 border border-blue-200'"
                    >
                      {{ member.role === 'org_admin' ? 'Admin' : 'Staff' }}
                    </span>

                    <!-- Status badge -->
                    <span
                      class="px-2.5 py-1 text-xs font-semibold rounded-full flex items-center gap-1.5"
                      :class="{
                        'bg-green-100 text-green-800': member.status === 'active',
                        'bg-amber-100 text-amber-800': member.status === 'pending',
                        'bg-red-100 text-red-800': member.status === 'revoked' || member.status === 'rejected'
                      }"
                    >
                      <span
                        v-if="member.status === 'pending'"
                        class="h-1.5 w-1.5 rounded-full bg-amber-500 animate-pulse"
                      ></span>
                      <span class="capitalize">{{ member.status }}</span>
                    </span>
                  </div>

                  <!-- Admin Action Controls -->
                  <div v-if="isOrgAdmin" class="flex items-center gap-1.5">
                    <!-- 1. Actions for PENDING members -->
                    <template v-if="member.status === 'pending'">
                      <button
                        :disabled="actionLoading === member.id"
                        @click="approveMember(member)"
                        class="px-2.5 py-1 text-xs font-semibold text-white bg-green-600 hover:bg-green-700 disabled:opacity-50 rounded shadow-xs transition-colors"
                        title="Approve and activate staff member"
                      >
                        {{ actionLoading === member.id ? 'Saving...' : 'Approve & Activate' }}
                      </button>

                      <button
                        :disabled="actionLoading === member.id"
                        @click="revokeMember(member)"
                        class="px-2.5 py-1 text-xs font-medium text-red-600 hover:bg-red-50 border border-red-300 disabled:opacity-50 rounded transition-colors"
                        title="Cancel this invitation"
                      >
                        Cancel Invite
                      </button>
                    </template>

                    <!-- 2. Actions for ACTIVE members -->
                    <template v-else-if="member.status === 'active'">
                      <button
                        @click="openEditModal(member)"
                        class="px-2.5 py-1 text-xs font-medium text-gray-700 hover:bg-gray-100 border border-gray-300 rounded transition-colors"
                        title="Edit title and department"
                      >
                        Edit
                      </button>

                      <!-- Change Role Button (promotes or demotes) -->
                      <button
                        v-if="member.user_id !== org.created_by"
                        :disabled="actionLoading === member.id"
                        @click="toggleRole(member)"
                        class="px-2.5 py-1 text-xs font-medium text-indigo-700 hover:bg-indigo-50 border border-indigo-200 rounded transition-colors"
                        :title="member.role === 'org_admin' ? 'Demote to Staff' : 'Promote to Admin'"
                      >
                        {{ member.role === 'org_admin' ? 'Make Staff' : 'Make Admin' }}
                      </button>

                      <!-- Remove member (cannot remove org creator or yourself if admin) -->
                      <button
                        v-if="member.user_id !== org.created_by && member.user_id !== auth.user?.id"
                        :disabled="actionLoading === member.id"
                        @click="revokeMember(member)"
                        class="px-2 py-1 text-xs font-medium text-red-600 hover:bg-red-50 border border-red-200 rounded transition-colors"
                        title="Remove member from organization"
                      >
                        Remove
                      </button>
                    </template>

                    <!-- 3. Actions for REVOKED / INACTIVE members -->
                    <template v-else-if="member.status === 'revoked' || member.status === 'rejected'">
                      <button
                        :disabled="actionLoading === member.id"
                        @click="reinviteMember(member)"
                        class="px-2.5 py-1 text-xs font-medium text-indigo-600 hover:bg-indigo-50 border border-indigo-200 rounded transition-colors"
                        title="Send new invite to this member"
                      >
                        Re-invite
                      </button>

                      <button
                        :disabled="actionLoading === member.id"
                        @click="deleteMemberRecord(member)"
                        class="px-2 py-1 text-xs font-medium text-red-600 hover:bg-red-50 border border-red-200 rounded transition-colors"
                        title="Delete member record"
                      >
                        Delete
                      </button>
                    </template>
                  </div>
                </div>
              </li>
            </ul>
          </div>
        </div>

        <!-- JOBS TAB (placeholder) -->
        <div v-else-if="activeTab === 'jobs'" class="bg-white rounded-lg shadow p-6">
          <h2 class="text-lg font-semibold mb-2 text-gray-900">Job Postings</h2>
          <p class="text-gray-600">Job management will be implemented in the next module.</p>
        </div>

        <!-- SETTINGS TAB -->
        <div v-else-if="activeTab === 'settings'" class="bg-white rounded-lg shadow p-6 space-y-6">
          <div>
            <h2 class="text-lg font-semibold mb-4 text-gray-900">Company Settings</h2>
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
                class="inline-flex items-center px-4 py-2 text-sm font-medium rounded-md text-white bg-indigo-600 hover:bg-indigo-700 shadow-sm"
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

    <!-- Quick Edit Member Modal -->
    <div
      v-if="editingMember"
      class="fixed inset-0 bg-black/40 flex items-center justify-center p-4 z-50"
    >
      <div class="bg-white rounded-lg shadow-xl max-w-md w-full p-6 space-y-4">
        <div class="flex items-center justify-between border-b pb-3">
          <h3 class="text-base font-bold text-gray-900">
            Edit Team Member Details
          </h3>
          <button @click="closeEditModal" class="text-gray-400 hover:text-gray-600 font-bold text-lg">
            ×
          </button>
        </div>

        <p class="text-xs text-gray-500">
          Updating details for <strong>{{ getMemberDisplayName(editingMember) }}</strong>
        </p>

        <div>
          <label class="block text-xs font-semibold text-gray-700 uppercase">Job Title / Position</label>
          <input
            v-model="editTitle"
            type="text"
            placeholder="e.g. Senior Recruiter, HR Director"
            class="mt-1 w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:ring-1 focus:ring-indigo-500"
          />
        </div>

        <div>
          <label class="block text-xs font-semibold text-gray-700 uppercase">Department</label>
          <input
            v-model="editDepartment"
            type="text"
            placeholder="e.g. Human Resources, Engineering"
            class="mt-1 w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:ring-1 focus:ring-indigo-500"
          />
        </div>

        <div class="flex justify-end gap-3 pt-3 border-t">
          <button
            type="button"
            @click="closeEditModal"
            class="px-4 py-2 text-xs font-medium text-gray-700 hover:bg-gray-100 rounded-md"
          >
            Cancel
          </button>
          <button
            type="button"
            :disabled="savingEdit"
            @click="saveMemberEdit"
            class="px-4 py-2 text-xs font-semibold text-white bg-indigo-600 hover:bg-indigo-700 disabled:opacity-50 rounded-md shadow-xs"
          >
            {{ savingEdit ? 'Saving...' : 'Save Changes' }}
          </button>
        </div>
      </div>
    </div>
  </div>

  <div v-else class="p-10 text-center text-red-600">
    Organization not found or you don't have access.
  </div>
</template>

<script lang="ts">
async function sendVerificationEmail() {
  alert('Verification email feature will be fully implemented next. For development you can manually set official_email_verified = true in the database.')
}
</script>
