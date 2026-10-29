<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { toast } from 'vue-sonner'
import {
  Plus,
  RefreshCw,
  Search,
  X,
  Users,
  AlertCircle,
  CheckCircle2,
  Mail,
  Shield,
  ShieldCheck,
  UserX,
  Pencil,
  Trash2,
  RotateCcw,
  Clock,
  Globe,
  MapPin,
  Briefcase,
  Loader2,
  MoreVertical,
  ChevronRight,
} from 'lucide-vue-next'
import { supabase } from '@/lib/supabase'
import { useAuthStore } from '@/stores/auth'
import { useOrganizationsStore } from '@/stores/organizations'
import { getOrganizationImageUrl } from '@/composables/useOrganizationImage'
import { getProfileImageUrl } from '@/composables/useProfileImage'
import ConfirmDialog from '@/components/ConfirmDialog.vue'

// ---------- Types ----------
type MemberStatus = 'active' | 'pending' | 'revoked' | 'rejected'
type MemberRole = 'org_admin' | 'org_staff'

interface OrgMemberUser {
  id: string
  email: string
  first_name: string | null
  last_name: string | null
  display_name: string | null
  avatar_path: string | null
}
interface OrgMemberInviter {
  id: string
  email: string
  display_name: string | null
  first_name: string | null
  last_name: string | null
}
interface OrgMember {
  id: string
  organization_id: string
  user_id: string
  role: MemberRole
  status: MemberStatus
  title: string | null
  department: string | null
  joined_at: string | null
  invited_at: string | null
  invited_by: string | null
  user: OrgMemberUser | null
  inviter: OrgMemberInviter | null
}

// ---------- Setup ----------
const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const orgStore = useOrganizationsStore()

type TabKey = 'overview' | 'team' | 'jobs' | 'settings'
const tabs: TabKey[] = ['overview', 'team', 'jobs', 'settings']

const activeTab = ref<TabKey>(((route.query.tab as TabKey) || 'overview'))

// ---------- Team state ----------
const members = ref<OrgMember[]>([])
const loadingMembers = ref(false)
const membersError = ref('')
const isOrgAdmin = ref(false)
const actionKey = ref<string | null>(null) // `${memberId}:${action}`
const selectedFilter = ref<'all' | 'active' | 'pending' | 'revoked'>('all')
const searchQuery = ref('')
const debouncedQuery = ref('')

// Debounce search
let searchTimer: number | undefined
watch(searchQuery, (q) => {
  clearTimeout(searchTimer)
  searchTimer = window.setTimeout(() => (debouncedQuery.value = q), 200)
})

// ---------- Edit modal ----------
const editingMember = ref<OrgMember | null>(null)
const editTitle = ref('')
const editDepartment = ref('')
const savingEdit = ref(false)
const editModalEl = ref<HTMLElement | null>(null)

// ---------- Confirm dialog ----------
interface ConfirmState {
  open: boolean
  title: string
  message: string
  confirmLabel?: string
  danger?: boolean
  resolve: ((v: boolean) => void) | null
}
const confirmState = ref<ConfirmState>({
  open: false,
  title: '',
  message: '',
  resolve: null,
})
function confirmDialog(opts: Omit<ConfirmState, 'open' | 'resolve'>): Promise<boolean> {
  return new Promise((resolve) => {
    confirmState.value = { open: true, resolve, ...opts }
  })
}
function resolveConfirm(value: boolean) {
  confirmState.value.resolve?.(value)
  confirmState.value.open = false
  confirmState.value.resolve = null
}

// ---------- Misc state ----------
const org = computed(() => orgStore.currentOrganization)
const sendingVerification = ref(false)
const verificationMessage = ref('')

// ---------- Route/tab sync ----------
watch(activeTab, (newTab) => {
  if (route.query.tab !== newTab) {
    router.replace({ query: { ...route.query, tab: newTab } })
  }
})

