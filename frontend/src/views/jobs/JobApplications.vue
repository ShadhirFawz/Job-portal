<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useJobsStore } from '@/stores/jobs'
import { applicationService } from '@/services/application.service'
import { organizationService } from '@/services/organization.service'
import { getProfileImageUrl } from '@/composables/useProfileImage'
import type {
  JobApplication,
  ApplicationStatus,
  ApplicationStatusFilter,
} from '@/types/application'
import {
  ArrowLeft,
  Briefcase,
  Mail,
  MapPin,
  Phone,
  Search,
  ShieldAlert,
  User,
  ChevronDown,
  ChevronUp,
  ExternalLink,
} from 'lucide-vue-next'
import { toast } from 'vue-sonner'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const jobsStore = useJobsStore()

const jobId = route.params.id as string

const loading = ref(true)
const authorized = ref(false)
const applications = ref<JobApplication[]>([])
const statusFilter = ref<ApplicationStatusFilter>('all')
const searchQuery = ref('')
const actionLoading = ref<string | null>(null)
const expandedCoverLetters = ref<Record<string, boolean>>({})

const job = computed(() => jobsStore.currentJob)

const filterTabs: Array<{ label: string; value: ApplicationStatusFilter }> = [
  { label: 'All', value: 'all' },
  { label: 'Applied', value: 'applied' },
  { label: 'Under Review', value: 'under_review' },
  { label: 'Shortlisted', value: 'shortlisted' },
  { label: 'Interview', value: 'interview' },
  { label: 'Offered', value: 'offered' },
  { label: 'Hired', value: 'hired' },
  { label: 'Rejected', value: 'rejected' },
]

onMounted(async () => {
  loading.value = true
  try {
    // 1. Fetch job
    await jobsStore.fetchJob(jobId)

    if (!jobsStore.currentJob) {
      toast.error('Job not found')
      router.replace('/')
      return
    }

    // 2. Check authorization: only super_admin, job creator, or org_admin/org_staff of this company
    if (!auth.user) {
      toast.error('Please log in to view job applications.')
      router.replace('/login')
      return
    }

    const isAllowed = await organizationService.checkIsOrgAdminOrStaff(
      jobsStore.currentJob.organization_id,
      auth.user.id,
      auth.user.role,
      jobsStore.currentJob.created_by
    )

    if (!isAllowed) {
      authorized.value = false
      toast.error('You do not have permission to view applications for this company.')
      return
    }

    authorized.value = true

    // 3. Fetch applications
    await loadApplications()
  } catch (err: any) {
    console.error('Error loading applications:', err)
    toast.error(err.message || 'Failed to load applications')
  } finally {
    loading.value = false
  }
})

async function loadApplications() {
  try {
    applications.value = await applicationService.fetchApplicationsForJob(jobId)
  } catch (err: any) {
    toast.error('Failed to load applications list')
  }
}

// Filtered and searched applications
const filteredApplications = computed(() => {
  let list = applications.value

  // Status tab filter
  if (statusFilter.value !== 'all') {
    list = list.filter((a) => a.status === statusFilter.value)
  }

  // Search query filter (name, email, headline)
  const query = searchQuery.value.trim().toLowerCase()
  if (query) {
    list = list.filter((a) => {
      const u = a.user
      const name = `${u?.first_name || ''} ${u?.last_name || ''} ${u?.display_name || ''}`.toLowerCase()
      const email = (u?.email || '').toLowerCase()
      const headline = (u?.headline || u?.current_job_title || '').toLowerCase()
      return name.includes(query) || email.includes(query) || headline.includes(query)
    })
  }

  return list
})

// Counts per status
const statusCounts = computed(() => {
  const counts: Record<string, number> = { all: applications.value.length }
  for (const a of applications.value) {
    counts[a.status] = (counts[a.status] || 0) + 1
  }
  return counts
})