watch(
  () => route.query.tab,
  (newTab) => {
    if (newTab && tabs.includes(newTab as TabKey)) {
      activeTab.value = newTab as TabKey
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
  if (activeTab.value === 'team') await fetchMembers(id)
})

onBeforeUnmount(() => {
  document.body.style.overflow = ''
})

// ---------- Permissions ----------
async function checkIfOrgAdmin(orgId: string) {
  if (!auth.user) {
    isOrgAdmin.value = false
    return
  }
  if (org.value && org.value.created_by === auth.user.id) {
    isOrgAdmin.value = true
    return
  }
  if (auth.user.role === 'super_admin') {
    isOrgAdmin.value = true
    return
  }
  const { data } = await supabase
    .from('organization_members')
    .select('role, status')
    .eq('organization_id', orgId)
    .eq('user_id', auth.user.id)
    .eq('status', 'active')
    .maybeSingle()
  isOrgAdmin.value = data?.role === 'org_admin'
}

// ---------- Fetch ----------
async function fetchMembers(orgId: string) {
  if (!orgId) return
  loadingMembers.value = true
  membersError.value = ''
  try {
    let { data, error } = await supabase
      .from('organization_members')
      .select(`
        id, organization_id, user_id, role, status, title, department,
        joined_at, invited_at, invited_by,
        user:users!user_id ( id, email, first_name, last_name, display_name, avatar_path ),
        inviter:users!invited_by ( id, email, display_name, first_name, last_name )
      `)
      .eq('organization_id', orgId)
      .order('created_at', { ascending: true })

    if (error) {
      console.warn('FK embed query failed, falling back:', error)
      const { data: rawMembers, error: rawError } = await supabase
        .from('organization_members')
        .select('*')
        .eq('organization_id', orgId)
        .order('created_at', { ascending: true })
      if (rawError) throw rawError

      if (rawMembers && rawMembers.length > 0) {
        const userIds = [...new Set(rawMembers.map((m: any) => m.user_id).filter(Boolean))]
        const inviterIds = [...new Set(rawMembers.map((m: any) => m.invited_by).filter(Boolean))]
        const allIds = [...new Set([...userIds, ...inviterIds])]
        let usersMap = new Map<string, any>()
        if (allIds.length > 0) {
          const { data: usersData } = await supabase
            .from('users')
            .select('id, email, first_name, last_name, display_name, avatar_path')
            .in('id', allIds)
          usersMap = new Map((usersData || []).map((u: any) => [u.id, u]))
        }
        data = rawMembers.map((m: any) => ({
          ...m,
          user: usersMap.get(m.user_id) || null,
          inviter: usersMap.get(m.invited_by) || null,
        }))
      } else {
        data = []
      }
    }
    members.value = (data || []) as unknown as OrgMember[]
  } catch (err: any) {
    console.error('Error fetching members:', err)
    membersError.value = err.message || 'Failed to load team members'
    toast.error(membersError.value)
  } finally {
    loadingMembers.value = false
  }
}

// ---------- Derived ----------
const counts = computed(() => {
  const all = members.value.length
  const active = members.value.filter((m) => m.status === 'active').length
  const pending = members.value.filter((m) => m.status === 'pending').length
  const revoked = members.value.filter((m) => m.status === 'revoked' || m.status === 'rejected').length
  return { all, active, pending, revoked, total: all }
})

const filteredMembers = computed<OrgMember[]>(() => {
  let list = members.value
  if (selectedFilter.value === 'active') list = list.filter((m) => m.status === 'active')
  else if (selectedFilter.value === 'pending') list = list.filter((m) => m.status === 'pending')
  else if (selectedFilter.value === 'revoked')
    list = list.filter((m) => m.status === 'revoked' || m.status === 'rejected')

  const q = debouncedQuery.value.trim().toLowerCase()
  if (!q) return list
  return list.filter((m) => {
    const name = (m.user?.display_name || `${m.user?.first_name || ''} ${m.user?.last_name || ''}`).toLowerCase()
    const email = (m.user?.email || '').toLowerCase()
    const title = (m.title || '').toLowerCase()
    const dept = (m.department || '').toLowerCase()
    const role = (m.role || '').toLowerCase()
    return (
      name.includes(q) || email.includes(q) || title.includes(q) || dept.includes(q) || role.includes(q)
    )
  })
})

// ---------- Helpers ----------
function isBusy(member: OrgMember, action: string) {
  return actionKey.value === `${member.id}:${action}`
}
function anyBusy() {
  return actionKey.value !== null
}

const avatarPalettes = [
  'from-indigo-500 to-purple-600',
  'from-emerald-500 to-teal-600',
  'from-rose-500 to-pink-600',
  'from-amber-500 to-orange-600',
  'from-sky-500 to-blue-600',
]
function avatarGradient(name: string) {
  const hash = [...name].reduce((a, c) => a + c.charCodeAt(0), 0)
  return avatarPalettes[hash % avatarPalettes.length]
}

function getMemberDisplayName(member: OrgMember) {
  if (member.user?.display_name) return member.user.display_name
  if (member.user?.first_name || member.user?.last_name) {
    return `${member.user.first_name || ''} ${member.user.last_name || ''}`.trim()
  }
  if (member.user?.email) return member.user.email
  return 'Registered User'
}

function getUserInitial(user: OrgMemberUser | null | undefined): string {
  if (user?.first_name?.trim()) return user.first_name.trim().charAt(0).toUpperCase()
  if (user?.display_name?.trim()) return user.display_name.trim().charAt(0).toUpperCase()
  if (user?.email?.trim()) return user.email.trim().charAt(0).toUpperCase()
  return 'U'
}

function formatDate(isoString: string | null) {
  if (!isoString) return ''
  return new Date(isoString).toLocaleDateString(undefined, {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  })
}

function humanizeStatus(s: string) {
  return s.split('_').join(' ')
}

// ---------- Actions ----------
async function approveMember(member: OrgMember) {
  actionKey.value = `${member.id}:approve`
  try {
    const { error } = await supabase
      .from('organization_members')
      .update({ status: 'active', joined_at: new Date().toISOString() })
      .eq('id', member.id)
    if (error) throw error
    toast.success(`${getMemberDisplayName(member)} activated`)
    await fetchMembers(route.params.id as string)
  } catch (err: any) {
    toast.error(`Failed to approve: ${err.message}`)
  } finally {
    actionKey.value = null
  }
}

async function revokeMember(member: OrgMember) {
  const isPending = member.status === 'pending'
  const ok = await confirmDialog({
    title: isPending ? 'Cancel this invitation?' : 'Remove this member?',
    message: isPending
      ? 'The pending invite will be revoked. You can re-invite later.'
      : 'They will lose access to the organization. You can re-invite later.',
    confirmLabel: isPending ? 'Cancel Invite' : 'Remove',
    danger: true,
  })
  if (!ok) return

  actionKey.value = `${member.id}:revoke`
  try {
    const { error } = await supabase
      .from('organization_members')
      .update({
        status: 'revoked',
        revoked_at: new Date().toISOString(),
        revoked_by: auth.user?.id,
      })
      .eq('id', member.id)
    if (error) throw error
    toast.success(isPending ? 'Invitation cancelled' : 'Member removed')
    await fetchMembers(route.params.id as string)
  } catch (err: any) {
    toast.error(`Failed to revoke: ${err.message}`)
  } finally {
    actionKey.value = null
  }
}

async function reinviteMember(member: OrgMember) {
  actionKey.value = `${member.id}:reinvite`
  try {
    const { error } = await supabase
      .from('organization_members')
      .update({
        status: 'pending',
        invited_at: new Date().toISOString(),
        invited_by: auth.user?.id,
        revoked_at: null,
        revoked_by: null,
        rejection_reason: null,
      })
      .eq('id', member.id)
    if (error) throw error
    toast.success('Re-invitation sent')
    await fetchMembers(route.params.id as string)
  } catch (err: any) {
    toast.error(`Failed to re-invite: ${err.message}`)
  } finally {
    actionKey.value = null
  }
}

async function deleteMemberRecord(member: OrgMember) {
  const ok = await confirmDialog({
    title: 'Delete member record?',
    message: 'This permanently removes the record. This cannot be undone.',
    confirmLabel: 'Delete',
    danger: true,
  })
  if (!ok) return

  actionKey.value = `${member.id}:delete`
  try {
    const { error } = await supabase.from('organization_members').delete().eq('id', member.id)
    if (error) throw error
    toast.success('Record deleted')
    await fetchMembers(route.params.id as string)
  } catch (err: any) {
    toast.error(`Failed to delete: ${err.message}`)
  } finally {
    actionKey.value = null
  }
}

async function toggleRole(member: OrgMember) {
  const newRole: MemberRole = member.role === 'org_admin' ? 'org_staff' : 'org_admin'
  const roleName = newRole === 'org_admin' ? 'Administrator' : 'Staff'

  if (member.user_id === org.value?.created_by && newRole === 'org_staff') {
    toast.error('The organization creator must remain an Administrator.')
    return
  }

  const ok = await confirmDialog({
    title: `Change role to ${roleName}?`,
    message: `${getMemberDisplayName(member)} will be ${newRole === 'org_admin' ? 'promoted to administrator' : 'demoted to staff'}.`,
    confirmLabel: 'Change role',
    danger: newRole === 'org_staff',
  })
  if (!ok) return

  actionKey.value = `${member.id}:role`
  try {
    const { error } = await supabase
      .from('organization_members')
      .update({ role: newRole })
      .eq('id', member.id)
    if (error) throw error
    toast.success(`Role changed to ${roleName}`)
    await fetchMembers(route.params.id as string)
  } catch (err: any) {
    toast.error(`Failed to change role: ${err.message}`)
  } finally {
    actionKey.value = null
  }
}

// ---------- Edit modal ----------
function openEditModal(member: OrgMember) {
  editingMember.value = member
  editTitle.value = member.title || ''
  editDepartment.value = member.department || ''
  document.body.style.overflow = 'hidden'
  requestAnimationFrame(() => editModalEl.value?.focus())
}
function closeEditModal() {
  editingMember.value = null
  editTitle.value = ''
  editDepartment.value = ''
  document.body.style.overflow = ''
}
async function saveMemberEdit() {
  if (!editingMember.value) return
  savingEdit.value = true
  try {
    const { error } = await supabase
      .from('organization_members')
      .update({
        title: editTitle.value.trim() || null,
        department: editDepartment.value.trim() || null,
      })
      .eq('id', editingMember.value.id)
    if (error) throw error
    toast.success('Member details updated')
    await fetchMembers(route.params.id as string)
    closeEditModal()
  } catch (err: any) {
    toast.error(`Failed to save: ${err.message}`)
  } finally {
    savingEdit.value = false
  }
}

// ---------- Tabs ----------
function switchTab(tab: TabKey) {
  activeTab.value = tab
  if (tab === 'team' && members.value.length === 0) {
    fetchMembers((route.params.id as string) || org.value?.id || '')
  }
}

// ---------- Mobile actions menu ----------
const openMenuId = ref<string | null>(null)
function toggleMenu(id: string) {
  openMenuId.value = openMenuId.value === id ? null : id
}
function closeMenu() {
  openMenuId.value = null
}
watch(
  () => route.fullPath,
  () => closeMenu()
)
onMounted(() => document.addEventListener('click', closeMenu))
onBeforeUnmount(() => document.removeEventListener('click', closeMenu))

// ---------- Verification ----------
async function sendVerificationEmail() {
  if (!org.value || !auth.user) return
  sendingVerification.value = true
  verificationMessage.value = ''
  try {
    const token = crypto.randomUUID() + '-' + crypto.randomUUID()
    const expiresAt = new Date()
    expiresAt.setHours(expiresAt.getHours() + 24)

    await supabase
      .from('organization_email_verifications')
      .delete()
      .eq('organization_id', org.value.id)
      .is('verified_at', null)

    const { error } = await supabase.from('organization_email_verifications').insert({
      organization_id: org.value.id,
      email: org.value.official_email,
      token,
      expires_at: expiresAt.toISOString(),
      created_by: auth.user.id,
    })
    if (error) throw error

    const verifyUrl = `${window.location.origin}/organizations/verify-email?token=${token}`
    verificationMessage.value = `Verification link generated (valid 24h). Dev link: ${verifyUrl}`
    await navigator.clipboard.writeText(verifyUrl).catch(() => {})
    toast.success('Verification link copied to clipboard')
  } catch (err: any) {
    verificationMessage.value = err.message || 'Failed to generate verification'
    toast.error(verificationMessage.value)
  } finally {
    sendingVerification.value = false
  }
}
</script>

<template>
  <div v-if="orgStore.loading" class="flex flex-col items-center justify-center py-24 text-gray-500">
    <Loader2 class="h-8 w-8 animate-spin text-indigo-600" />
    <p class="mt-3 text-sm">Loading company…</p>
  </div>

  <div v-else-if="org" class="min-h-screen bg-gray-50 pb-16">
    <div class="max-w-6xl mx-auto px-4 sm:px-6 pt-6">
      <!-- Cover Banner -->
      <div class="relative h-48 sm:h-64 rounded-3xl overflow-hidden shadow-sm">
        <div class="absolute inset-0 bg-gradient-to-r from-indigo-600 via-purple-600 to-fuchsia-600"></div>
        <!-- Watermark letter fallback when cover is not uploaded -->
        <div
          v-if="!org.cover_path"
          class="absolute inset-0 flex items-center justify-end pr-8 sm:pr-16 pointer-events-none select-none opacity-20 overflow-hidden"
          aria-hidden="true"
        >
          <span class="text-9xl sm:text-[14rem] font-black text-white tracking-widest uppercase">
            {{ (org.name || 'O').charAt(0) }}
          </span>
        </div>
        <img
          v-if="org.cover_path"
          :src="getOrganizationImageUrl(org.cover_path) || ''"
          class="relative w-full h-full object-cover"
          alt=""
        />
        <div class="absolute inset-0 bg-gradient-to-t from-black/40 via-black/10 to-transparent pointer-events-none"></div>
      </div>

      <!-- Header Card & Content Container -->
      <div class="relative z-10 -mt-14 sm:-mt-16 px-2 sm:px-4">
        <!-- Header Card -->
        <div
          class="bg-white rounded-2xl shadow-lg ring-1 ring-gray-200/80 p-6 flex flex-col sm:flex-row items-start gap-6"
        >
          <!-- Organization Avatar -->
          <div
            class="h-24 w-24 sm:h-28 sm:w-28 rounded-2xl ring-4 ring-white bg-gradient-to-br from-indigo-600 to-purple-700 overflow-hidden shrink-0 shadow-md flex items-center justify-center"
          >
            <img
              v-if="org.avatar_path"
              :src="getOrganizationImageUrl(org.avatar_path) || ''"
              class="h-full w-full object-cover"
              :alt="org.name"
            />
            <div v-else class="flex h-full w-full items-center justify-center font-bold text-3xl sm:text-4xl text-white select-none">
              {{ (org.name || 'O').charAt(0).toUpperCase() }}
            </div>
          </div>

          <div class="flex-1 min-w-0">
            <div class="flex flex-col sm:flex-row sm:items-start sm:justify-between gap-3">
              <div>
                <h1 class="text-2xl sm:text-3xl font-bold text-gray-900 truncate">{{ org.name }}</h1>
                <p v-if="org.tagline" class="text-gray-600 mt-1">{{ org.tagline }}</p>
                <div class="mt-3 flex flex-wrap items-center gap-x-4 gap-y-1.5 text-sm text-gray-500">
                  <span v-if="org.industry" class="inline-flex items-center gap-1">
                    <Briefcase class="h-3.5 w-3.5" /> {{ org.industry }}
                  </span>
                  <span v-if="org.company_size" class="inline-flex items-center gap-1">
                    <Users class="h-3.5 w-3.5" /> {{ org.company_size }} employees
                  </span>
                  <span v-if="org.city" class="inline-flex items-center gap-1">
                    <MapPin class="h-3.5 w-3.5" /> {{ org.city }}
                  </span>
                  <a
                    v-if="org.website_url"
                    :href="org.website_url"
                    target="_blank"
                    rel="noopener"
                    class="inline-flex items-center gap-1 text-indigo-600 hover:underline"
                  >
                    <Globe class="h-3.5 w-3.5" /> Website
                  </a>
                </div>
              </div>

              <div class="flex items-center gap-2.5 sm:self-start">
                <span
                  class="px-3 py-1 text-xs font-semibold rounded-full capitalize"
                  :class="{
                    'bg-green-100 text-green-800': org.status === 'active',
                    'bg-yellow-100 text-yellow-800': org.status === 'pending_verification',
                    'bg-red-100 text-red-800':
                      org.status === 'suspended' || org.status === 'rejected',
                  }"
                >
                  {{ humanizeStatus(org.status) }}
                </span>

                <router-link
                  v-if="isOrgAdmin"
                  :to="`/organizations/${org.id}/edit`"
                  class="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs sm:text-sm font-medium rounded-lg text-gray-700 bg-white border border-gray-300 hover:bg-gray-50 shadow-xs transition-colors"
                >
                  <Pencil class="h-3.5 w-3.5 text-gray-500" />
                  Edit Company
                </router-link>
              </div>
            </div>
          </div>
        </div>

      <!-- Tabs -->
      <div class="mt-6 border-b border-gray-200">
        <nav class="flex gap-1 sm:gap-6 overflow-x-auto" role="tablist" aria-label="Organization sections">
          <button
            v-for="tab in tabs"
            :key="tab"
            role="tab"
            :aria-selected="activeTab === tab"
            :tabindex="activeTab === tab ? 0 : -1"
            @click="switchTab(tab)"
            class="relative pb-3 pt-1 px-2 text-sm font-medium border-b-2 transition-colors whitespace-nowrap flex items-center gap-2"
            :class="
              activeTab === tab
                ? 'border-indigo-600 text-indigo-600'
                : 'border-transparent text-gray-500 hover:text-gray-700'
            "
          >
            <span class="capitalize">{{ tab }}</span>

            <template v-if="tab === 'team'">
              <span
                v-if="counts.pending > 0"
                class="px-1.5 py-0.5 text-[10px] font-bold rounded-full bg-amber-500 text-white"
                :title="`${counts.pending} pending invitations`"
              >
                {{ counts.pending }}
              </span>
              <span
                v-else-if="counts.total > 0"
                class="px-2 py-0.5 text-[10px] rounded-full bg-gray-100 text-gray-600"
              >
                {{ counts.total }}
              </span>
            </template>
          </button>
        </nav>
      </div>

      <!-- Tab panels -->
      <div class="mt-6">
        <!-- OVERVIEW -->
        <section
          v-if="activeTab === 'overview'"
          role="tabpanel"
          class="bg-white rounded-2xl shadow-sm ring-1 ring-gray-200 p-6"
        >
          <h2 class="text-lg font-semibold mb-4 text-gray-900">About</h2>
          <p class="text-gray-700 whitespace-pre-line leading-relaxed">
            {{ org.description || 'No description provided.' }}
          </p>

          <div
            v-if="org.status === 'pending_verification'"
            class="mt-6 flex items-start gap-3 p-4 bg-yellow-50 border border-yellow-200 rounded-lg"
          >
            <AlertCircle class="h-5 w-5 text-yellow-600 shrink-0 mt-0.5" />
            <div class="flex-1">
              <h3 class="font-medium text-yellow-800">Email verification required</h3>
              <p class="text-sm text-yellow-700 mt-1">
                Verify <strong>{{ org.official_email }}</strong> to activate your company and start
                posting jobs.
              </p>
              <button
                class="mt-3 inline-flex items-center gap-1.5 px-4 py-2 text-sm font-medium rounded-md text-white bg-yellow-600 hover:bg-yellow-700"
                @click="switchTab('settings')"
              >
                Go to verification <ChevronRight class="h-4 w-4" />
              </button>
            </div>
          </div>
        </section>

        <!-- TEAM -->
        <section v-else-if="activeTab === 'team'" role="tabpanel" class="space-y-6">
          <!-- Pending alert -->
          <div
            v-if="isOrgAdmin && counts.pending > 0"
            class="p-4 bg-amber-50 border border-amber-200 rounded-xl flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3"
          >
            <div class="flex items-center gap-3">
              <span class="relative flex h-3 w-3 shrink-0">
                <span
                  class="animate-ping absolute inline-flex h-full w-full rounded-full bg-amber-400 opacity-75"
                ></span>
                <span class="relative inline-flex rounded-full h-3 w-3 bg-amber-500"></span>
              </span>
              <div>
                <p class="text-sm font-semibold text-amber-900">
                  {{ counts.pending }} pending invitation{{ counts.pending > 1 ? 's' : '' }}
                </p>
                <p class="text-xs text-amber-700">
                  Review and approve staff so they can start managing company tasks.
                </p>
              </div>
            </div>
            <button
              @click="selectedFilter = 'pending'"
              class="self-start sm:self-auto px-3 py-1.5 text-xs font-semibold rounded bg-amber-600 hover:bg-amber-700 text-white shadow-sm"
            >
              Filter pending
            </button>
          </div>

          <!-- Team card -->
          <div class="bg-white rounded-2xl shadow-sm ring-1 ring-gray-200 overflow-hidden">
            <!-- Header -->
            <div
              class="p-6 border-b border-gray-200 flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4"
            >
              <div>
                <h2 class="text-lg font-bold text-gray-900">Team Members & Staff</h2>
                <p class="text-sm text-gray-500 mt-0.5">
                  Manage administrators, recruiters, and staff.
                </p>
              </div>
              <div class="flex items-center gap-2">
                <button
                  v-if="isOrgAdmin"
                  @click="router.push(`/organizations/${org.id}/invite`)"
                  class="inline-flex items-center gap-1.5 px-4 py-2 text-sm font-semibold rounded-lg text-white bg-indigo-600 hover:bg-indigo-700 shadow-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:ring-offset-1"
                >
                  <Plus class="h-4 w-4" /> Invite Member
                </button>
                <button
                  @click="fetchMembers(org.id)"
                  :disabled="loadingMembers"
                  class="p-2 text-gray-500 hover:text-gray-700 rounded-lg hover:bg-gray-100 disabled:opacity-50 focus:outline-none focus:ring-2 focus:ring-indigo-500"
                  title="Refresh team list"
                  aria-label="Refresh team list"
                >
                  <RefreshCw class="h-5 w-5" :class="{ 'animate-spin': loadingMembers }" />
                </button>
              </div>
            </div>

            <!-- Filters -->
            <div
              class="px-6 py-3 bg-gray-50 border-b border-gray-200 flex flex-col sm:flex-row items-stretch sm:items-center justify-between gap-3"
            >
              <div class="flex flex-wrap items-center gap-1.5" role="group" aria-label="Filter members">
                <button
                  @click="selectedFilter = 'all'"
                  class="px-3 py-1.5 text-xs font-medium rounded-full transition-colors"
                  :class="
                    selectedFilter === 'all'
                      ? 'bg-indigo-600 text-white shadow-sm'
                      : 'text-gray-600 hover:bg-gray-200'
                  "
                >
                  All ({{ counts.all }})
                </button>
                <button
                  @click="selectedFilter = 'active'"
                  class="px-3 py-1.5 text-xs font-medium rounded-full transition-colors"
                  :class="
                    selectedFilter === 'active'
                      ? 'bg-indigo-600 text-white shadow-sm'
                      : 'text-gray-600 hover:bg-gray-200'
                  "
                >
                  Active ({{ counts.active }})
                </button>
                <button
                  @click="selectedFilter = 'pending'"
                  class="px-3 py-1.5 text-xs font-medium rounded-full transition-colors flex items-center gap-1.5"
                  :class="
                    selectedFilter === 'pending'
                      ? 'bg-amber-600 text-white shadow-sm'
                      : 'text-gray-600 hover:bg-gray-200'
                  "
                >
                  Pending ({{ counts.pending }})
                  <span
                    v-if="counts.pending > 0 && selectedFilter !== 'pending'"
                    class="h-2 w-2 rounded-full bg-amber-500"
                  ></span>
                </button>
                <button
                  v-if="counts.revoked > 0"
                  @click="selectedFilter = 'revoked'"
                  class="px-3 py-1.5 text-xs font-medium rounded-full transition-colors"
                  :class="
                    selectedFilter === 'revoked'
                      ? 'bg-indigo-600 text-white shadow-sm'
                      : 'text-gray-600 hover:bg-gray-200'
                  "
                >
                  Revoked ({{ counts.revoked }})
                </button>
              </div>

              <div class="relative w-full sm:w-64">
                <Search class="h-4 w-4 absolute left-2.5 top-2 text-gray-400 pointer-events-none" />
                <input
                  v-model="searchQuery"
                  type="text"
                  placeholder="Search name, email, role…"
                  aria-label="Search team members"
                  class="w-full text-xs rounded-lg border border-gray-300 pl-8 pr-8 py-1.5 focus:ring-1 focus:ring-indigo-500 bg-white"
                />
                <button
                  v-if="searchQuery"
                  @click="searchQuery = ''"
                  class="absolute right-2 top-1.5 p-0.5 text-gray-400 hover:text-gray-700 rounded"
                  aria-label="Clear search"
                >
                  <X class="h-3.5 w-3.5" />
                </button>
              </div>
            </div>

            <!-- Error -->
            <div
              v-if="membersError"
              class="p-4 bg-red-50 border-b border-red-200 text-sm text-red-700 flex items-center justify-between gap-3"
            >
              <div class="flex items-center gap-2">
                <AlertCircle class="h-4 w-4 shrink-0" />
                <span><strong>Error:</strong> {{ membersError }}</span>
              </div>
              <button
                @click="fetchMembers(org.id)"
                class="px-3 py-1 text-xs font-medium text-red-700 bg-red-100 hover:bg-red-200 rounded"
              >
                Retry
              </button>
            </div>

            <!-- Loading -->
            <div v-if="loadingMembers" class="p-12 text-center">
              <Loader2 class="inline-block h-8 w-8 animate-spin text-indigo-600" />
              <p class="mt-2 text-sm text-gray-500">Loading team members…</p>
            </div>

            <!-- Empty -->
            <div v-else-if="filteredMembers.length === 0" class="p-12 text-center">
              <Users class="mx-auto h-12 w-12 text-gray-300" />
              <h3 class="mt-3 text-sm font-medium text-gray-900">
                {{ searchQuery ? 'No members match your search' : 'No members in this view' }}
              </h3>
              <p class="mt-1 text-xs text-gray-500 max-w-sm mx-auto">
                {{
                  searchQuery
                    ? 'Try adjusting your search or filters.'
                    : 'Get started by inviting colleagues to collaborate.'
                }}
              </p>
              <div class="mt-4">
                <button
                  v-if="isOrgAdmin && !searchQuery"
                  @click="router.push(`/organizations/${org.id}/invite`)"
                  class="inline-flex items-center gap-1.5 px-4 py-2 text-xs font-semibold rounded-md text-white bg-indigo-600 hover:bg-indigo-700 shadow-sm"
                >
                  <Plus class="h-4 w-4" /> Invite first team member
                </button>
                <button
                  v-else-if="searchQuery"
                  @click="
                    searchQuery = '';
                    selectedFilter = 'all'
                  "
                  class="inline-flex items-center gap-1 px-3 py-1.5 text-xs font-medium text-indigo-600 hover:underline"
                >
                  Clear filters
                </button>
              </div>
            </div>

            <!-- Members list -->
            <ul v-else class="divide-y divide-gray-100">
              <li
                v-for="member in filteredMembers"
                :key="member.id"
                class="p-5 hover:bg-gray-50/70 transition-colors"
              >
                <div class="flex flex-col md:flex-row md:items-center md:justify-between gap-4">
                  <!-- Info -->
                  <div class="flex items-start gap-4 min-w-0">
                    <div
                      class="h-11 w-11 rounded-full bg-linear-to-tr overflow-hidden shrink-0 flex items-center justify-center text-white font-bold text-sm shadow-sm"
                      :class="avatarGradient(getMemberDisplayName(member))"
                    >
                      <img
                        v-if="member.user?.avatar_path"
                        :src="getProfileImageUrl(member.user.avatar_path) || ''"
                        class="h-full w-full object-cover"
                        :alt="getMemberDisplayName(member)"
                      />
                      <span v-else>{{ getUserInitial(member.user) }}</span>
                    </div>

                    <div class="min-w-0">
                      <div class="flex flex-wrap items-center gap-2">
                        <p class="font-semibold text-gray-900 text-sm truncate">
                          {{ getMemberDisplayName(member) }}
                        </p>
                        <span
                          v-if="member.user_id === auth.user?.id"
                          class="px-1.5 py-0.5 text-[10px] font-bold rounded bg-gray-100 text-gray-600 leading-none"
                        >
                          You
                        </span>
                        <span
                          v-if="member.user_id === org.created_by"
                          class="inline-flex items-center gap-1 px-1.5 py-0.5 text-[10px] font-bold rounded bg-purple-100 text-purple-700 leading-none"
                          title="Organization Creator"
                        >
                          <ShieldCheck class="h-2.5 w-2.5" /> Creator
                        </span>
                      </div>

                      <p class="text-xs text-gray-500 truncate mt-0.5 flex items-center gap-1">
                        <Mail class="h-3 w-3 shrink-0" />
                        {{ member.user?.email || 'No email attached' }}
                      </p>

                      <div class="mt-1 flex flex-wrap items-center gap-x-3 gap-y-1 text-xs text-gray-500">
                        <span v-if="member.title" class="font-medium text-gray-700">
                          {{ member.title }}
                        </span>
                        <span v-if="member.department">• {{ member.department }}</span>
                        <span
                          v-if="member.status === 'active' && member.joined_at"
                          class="inline-flex items-center gap-1 text-gray-400"
                        >
                          <Clock class="h-3 w-3" /> Joined {{ formatDate(member.joined_at) }}
                        </span>
                        <span
                          v-else-if="member.status === 'pending' && member.invited_at"
                          class="inline-flex items-center gap-1 text-amber-700 font-medium"
                        >
                          <Clock class="h-3 w-3" /> Invited {{ formatDate(member.invited_at) }}
                        </span>
                        <span
                          v-if="member.inviter?.display_name || member.inviter?.email"
                          class="text-gray-400"
                        >
                          by {{ member.inviter?.display_name || member.inviter?.email }}
                        </span>
                      </div>
                    </div>
                  </div>

                  <!-- Controls -->
                  <div
                    class="flex flex-wrap items-center justify-between md:justify-end gap-2 shrink-0 pt-2 md:pt-0 border-t md:border-t-0 border-gray-100"
                  >
                    <div class="flex items-center gap-2">
                      <span
                        class="inline-flex items-center gap-1 px-2.5 py-1 text-xs font-semibold rounded-full"
                        :class="
                          member.role === 'org_admin'
                            ? 'bg-indigo-100 text-indigo-800'
                            : 'bg-blue-50 text-blue-700 border border-blue-200'
                        "
                      >
                        <Shield v-if="member.role === 'org_admin'" class="h-3 w-3" />
                        {{ member.role === 'org_admin' ? 'Admin' : 'Staff' }}
                      </span>

                      <span
                        class="px-2.5 py-1 text-xs font-semibold rounded-full flex items-center gap-1.5 capitalize"
                        :class="{
                          'bg-green-100 text-green-800': member.status === 'active',
                          'bg-amber-100 text-amber-800': member.status === 'pending',
                          'bg-red-100 text-red-800':
                            member.status === 'revoked' || member.status === 'rejected',
                        }"
                      >
                        <span
                          v-if="member.status === 'pending'"
                          class="h-1.5 w-1.5 rounded-full bg-amber-500 animate-pulse"
                          aria-hidden="true"
                        ></span>
                        {{ member.status }}
                      </span>
                    </div>

                    <!-- Desktop actions -->
                    <div v-if="isOrgAdmin" class="hidden md:flex items-center gap-1.5">
                      <template v-if="member.status === 'pending'">
                        <button
                          :disabled="anyBusy()"
                          @click="approveMember(member)"
                          class="inline-flex items-center gap-1 px-2.5 py-1 text-xs font-semibold text-white bg-green-600 hover:bg-green-700 disabled:opacity-50 rounded-md shadow-sm"
                        >
                          <Loader2 v-if="isBusy(member, 'approve')" class="h-3 w-3 animate-spin" />
                          <CheckCircle2 v-else class="h-3 w-3" />
                          {{ isBusy(member, 'approve') ? 'Approving…' : 'Approve' }}
                        </button>
                        <button
                          :disabled="anyBusy()"
                          @click="revokeMember(member)"
                          class="inline-flex items-center gap-1 px-2.5 py-1 text-xs font-medium text-red-600 hover:bg-red-50 border border-red-300 disabled:opacity-50 rounded-md"
                        >
                          <X class="h-3 w-3" /> Cancel
                        </button>
                      </template>

                      <template v-else-if="member.status === 'active'">
                        <button
                          @click="openEditModal(member)"
                          class="inline-flex items-center gap-1 px-2.5 py-1 text-xs font-medium text-gray-700 hover:bg-gray-100 border border-gray-300 rounded-md"
                        >
                          <Pencil class="h-3 w-3" /> Edit
                        </button>
                        <button
                          v-if="member.user_id !== org.created_by"
                          :disabled="anyBusy()"
                          @click="toggleRole(member)"
                          class="inline-flex items-center gap-1 px-2.5 py-1 text-xs font-medium text-indigo-700 hover:bg-indigo-50 border border-indigo-200 disabled:opacity-50 rounded-md"
                        >
                          <Loader2 v-if="isBusy(member, 'role')" class="h-3 w-3 animate-spin" />
                          {{ member.role === 'org_admin' ? 'Make Staff' : 'Make Admin' }}
                        </button>
                        <button
                          v-if="
                            member.user_id !== org.created_by && member.user_id !== auth.user?.id
                          "
                          :disabled="anyBusy()"
                          @click="revokeMember(member)"
                          class="inline-flex items-center gap-1 px-2 py-1 text-xs font-medium text-red-600 hover:bg-red-50 border border-red-200 disabled:opacity-50 rounded-md"
                        >
                          <UserX class="h-3 w-3" /> Remove
                        </button>
                      </template>

                      <template v-else-if="member.status === 'revoked' || member.status === 'rejected'">
                        <button
                          :disabled="anyBusy()"
                          @click="reinviteMember(member)"
                          class="inline-flex items-center gap-1 px-2.5 py-1 text-xs font-medium text-indigo-600 hover:bg-indigo-50 border border-indigo-200 disabled:opacity-50 rounded-md"
                        >
                          <Loader2 v-if="isBusy(member, 'reinvite')" class="h-3 w-3 animate-spin" />
                          <RotateCcw v-else class="h-3 w-3" /> Re-invite
                        </button>
                        <button
                          :disabled="anyBusy()"
                          @click="deleteMemberRecord(member)"
                          class="inline-flex items-center gap-1 px-2 py-1 text-xs font-medium text-red-600 hover:bg-red-50 border border-red-200 disabled:opacity-50 rounded-md"
                        >
                          <Trash2 class="h-3 w-3" /> Delete
                        </button>
                      </template>
                    </div>

                    <!-- Mobile overflow menu -->
                    <div v-if="isOrgAdmin" class="relative md:hidden">
                      <button
                        @click.stop="toggleMenu(member.id)"
                        class="p-1.5 text-gray-500 hover:text-gray-800 hover:bg-gray-100 rounded-md"
                        aria-label="Member actions"
                        :aria-expanded="openMenuId === member.id"
                      >
                        <MoreVertical class="h-4 w-4" />
                      </button>
                      <div
                        v-if="openMenuId === member.id"
                        @click.stop
                        class="absolute right-0 mt-1 w-40 bg-white rounded-lg shadow-lg ring-1 ring-gray-200 py-1 z-20"
                      >
                        <template v-if="member.status === 'pending'">
                          <button
                            @click="
                              closeMenu();
                              approveMember(member)
                            "
                            class="w-full text-left px-3 py-2 text-xs hover:bg-gray-50 text-green-700 font-medium"
                          >
                            Approve & activate
                          </button>
                          <button
                            @click="
                              closeMenu();
                              revokeMember(member)
                            "
                            class="w-full text-left px-3 py-2 text-xs hover:bg-gray-50 text-red-600"
                          >
                            Cancel invite
                          </button>
                        </template>
                        <template v-else-if="member.status === 'active'">
                          <button
                            @click="
                              closeMenu();
                              openEditModal(member)
                            "
                            class="w-full text-left px-3 py-2 text-xs hover:bg-gray-50 text-gray-700"
                          >
                            Edit details
                          </button>
                          <button
                            v-if="member.user_id !== org.created_by"
                            @click="
                              closeMenu();
                              toggleRole(member)
                            "
                            class="w-full text-left px-3 py-2 text-xs hover:bg-gray-50 text-indigo-700"
                          >
                            {{ member.role === 'org_admin' ? 'Make Staff' : 'Make Admin' }}
                          </button>
                          <button
                            v-if="
                              member.user_id !== org.created_by &&
                              member.user_id !== auth.user?.id
                            "
                            @click="
                              closeMenu();
                              revokeMember(member)
                            "
                            class="w-full text-left px-3 py-2 text-xs hover:bg-gray-50 text-red-600"
                          >
                            Remove
                          </button>
                        </template>
                        <template v-else>
                          <button
                            @click="
                              closeMenu();
                              reinviteMember(member)
                            "
                            class="w-full text-left px-3 py-2 text-xs hover:bg-gray-50 text-indigo-600"
                          >
                            Re-invite
                          </button>
                          <button
                            @click="
                              closeMenu();
                              deleteMemberRecord(member)
                            "
                            class="w-full text-left px-3 py-2 text-xs hover:bg-gray-50 text-red-600"
                          >
                            Delete
                          </button>
                        </template>
                      </div>
                    </div>
                  </div>
                </div>
              </li>
            </ul>
          </div>
        </section>

        <!-- JOBS -->
        <section
          v-else-if="activeTab === 'jobs'"
          role="tabpanel"
          class="bg-white rounded-2xl shadow-sm ring-1 ring-gray-200 p-6"
        >
          <h2 class="text-lg font-semibold mb-2 text-gray-900">Job Postings</h2>
          <p class="text-gray-600">Job management will be implemented in the next module.</p>
        </section>

        <!-- SETTINGS -->
        <section
          v-else-if="activeTab === 'settings'"
          role="tabpanel"
          class="bg-white rounded-2xl shadow-sm ring-1 ring-gray-200 p-6 space-y-6"
        >
          <h2 class="text-lg font-semibold text-gray-900">Company Settings</h2>

          <div class="border border-gray-200 rounded-xl p-5">
            <h3 class="font-medium text-gray-900">Official Email Verification</h3>
            <p class="text-sm text-gray-600 mt-1">
              Current official email: <strong>{{ org.official_email }}</strong>
            </p>

            <div v-if="org.official_email_verified" class="mt-3 flex items-center gap-2 text-green-700">
              <CheckCircle2 class="h-5 w-5" />
              <span class="font-medium text-sm">Official email verified</span>
            </div>

            <div v-else class="mt-4 space-y-3">
              <p class="text-sm text-yellow-700 flex items-start gap-2">
                <AlertCircle class="h-4 w-4 mt-0.5 shrink-0" />
                <span>This email is not yet verified. Verification is required to post jobs.</span>
              </p>
              <button
                :disabled="sendingVerification"
                class="inline-flex items-center gap-1.5 px-4 py-2 text-sm font-medium rounded-md text-white bg-indigo-600 hover:bg-indigo-700 disabled:opacity-50"
                @click="sendVerificationEmail"
              >
                <Loader2 v-if="sendingVerification" class="h-4 w-4 animate-spin" />
                <Mail v-else class="h-4 w-4" />
                {{ sendingVerification ? 'Generating…' : 'Send Verification Email' }}
              </button>
              <p v-if="verificationMessage" class="text-xs text-gray-600 break-all">
                {{ verificationMessage }}
              </p>
            </div>
          </div>

          <!-- Organization Profile Edit Section -->
          <div v-if="isOrgAdmin" class="border border-gray-200 rounded-xl p-5 flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
            <div>
              <h3 class="font-medium text-gray-900">Organization Profile Details</h3>
              <p class="text-sm text-gray-600 mt-1">
                Update your company's name, logos, tagline, about, contact info, office locations, and social presence.
              </p>
            </div>
            <router-link
              :to="`/organizations/${org.id}/edit`"
              class="inline-flex items-center gap-1.5 px-4 py-2 text-sm font-medium rounded-lg text-white bg-indigo-600 hover:bg-indigo-700 shadow-xs transition-colors whitespace-nowrap"
            >
              <Pencil class="h-4 w-4" />
              Edit Organization
            </router-link>
          </div>
        </section>
      </div>
    </div>
  </div>

    <!-- Edit modal -->
    <Teleport to="body">
      <Transition
        enter-active-class="transition ease-out duration-150"
        enter-from-class="opacity-0"
        enter-to-class="opacity-100"
        leave-active-class="transition ease-in duration-100"
        leave-from-class="opacity-100"
        leave-to-class="opacity-0"
      >
        <div
          v-if="editingMember"
          class="fixed inset-0 bg-black/50 backdrop-blur-sm flex items-center justify-center p-4 z-50"
          role="dialog"
          aria-modal="true"
          aria-labelledby="edit-modal-title"
          @click.self="closeEditModal"
          @keydown.esc="closeEditModal"
        >
          <form
            @submit.prevent="saveMemberEdit"
            ref="editModalEl"
            tabindex="-1"
            class="bg-white rounded-2xl shadow-2xl max-w-md w-full p-6 space-y-4 focus:outline-none"
          >
            <div class="flex items-center justify-between border-b pb-3">
              <h3 id="edit-modal-title" class="text-base font-bold text-gray-900">
                Edit Team Member
              </h3>
              <button
                type="button"
                @click="closeEditModal"
                class="p-1 text-gray-400 hover:text-gray-700 rounded"
                aria-label="Close"
              >
                <X class="h-5 w-5" />
              </button>
            </div>

            <p class="text-xs text-gray-500">
              Updating details for <strong>{{ getMemberDisplayName(editingMember) }}</strong>
            </p>

            <div>
              <label class="block text-xs font-semibold text-gray-700 uppercase mb-1">
                Job Title / Position
              </label>
              <input
                v-model="editTitle"
                type="text"
                placeholder="e.g. Senior Recruiter, HR Director"
                class="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:ring-1 focus:ring-indigo-500"
              />
            </div>

            <div>
              <label class="block text-xs font-semibold text-gray-700 uppercase mb-1">
                Department
              </label>
              <input
                v-model="editDepartment"
                type="text"
                placeholder="e.g. Human Resources, Engineering"
                class="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:ring-1 focus:ring-indigo-500"
              />
            </div>

            <div class="flex justify-end gap-2 pt-3 border-t">
              <button
                type="button"
                @click="closeEditModal"
                class="px-4 py-2 text-xs font-medium text-gray-700 hover:bg-gray-100 rounded-md"
              >
                Cancel
              </button>
              <button
                type="submit"
                :disabled="savingEdit"
                class="inline-flex items-center gap-1.5 px-4 py-2 text-xs font-semibold text-white bg-indigo-600 hover:bg-indigo-700 disabled:opacity-50 rounded-md shadow-sm"
              >
                <Loader2 v-if="savingEdit" class="h-3 w-3 animate-spin" />
                {{ savingEdit ? 'Saving…' : 'Save Changes' }}
              </button>
            </div>
          </form>
        </div>
      </Transition>
    </Teleport>

    <!-- Confirm dialog -->
    <ConfirmDialog
      :open="confirmState.open"
      :title="confirmState.title"
      :message="confirmState.message"
      :confirm-label="confirmState.confirmLabel"
      :danger="confirmState.danger"
      @confirm="resolveConfirm(true)"
      @cancel="resolveConfirm(false)"
    />
  </div>

  <div v-else class="p-10 text-center text-red-600">
    Organization not found or you don't have access.
  </div>
</template>