async function handleUpdateStatus(
  application: JobApplication,
  newStatus: ApplicationStatus
) {
  actionLoading.value = application.id
  try {
    await applicationService.updateApplicationStatus(application.id, newStatus)
    application.status = newStatus
    toast.success(`Application updated to "${newStatus.replace('_', ' ')}"`)
  } catch (err: any) {
    toast.error(err.message || 'Failed to update application status')
  } finally {
    actionLoading.value = null
  }
}

function toggleCoverLetter(appId: string) {
  expandedCoverLetters.value[appId] = !expandedCoverLetters.value[appId]
}

function getApplicantName(user?: JobApplication['user']): string {
  if (!user) return 'Applicant'
  return (
    user.display_name ||
    `${user.first_name || ''} ${user.last_name || ''}`.trim() ||
    user.email ||
    'Applicant'
  )
}

function getApplicantInitial(user?: JobApplication['user']): string {
  const name = getApplicantName(user)
  return name.charAt(0).toUpperCase()
}

function formatAppliedDate(dateString: string): string {
  if (!dateString) return ''
  const date = new Date(dateString)
  return date.toLocaleDateString(undefined, {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  })
}
</script>

<template>
  <div class="min-h-screen bg-page py-6 sm:py-10 px-3 sm:px-6 lg:px-8">
    <div class="max-w-6xl mx-auto space-y-6">
      
      <!-- Back Navigation & Organization Link -->
      <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-2">
        <button
          type="button"
          @click="router.push(`/jobs/${jobId}`)"
          class="inline-flex items-center gap-1.5 text-xs sm:text-sm font-medium text-text-muted hover:text-text-strong transition focus:outline-none focus:ring-2 focus:ring-primary/20 rounded-md"
        >
          <ArrowLeft class="h-4 w-4" /> Back to Job Details
        </button>

        <router-link
          v-if="job?.organization_id"
          :to="`/organizations/${job.organization_id}`"
          class="text-xs font-semibold text-primary hover:underline inline-flex items-center gap-1 self-start sm:self-auto"
        >
          {{ job.organization?.name || 'Organization' }} Dashboard
          <ExternalLink class="h-3 w-3" />
        </router-link>
      </div>

      <!-- Loading State -->
      <div v-if="loading" class="bg-card rounded-xl sm:rounded-2xl shadow-sm border border-border p-12 sm:p-16 text-center">
        <div class="h-8 w-8 animate-spin rounded-full border-2 border-primary border-t-transparent mx-auto mb-3"></div>
        <p class="text-xs sm:text-sm text-text-muted">Loading job applications...</p>
      </div>

      <!-- Unauthorized State -->
      <div
        v-else-if="!authorized"
        class="bg-card rounded-xl sm:rounded-2xl shadow-sm border border-danger-border p-8 sm:p-12 text-center space-y-4"
      >
        <div class="h-12 w-12 rounded-full bg-danger-bg text-danger flex items-center justify-center mx-auto">
          <ShieldAlert class="h-6 w-6" />
        </div>
        <h2 class="text-lg sm:text-xl font-bold text-text-strong">Access Restricted</h2>
        <p class="text-xs sm:text-sm text-text-muted max-w-md mx-auto leading-relaxed">
          Viewing applications is restricted to OrgAdmin and OrgStaff members of this organization.
        </p>
        <button
          type="button"
          @click="router.push(`/jobs/${jobId}`)"
          class="inline-flex items-center px-4 py-2 text-xs font-semibold rounded-lg text-white bg-primary hover:bg-primary-hover transition shadow-sm"
        >
          Return to Job Details
        </button>
      </div>

      <!-- Main Content -->
      <div v-else class="space-y-6">
        
        <!-- Top Job Summary Card -->
        <div class="bg-card rounded-xl sm:rounded-2xl shadow-sm border border-border p-4 sm:p-6 lg:p-8">
          <div class="flex flex-col lg:flex-row lg:items-center justify-between gap-4 sm:gap-6">
            <div class="space-y-1.5 min-w-0">
              <div class="flex items-center gap-2 text-xs font-semibold uppercase tracking-wider text-primary">
                <Briefcase class="h-3.5 w-3.5 flex-shrink-0" />
                <span>Candidate Pipeline</span>
              </div>
              <h1 class="text-xl sm:text-2xl lg:text-3xl font-bold text-text-strong tracking-tight truncate">
                {{ job?.title }}
              </h1>
              <p class="text-xs sm:text-sm text-text-secondary flex flex-wrap items-center gap-1.5">
                <span class="font-medium">{{ job?.organization?.name }}</span>
                <span>•</span>
                <span class="capitalize">{{ job?.job_type?.replace('_', ' ') }}</span>
                <span v-if="job?.city">•</span>
                <span v-if="job?.city">{{ job.city }}</span>
              </p>
            </div>

            <!-- Stats Badge -->
            <div class="flex items-center self-start lg:self-auto">
              <div class="bg-primary-light border border-primary/30 rounded-xl px-4 py-3 text-center min-w-[120px]">
                <span class="block text-xl sm:text-2xl font-bold text-primary">
                  {{ applications.length }}
                </span>
                <span class="text-[11px] sm:text-xs font-semibold text-primary uppercase tracking-wide">Total Applicants</span>
              </div>
            </div>
          </div>

          <!-- Search & Filter Controls -->
          <div class="mt-6 pt-5 border-t border-border flex flex-col lg:flex-row items-stretch lg:items-center justify-between gap-4">
            <!-- Search bar (constrained and responsive) -->
            <div class="relative w-full lg:w-76 flex-shrink-0">
              <Search class="absolute left-3.5 top-1/2 -translate-y-1/2 h-4 w-4 text-text-subtle pointer-events-none" />
              <input
                v-model="searchQuery"
                type="text"
                placeholder="Search candidates by name, email, or headline..."
                class="w-full pl-10 pr-4 py-2.5 text-xs sm:text-sm rounded-xl border border-border bg-card text-text-strong placeholder:text-text-subtle focus:border-primary focus:ring-2 focus:ring-primary/25 shadow-sm transition"
              />
            </div>

            <!-- Status Tabs (Horizontal Scrollable Layout) -->
            <div class="w-full lg:w-auto overflow-x-auto pb-1 lg:pb-0 [-ms-overflow-style:none] [scrollbar-width:none] [&::-webkit-scrollbar]:hidden">
              <div class="flex items-center gap-1.5 min-w-max">
                <button
                  v-for="tab in filterTabs"
                  :key="tab.value"
                  type="button"
                  @click="statusFilter = tab.value"
                  class="px-3 py-1.5 text-xs font-semibold rounded-lg transition whitespace-nowrap flex-shrink-0"
                  :class="
                    statusFilter === tab.value
                      ? 'bg-primary text-white shadow-sm'
                      : 'bg-surface-raised text-text-secondary hover:bg-surface-raised/70 border border-border'
                  "
                >
                  {{ tab.label }}
                  <span
                    class="ml-1 text-[10px] sm:text-[11px] px-1.5 py-0.2 rounded-full"
                    :class="
                      statusFilter === tab.value
                        ? 'bg-white/25 text-white'
                        : 'bg-card text-text-muted border border-border'
                    "
                  >
                    {{ statusCounts[tab.value] || 0 }}
                  </span>
                </button>
              </div>
            </div>
          </div>
        </div>

        <!-- Applications List Empty State -->
        <div v-if="filteredApplications.length === 0" class="bg-card rounded-xl sm:rounded-2xl shadow-sm border border-border p-10 sm:p-12 text-center space-y-3">
          <User class="h-10 w-10 text-text-subtle mx-auto" />
          <h3 class="text-sm sm:text-base font-bold text-text-strong">No Applications Found</h3>
          <p class="text-xs text-text-muted max-w-sm mx-auto leading-relaxed">
            <span v-if="searchQuery">No applicants match your search query "{{ searchQuery }}".</span>
            <span v-else-if="statusFilter !== 'all'">No candidates found in the "{{ statusFilter.replace('_', ' ') }}" stage.</span>
            <span v-else>No candidates have applied to this job yet.</span>
          </p>
        </div>

        <!-- Applications List Items -->
        <div v-else class="space-y-4">
          <div
            v-for="app in filteredApplications"
            :key="app.id"
            class="bg-card rounded-xl sm:rounded-2xl shadow-sm border border-border p-4 sm:p-6 transition hover:border-border-strong space-y-4"
          >
            <!-- Top Row: Applicant Profile & Status -->
            <div class="flex flex-col lg:flex-row lg:items-start justify-between gap-4">
              <div class="flex items-start gap-3.5 sm:gap-4 min-w-0">
                <!-- Avatar -->
                <div class="h-11 w-11 sm:h-12 sm:w-12 rounded-full bg-primary text-white font-bold text-base sm:text-lg flex items-center justify-center overflow-hidden flex-shrink-0 shadow-sm">
                  <img
                    v-if="app.user?.avatar_path"
                    :src="getProfileImageUrl(app.user.avatar_path) || ''"
                    class="h-full w-full object-cover"
                    :alt="getApplicantName(app.user)"
                  />
                  <span v-else>{{ getApplicantInitial(app.user) }}</span>
                </div>

                <!-- Info -->
                <div class="min-w-0 flex-1">
                  <div class="flex flex-wrap items-center gap-1.5 sm:gap-2">
                    <h3 class="text-sm sm:text-base font-bold text-text-strong truncate">
                      {{ getApplicantName(app.user) }}
                    </h3>
                    <span class="text-xs text-text-subtle">•</span>
                    <span class="text-[11px] sm:text-xs text-text-muted whitespace-nowrap">
                      Applied {{ formatAppliedDate(app.applied_at) }}
                    </span>
                  </div>

                  <p v-if="app.user?.headline || app.user?.current_job_title" class="text-xs text-text-secondary font-medium mt-0.5 truncate">
                    {{ app.user.headline || app.user.current_job_title }}
                  </p>

                  <div class="mt-2 flex flex-wrap items-center gap-x-4 gap-y-1 text-xs text-text-muted">
                    <span class="inline-flex items-center gap-1 truncate">
                      <Mail class="h-3 w-3 text-text-subtle flex-shrink-0" />
                      <a :href="`mailto:${app.user?.email}`" class="hover:text-primary hover:underline truncate">
                        {{ app.user?.email }}
                      </a>
                    </span>

                    <span v-if="app.user?.phone" class="inline-flex items-center gap-1">
                      <Phone class="h-3 w-3 text-text-subtle flex-shrink-0" />
                      {{ app.user.phone }}
                    </span>

                    <span v-if="app.user?.city" class="inline-flex items-center gap-1">
                      <MapPin class="h-3 w-3 text-text-subtle flex-shrink-0" />
                      {{ app.user.city }}
                    </span>
                  </div>
                </div>
              </div>

              <!-- Status Badge & Controls -->
              <div class="flex flex-col sm:flex-row lg:flex-col items-start sm:items-center lg:items-end justify-between gap-3 pt-2 lg:pt-0 border-t lg:border-t-0 border-border">
                <span
                  class="inline-flex items-center px-2.5 py-1 rounded-full text-[11px] sm:text-xs font-semibold uppercase tracking-wider border"
                  :class="{
                    'badge-neutral': app.status === 'applied' || app.status === 'withdrawn',
                    'badge-warning': app.status === 'under_review',
                    'badge-primary': app.status === 'shortlisted' || app.status === 'interview',
                    'badge-success': app.status === 'offered' || app.status === 'hired',
                    'badge-danger': app.status === 'rejected',
                  }"
                >
                  {{ app.status.replace('_', ' ') }}
                </span>

                <!-- Action Pipeline Buttons -->
                <div class="flex flex-wrap items-center gap-1.5">
                  <button
                    v-if="app.status === 'applied'"
                    type="button"
                    :disabled="actionLoading === app.id"
                    @click="handleUpdateStatus(app, 'under_review')"
                    class="px-2.5 py-1 text-xs font-semibold rounded-md alert-warning hover:bg-warning-bg disabled:opacity-50 transition"
                  >
                    Review
                  </button>

                  <button
                    v-if="['applied', 'under_review'].includes(app.status)"
                    type="button"
                    :disabled="actionLoading === app.id"
                    @click="handleUpdateStatus(app, 'shortlisted')"
                    class="px-2.5 py-1 text-xs font-semibold rounded-md bg-primary-light text-primary border border-primary/30 hover:bg-primary/15 disabled:opacity-50 transition"
                  >
                    Shortlist
                  </button>

                  <button
                    v-if="['under_review', 'shortlisted'].includes(app.status)"
                    type="button"
                    :disabled="actionLoading === app.id"
                    @click="handleUpdateStatus(app, 'interview')"
                    class="px-2.5 py-1 text-xs font-semibold rounded-md bg-primary-light text-primary border border-primary/30 hover:bg-primary/15 disabled:opacity-50 transition"
                  >
                    Interview
                  </button>

                  <button
                    v-if="['shortlisted', 'interview'].includes(app.status)"
                    type="button"
                    :disabled="actionLoading === app.id"
                    @click="handleUpdateStatus(app, 'offered')"
                    class="px-2.5 py-1 text-xs font-semibold rounded-md alert-success hover:bg-success-bg disabled:opacity-50 transition"
                  >
                    Offer
                  </button>

                  <button
                    v-if="app.status === 'offered'"
                    type="button"
                    :disabled="actionLoading === app.id"
                    @click="handleUpdateStatus(app, 'hired')"
                    class="px-2.5 py-1 text-xs font-semibold rounded-md alert-success hover:bg-success-bg disabled:opacity-50 transition"
                  >
                    Hire
                  </button>

                  <button
                    v-if="app.status !== 'rejected' && app.status !== 'hired'"
                    type="button"
                    :disabled="actionLoading === app.id"
                    @click="handleUpdateStatus(app, 'rejected')"
                    class="px-2.5 py-1 text-xs font-semibold rounded-md alert-error hover:bg-danger-bg disabled:opacity-50 transition"
                  >
                    Reject
                  </button>
                </div>
              </div>
            </div>

            <!-- Cover Letter Section (Collapsible) -->
            <div v-if="app.cover_letter" class="pt-3 border-t border-border">
              <div class="flex items-center justify-between">
                <span class="text-[11px] sm:text-xs font-bold uppercase tracking-wider text-text-muted">
                  Cover Letter
                </span>
                <button
                  type="button"
                  @click="toggleCoverLetter(app.id)"
                  class="text-xs font-semibold text-primary hover:text-primary-hover inline-flex items-center gap-1 transition"
                >
                  <span>{{ expandedCoverLetters[app.id] ? 'Collapse' : 'Expand' }}</span>
                  <ChevronUp v-if="expandedCoverLetters[app.id]" class="h-3.5 w-3.5" />
                  <ChevronDown v-else class="h-3.5 w-3.5" />
                </button>
              </div>

              <div
                class="mt-2 text-xs text-text-secondary bg-surface-raised p-3 sm:p-3.5 rounded-xl border border-border whitespace-pre-line leading-relaxed"
                :class="{ 'line-clamp-3': !expandedCoverLetters[app.id] }"
              >
                {{ app.cover_letter }}
              </div>
            </div>

            <!-- Custom Form Answers (if any) -->
            <div
              v-if="app.custom_answers && Object.keys(app.custom_answers).length > 0"
              class="pt-3 border-t border-border space-y-2"
            >
              <span class="text-[11px] sm:text-xs font-bold uppercase tracking-wider text-text-muted">
                Custom Answers
              </span>
              <div class="grid grid-cols-1 sm:grid-cols-2 gap-2 text-xs">
                <div
                  v-for="(val, key) in app.custom_answers"
                  :key="key"
                  class="p-2.5 bg-surface-raised rounded-lg border border-border"
                >
                  <span class="font-semibold text-text-strong capitalize">{{ key }}:</span>
                  <p class="text-text-secondary mt-0.5 leading-relaxed">{{ val }}</p>
                </div>
              </div>
            </div>
          </div>
        </div>

      </div>
    </div>
  </div>
</template>