<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useJobsStore } from '@/stores/jobs'
import { applicationService } from '@/services/application.service'
import { organizationService } from '@/services/organization.service'
import { resumeService } from '@/services/resume.service'
import { assessmentService } from '@/services/assessment.service'
import { supabase } from '@/lib/supabase'
import { getProfileImageUrl } from '@/composables/useProfileImage'
import type {
  JobApplication,
  ApplicationStatus,
  ApplicationStatusFilter,
} from '@/types/application'
import type { QuizAttempt, QuizAnswerDetail } from '@/types/assessment'
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
  FileText,
  Download,
  Loader2,
  Award,
  CheckCircle2,
  XCircle,
  AlertCircle,
  Clock,
  HelpCircle,
  Check,
  X,
  Eye,
  Lightbulb,
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
const downloadingResumeId = ref<string | null>(null)
const expandedCoverLetters = ref<Record<string, boolean>>({})

// Assessment Quiz state
const quizAttempts = ref<QuizAttempt[]>([])
const loadingQuizAttempts = ref(false)
const selectedQuizAttempt = ref<QuizAttempt | null>(null)
const selectedApplicationForQuiz = ref<JobApplication | null>(null)
const showQuizModal = ref(false)
const loadingQuizDetail = ref(false)
const hasQuizStage = ref(false)
const quizStageTitle = ref('Quiz Assessment')

const attemptsByAppId = computed(() => {
  const map = new Map<string, QuizAttempt>()
  for (const a of quizAttempts.value) {
    const key = (a.application_id || '').toLowerCase().trim()
    if (!key) continue
    const existing = map.get(key)
    if (!existing) {
      map.set(key, a)
    } else if (existing.status !== 'graded' && (a.status === 'graded' || a.status === 'submitted')) {
      map.set(key, a)
    }
  }
  return map
})

const attemptsByUserId = computed(() => {
  const map = new Map<string, QuizAttempt>()
  for (const a of quizAttempts.value) {
    const key = (a.user_id || '').toLowerCase().trim()
    if (!key) continue
    const existing = map.get(key)
    if (!existing) {
      map.set(key, a)
    } else if (existing.status !== 'graded' && (a.status === 'graded' || a.status === 'submitted')) {
      map.set(key, a)
    }
  }
  return map
})

function getQuizAttempt(app: JobApplication): QuizAttempt | undefined {
  const appId = (app.id || '').toLowerCase().trim()
  const candidateUserId = (app.user_id || app.user?.id || '').toLowerCase().trim()
  return (appId ? attemptsByAppId.value.get(appId) : undefined) ||
         (candidateUserId ? attemptsByUserId.value.get(candidateUserId) : undefined)
}

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

    // 3. Fetch applications, quiz attempts, and job selection processes
    await Promise.all([
      loadApplications(),
      loadQuizAttempts(),
      loadJobProcesses()
    ])
  } catch (err: any) {
    console.error('Error loading applications:', err)
    toast.error(err.message || 'Failed to load applications')
  } finally {
    loading.value = false
  }
})

async function loadJobProcesses() {
  try {
    const { data: processes } = await supabase
      .from('job_selection_processes')
      .select('id, title, process_type, config, question_bank_id')
      .eq('job_id', jobId)

    if (processes && processes.length > 0) {
      const quizProc = processes.find((p: any) =>
        p.process_type === 'quiz' ||
        p.process_type === 'screening_quiz' ||
        p.process_type === 'quiz_coding' ||
        p.question_bank_id ||
        p.config?.question_bank_id
      )
      if (quizProc) {
        hasQuizStage.value = true
        quizStageTitle.value = quizProc.title || 'Quiz Assessment'
      }
    }
  } catch (e) {
    console.warn('Failed to load job selection processes:', e)
  }
}

async function loadApplications() {
  try {
    applications.value = await applicationService.fetchApplicationsForJob(jobId)
  } catch (err: any) {
    toast.error('Failed to load applications list')
  }
}

async function loadQuizAttempts() {
  if (!auth.user) return
  loadingQuizAttempts.value = true
  try {
    quizAttempts.value = await assessmentService.getJobAttempts(jobId, auth.user.id)
    console.log('[JobApplications] Loaded quiz attempts:', quizAttempts.value.length, quizAttempts.value)
  } catch (err) {
    console.error('Failed to load quiz attempts for job:', err)
  } finally {
    loadingQuizAttempts.value = false
  }
}

async function openQuizResults(app: JobApplication) {
  selectedApplicationForQuiz.value = app
  showQuizModal.value = true
  loadingQuizDetail.value = true
  try {
    const summary = getQuizAttempt(app)
    if (summary?.id) {
      const detailed = await assessmentService.getAttempt(summary.id, auth.user!.id)
      selectedQuizAttempt.value = detailed || summary
    } else {
      const attemptByApp = await assessmentService.getApplicationAttempt(app.id, auth.user!.id)
      selectedQuizAttempt.value = attemptByApp
    }
  } catch (err: any) {
    console.error('Error fetching quiz details:', err)
    toast.error(err.message || 'Failed to load detailed quiz results.')
  } finally {
    loadingQuizDetail.value = false
  }
}

function closeQuizModal() {
  showQuizModal.value = false
  selectedQuizAttempt.value = null
  selectedApplicationForQuiz.value = null
}

function formatQuestionType(type?: string): string {
  if (!type) return 'Question'
  switch (type) {
    case 'single_choice':
      return 'Single Choice'
    case 'multiple_choice':
      return 'Multiple Choice'
    case 'true_false':
      return 'True / False'
    case 'structured':
      return 'Structured Open-Ended'
    default:
      return type.replace('_', ' ')
  }
}

function formatAttemptDate(dateStr?: string | null): string {
  if (!dateStr) return ''
  try {
    return new Date(dateStr).toLocaleString(undefined, {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
      hour: 'numeric',
      minute: '2-digit'
    })
  } catch {
    return dateStr
  }
}

function isCandidateSelected(answer: any, optId: string): boolean {
  if (answer === undefined || answer === null) return false
  if (Array.isArray(answer)) {
    return answer.some(v => String(v).trim().toLowerCase() === optId.toLowerCase())
  }
  return String(answer).trim().toLowerCase() === optId.toLowerCase()
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

function formatFileSize(bytes: number | null | undefined): string {
  if (!bytes) return ''
  if (bytes < 1024) return bytes + ' B'
  if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB'
  return (bytes / (1024 * 1024)).toFixed(1) + ' MB'
}

async function handleDownloadResume(app: JobApplication) {
  if (!app.resume?.file_path) {
    toast.error('No resume file attached to this application.')
    return
  }

  downloadingResumeId.value = app.id
  try {
    const url = await resumeService.getDownloadUrl(app.resume.file_path)
    if (url) {
      window.open(url, '_blank')
    } else {
      toast.error('Could not generate download link for this resume.')
    }
  } catch (err: any) {
    toast.error(err.message || 'Failed to download resume.')
  } finally {
    downloadingResumeId.value = null
  }
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

            <!-- Quiz Assessment Results Card -->
            <div
              v-if="getQuizAttempt(app) || (app.score !== null && app.score !== undefined) || hasQuizStage"
              class="pt-3 border-t border-border"
            >
              <!-- 1. Candidate Has Attended the Quiz -->
              <div
                v-if="getQuizAttempt(app)"
                class="flex flex-col sm:flex-row sm:items-center justify-between gap-3 p-3.5 rounded-xl border transition"
                :class="{
                  'bg-success-bg/25 border-success-border/60': getQuizAttempt(app)?.passed === true,
                  'bg-danger-bg/25 border-danger-border/60': getQuizAttempt(app)?.passed === false,
                  'bg-warning-bg/25 border-warning-border/60': getQuizAttempt(app)?.status === 'submitted',
                  'bg-surface-raised/60 border-border': getQuizAttempt(app)?.status === 'in_progress' || getQuizAttempt(app)?.passed === null,
                }"
              >
                <div class="flex items-center gap-3 min-w-0">
                  <div
                    class="h-9 w-9 rounded-lg flex items-center justify-center shrink-0 shadow-xs"
                    :class="{
                      'bg-success-bg text-success': getQuizAttempt(app)?.passed === true,
                      'bg-danger-bg text-danger': getQuizAttempt(app)?.passed === false,
                      'bg-warning-bg text-warning': getQuizAttempt(app)?.status === 'submitted',
                      'bg-primary-light text-primary': getQuizAttempt(app)?.status === 'in_progress' || getQuizAttempt(app)?.passed === null,
                    }"
                  >
                    <Award class="h-4 w-4" />
                  </div>

                  <div class="min-w-0">
                    <div class="flex flex-wrap items-center gap-2">
                      <span class="text-xs font-bold text-text-strong truncate">
                        {{ getQuizAttempt(app)?.stage_title || quizStageTitle }}
                      </span>
                      <!-- Result Status Badge -->
                      <span
                        v-if="getQuizAttempt(app)?.status === 'graded'"
                        class="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[10px] font-bold uppercase tracking-wider"
                        :class="getQuizAttempt(app)?.passed ? 'bg-success text-white' : 'bg-danger text-white'"
                      >
                        <CheckCircle2 v-if="getQuizAttempt(app)?.passed" class="h-3 w-3" />
                        <XCircle v-else class="h-3 w-3" />
                        <span>{{ getQuizAttempt(app)?.passed ? 'Passed' : 'Failed' }}</span>
                      </span>
                      <span
                        v-else-if="getQuizAttempt(app)?.status === 'submitted'"
                        class="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[10px] font-bold uppercase tracking-wider alert-warning"
                      >
                        <Clock class="h-3 w-3" />
                        <span>Under Review</span>
                      </span>
                      <span
                        v-else-if="getQuizAttempt(app)?.status === 'in_progress'"
                        class="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[10px] font-bold uppercase tracking-wider bg-primary-light text-primary border border-primary/20"
                      >
                        <Clock class="h-3 w-3" />
                        <span>In Progress</span>
                      </span>
                    </div>

                    <div class="flex flex-wrap items-center gap-x-2 gap-y-0.5 text-[11px] text-text-muted mt-0.5">
                      <span
                        class="font-semibold"
                        :class="getQuizAttempt(app)?.passed ? 'text-success' : (getQuizAttempt(app)?.passed === false ? 'text-danger' : 'text-text-strong')"
                      >
                        Score: {{ getQuizAttempt(app)?.percentage ?? app.score ?? 0 }}%
                        <span v-if="getQuizAttempt(app)?.max_score">
                          ({{ getQuizAttempt(app)?.score }} / {{ getQuizAttempt(app)?.max_score }} pts)
                        </span>
                      </span>
                      <span v-if="getQuizAttempt(app)?.passing_score">•</span>
                      <span v-if="getQuizAttempt(app)?.passing_score">
                        Pass Mark: {{ getQuizAttempt(app)?.passing_score }}%
                      </span>
                      <span v-if="getQuizAttempt(app)?.submitted_at">•</span>
                      <span v-if="getQuizAttempt(app)?.submitted_at">
                        Submitted {{ formatAttemptDate(getQuizAttempt(app)?.submitted_at) }}
                      </span>
                    </div>
                  </div>
                </div>

                <!-- Action Button -->
                <button
                  type="button"
                  @click="openQuizResults(app)"
                  class="inline-flex items-center justify-center gap-1.5 px-3 py-1.5 text-xs font-semibold rounded-lg transition shrink-0 shadow-xs cursor-pointer"
                  :class="getQuizAttempt(app)?.passed
                    ? 'bg-success-bg text-success hover:bg-success hover:text-white border border-success-border'
                    : (getQuizAttempt(app)?.passed === false
                      ? 'bg-danger-bg text-danger hover:bg-danger hover:text-white border border-danger-border'
                      : 'bg-primary-light text-primary hover:bg-primary hover:text-white border border-primary/30')"
                >
                  <Eye class="h-3.5 w-3.5" />
                  <span>View Quiz Results</span>
                </button>
              </div>

              <!-- 2. Application has score record directly -->
              <div
                v-else-if="app.score !== null && app.score !== undefined"
                class="flex flex-col sm:flex-row sm:items-center justify-between gap-3 p-3.5 rounded-xl border border-border bg-surface-raised/50"
              >
                <div class="flex items-center gap-3 min-w-0">
                  <div class="h-9 w-9 rounded-lg bg-primary-light text-primary flex items-center justify-center shrink-0">
                    <Award class="h-4 w-4" />
                  </div>
                  <div class="min-w-0">
                    <span class="text-xs font-bold text-text-strong truncate">
                      {{ quizStageTitle }}
                    </span>
                    <p class="text-[11px] font-semibold text-text-strong mt-0.5">
                      Recorded Score: {{ app.score }}%
                    </p>
                  </div>
                </div>
                <button
                  type="button"
                  @click="openQuizResults(app)"
                  class="inline-flex items-center justify-center gap-1.5 px-3 py-1.5 text-xs font-semibold rounded-lg bg-primary-light text-primary hover:bg-primary hover:text-white border border-primary/30 transition shrink-0 cursor-pointer"
                >
                  <Eye class="h-3.5 w-3.5" />
                  <span>View Quiz Results</span>
                </button>
              </div>

              <!-- 3. Candidate Has NOT Taken Quiz Yet -->
              <div
                v-else-if="hasQuizStage"
                class="flex items-center justify-between gap-3 p-3 rounded-xl border border-border bg-surface-raised/30"
              >
                <div class="flex items-center gap-3 min-w-0">
                  <div class="h-8 w-8 rounded-lg bg-surface flex items-center justify-center shrink-0 text-text-subtle border border-border">
                    <Award class="h-3.5 w-3.5" />
                  </div>
                  <div class="min-w-0">
                    <div class="flex items-center gap-2">
                      <span class="text-xs font-bold text-text-secondary truncate">
                        {{ quizStageTitle }}
                      </span>
                      <span class="inline-flex items-center px-2 py-0.5 rounded-full text-[10px] font-semibold uppercase tracking-wider bg-surface text-text-muted border border-border">
                        Not Attempted Yet
                      </span>
                    </div>
                    <p class="text-[11px] text-text-subtle mt-0.5">
                      Candidate has not started or submitted the quiz for this job opening.
                    </p>
                  </div>
                </div>
              </div>
            </div>

            <!-- Attached Resume -->
            <div
              v-if="app.resume"
              class="pt-3 border-t border-border flex flex-col sm:flex-row sm:items-center justify-between gap-3 bg-surface-raised/40 p-3 rounded-xl border"
            >
              <div class="flex items-center gap-3 min-w-0">
                <div
                  class="h-9 w-9 rounded-lg bg-primary-light text-primary flex items-center justify-center shrink-0"
                >
                  <FileText class="h-4 w-4" />
                </div>
                <div class="min-w-0">
                  <div class="flex items-center gap-2">
                    <span class="text-xs font-bold text-text-strong truncate">
                      {{ app.resume.title }}
                    </span>
                  </div>
                  <p class="text-[11px] text-text-muted truncate mt-0.5">
                    {{ app.resume.file_name }}
                    <span v-if="app.resume.file_size">
                      • {{ formatFileSize(app.resume.file_size) }}
                    </span>
                  </p>
                </div>
              </div>

              <button
                type="button"
                :disabled="downloadingResumeId === app.id"
                @click="handleDownloadResume(app)"
                class="inline-flex items-center justify-center gap-1.5 px-3 py-1.5 text-xs font-semibold rounded-lg border border-border bg-card text-text-secondary hover:text-text-strong hover:bg-surface-raised transition shrink-0"
              >
                <Loader2 v-if="downloadingResumeId === app.id" class="h-3.5 w-3.5 animate-spin" />
                <Download v-else class="h-3.5 w-3.5" />
                <span>View / Download Resume</span>
              </button>
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

    <!-- Quiz Results Modal for Organization Officials -->
    <Teleport to="body">
      <div
        v-if="showQuizModal"
        class="fixed inset-0 z-50 overflow-y-auto bg-black/60 backdrop-blur-xs flex items-center justify-center p-3 sm:p-6"
        @keydown.esc="closeQuizModal"
      >
        <div
          class="bg-card w-full max-w-3xl rounded-2xl border border-border shadow-2xl overflow-hidden flex flex-col max-h-[90vh] my-auto animate-in fade-in zoom-in-95 duration-150"
        >
          <!-- Modal Header -->
          <div class="px-5 sm:px-6 py-4 border-b border-border bg-surface flex items-start justify-between gap-4">
            <div class="space-y-1 min-w-0">
              <div class="flex items-center gap-2">
                <span class="inline-flex items-center gap-1 px-2 py-0.5 rounded-md text-[11px] font-bold uppercase tracking-wider bg-primary-light text-primary border border-primary/20">
                  <Award class="h-3 w-3" />
                  <span>{{ selectedQuizAttempt?.stage_title || 'Quiz Assessment Results' }}</span>
                </span>
                <span
                  v-if="selectedQuizAttempt?.status === 'graded'"
                  class="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[10px] font-bold uppercase tracking-wider"
                  :class="selectedQuizAttempt?.passed ? 'bg-success text-white' : 'bg-danger text-white'"
                >
                  <CheckCircle2 v-if="selectedQuizAttempt?.passed" class="h-3 w-3" />
                  <XCircle v-else class="h-3 w-3" />
                  <span>{{ selectedQuizAttempt?.passed ? 'Passed' : 'Failed' }}</span>
                </span>
              </div>

              <h2 class="text-base sm:text-lg font-bold text-text-strong truncate">
                Candidate: {{ selectedQuizAttempt?.candidate_name || getApplicantName(selectedApplicationForQuiz?.user) }}
              </h2>
              <p class="text-xs text-text-muted truncate">
                {{ selectedQuizAttempt?.candidate_email || selectedApplicationForQuiz?.user?.email }}
                <span v-if="job?.title">• {{ job.title }}</span>
              </p>
            </div>

            <button
              type="button"
              @click="closeQuizModal"
              class="text-text-muted hover:text-text-strong p-1.5 rounded-lg hover:bg-surface-raised transition cursor-pointer"
              title="Close Modal"
            >
              <X class="h-5 w-5" />
            </button>
          </div>

          <!-- Modal Body (Scrollable) -->
          <div class="p-5 sm:p-6 overflow-y-auto space-y-6 flex-1">
            <!-- Loading Details Indicator -->
            <div v-if="loadingQuizDetail" class="py-12 text-center space-y-3">
              <Loader2 class="h-8 w-8 animate-spin text-primary mx-auto" />
              <p class="text-xs sm:text-sm text-text-muted">Loading candidate answers and evaluation breakdown...</p>
            </div>

            <!-- Quiz Attempt Content -->
            <div v-else-if="selectedQuizAttempt" class="space-y-6">
              <!-- Scorecard Banner -->
              <div
                class="rounded-2xl p-4 sm:p-5 border flex flex-col sm:flex-row items-center justify-between gap-4"
                :class="{
                  'bg-success-bg/30 border-success-border': selectedQuizAttempt.passed === true,
                  'bg-danger-bg/30 border-danger-border': selectedQuizAttempt.passed === false,
                  'bg-surface-raised border-border': selectedQuizAttempt.passed === null || selectedQuizAttempt.passed === undefined
                }"
              >
                <!-- Left: Big percentage & Status -->
                <div class="flex items-center gap-4 text-center sm:text-left">
                  <div
                    class="h-16 w-16 rounded-2xl flex flex-col items-center justify-center font-bold text-2xl shadow-sm border"
                    :class="{
                      'bg-success text-white border-success': selectedQuizAttempt.passed === true,
                      'bg-danger text-white border-danger': selectedQuizAttempt.passed === false,
                      'bg-primary text-white border-primary': selectedQuizAttempt.passed === null || selectedQuizAttempt.passed === undefined
                    }"
                  >
                    <span>{{ selectedQuizAttempt.percentage }}%</span>
                  </div>

                  <div>
                    <h3 class="text-base font-bold text-text-strong">
                      {{ selectedQuizAttempt.passed ? 'Assessment Passed' : (selectedQuizAttempt.passed === false ? 'Assessment Failed' : 'Assessment Completed') }}
                    </h3>
                    <p class="text-xs text-text-secondary mt-0.5">
                      Earned {{ selectedQuizAttempt.score }} of {{ selectedQuizAttempt.max_score }} total points
                    </p>
                    <p v-if="selectedQuizAttempt.passing_score" class="text-[11px] text-text-muted mt-0.5">
                      Passing Requirement: {{ selectedQuizAttempt.passing_score }}%
                    </p>
                  </div>
                </div>

                <!-- Right: Submission metadata -->
                <div class="text-xs text-text-muted text-center sm:text-right space-y-1">
                  <div v-if="selectedQuizAttempt.submitted_at" class="flex items-center sm:justify-end gap-1.5">
                    <Clock class="h-3.5 w-3.5 text-text-subtle" />
                    <span>Submitted {{ formatAttemptDate(selectedQuizAttempt.submitted_at) }}</span>
                  </div>
                  <div v-if="selectedQuizAttempt.started_at" class="text-[11px]">
                    Started: {{ formatAttemptDate(selectedQuizAttempt.started_at) }}
                  </div>
                </div>
              </div>

              <!-- Question Breakdown List -->
              <div class="space-y-4">
                <div class="flex items-center justify-between border-b border-border pb-2">
                  <h4 class="text-xs sm:text-sm font-bold uppercase tracking-wider text-text-muted">
                    Questions & Responses Breakdown
                    <span v-if="selectedQuizAttempt.answers">({{ selectedQuizAttempt.answers.length }})</span>
                  </h4>
                  <span class="text-xs text-text-subtle">Graded Assessment</span>
                </div>

                <!-- Empty Answers State -->
                <div
                  v-if="!selectedQuizAttempt.answers || selectedQuizAttempt.answers.length === 0"
                  class="p-6 text-center text-xs sm:text-sm text-text-muted bg-surface-raised rounded-xl border border-border"
                >
                  No detailed answer records found for this assessment attempt.
                </div>

                <!-- Answers List -->
                <div v-else class="space-y-4">
                  <div
                    v-for="(ans, index) in selectedQuizAttempt.answers"
                    :key="ans.question_id"
                    class="rounded-xl border border-border bg-card p-4 sm:p-5 space-y-3.5 shadow-xs"
                  >
                    <!-- Question Header Row -->
                    <div class="flex flex-wrap items-center justify-between gap-2">
                      <div class="flex items-center gap-2">
                        <span class="text-xs font-bold text-text-muted">#{{ index + 1 }}</span>
                        <span class="text-[11px] font-semibold px-2 py-0.5 rounded-md bg-surface-raised text-text-secondary border border-border">
                          {{ formatQuestionType(ans.question_type) }}
                        </span>
                      </div>

                      <!-- Points Badge -->
                      <span
                        class="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-bold"
                        :class="{
                          'bg-success-bg text-success border border-success-border': ans.is_correct === true,
                          'bg-danger-bg text-danger border border-danger-border': ans.is_correct === false,
                          'bg-warning-bg text-warning border border-warning-border': ans.is_correct === null || ans.is_correct === undefined
                        }"
                      >
                        <CheckCircle2 v-if="ans.is_correct === true" class="h-3 w-3" />
                        <XCircle v-else-if="ans.is_correct === false" class="h-3 w-3" />
                        <HelpCircle v-else class="h-3 w-3" />
                        <span>{{ ans.points_awarded }} / {{ ans.max_points }} pts</span>
                      </span>
                    </div>

                    <!-- Question Body -->
                    <div class="text-xs sm:text-sm font-semibold text-text-strong leading-relaxed whitespace-pre-line">
                      {{ ans.question_body }}
                    </div>

                    <!-- Options Rubric (for Single Choice, Multiple Choice, True/False) -->
                    <div
                      v-if="ans.options && ans.options.length > 0 && ans.question_type !== 'structured'"
                      class="space-y-2 pt-1"
                    >
                      <div
                        v-for="opt in ans.options"
                        :key="opt.id"
                        class="flex items-center justify-between gap-3 p-2.5 sm:p-3 rounded-lg border text-xs transition"
                        :class="{
                          'bg-success-bg/40 border-success text-success font-medium': opt.is_correct && isCandidateSelected(ans.answer, opt.id),
                          'bg-danger-bg/40 border-danger text-danger font-medium': !opt.is_correct && isCandidateSelected(ans.answer, opt.id),
                          'bg-success-bg/15 border-success/40 text-text-strong font-medium': opt.is_correct && !isCandidateSelected(ans.answer, opt.id),
                          'bg-surface-raised/40 border-border text-text-secondary': !opt.is_correct && !isCandidateSelected(ans.answer, opt.id),
                        }"
                      >
                        <div class="flex items-center gap-2.5 min-w-0">
                          <!-- Check/X Indicator Icon -->
                          <div
                            class="h-5 w-5 rounded-full flex items-center justify-center shrink-0 text-white"
                            :class="{
                              'bg-success': opt.is_correct && isCandidateSelected(ans.answer, opt.id),
                              'bg-danger': !opt.is_correct && isCandidateSelected(ans.answer, opt.id),
                              'bg-success/70': opt.is_correct && !isCandidateSelected(ans.answer, opt.id),
                              'border border-border text-transparent': !opt.is_correct && !isCandidateSelected(ans.answer, opt.id),
                            }"
                          >
                            <Check v-if="opt.is_correct" class="h-3 w-3 text-white" />
                            <X v-else-if="isCandidateSelected(ans.answer, opt.id)" class="h-3 w-3 text-white" />
                          </div>
                          <span class="break-words">{{ opt.text }}</span>
                        </div>

                        <!-- Badge Tags on Option -->
                        <div class="flex items-center gap-1.5 shrink-0">
                          <span
                            v-if="isCandidateSelected(ans.answer, opt.id)"
                            class="px-2 py-0.5 rounded text-[10px] font-bold uppercase tracking-wider"
                            :class="opt.is_correct ? 'bg-success text-white' : 'bg-danger text-white'"
                          >
                            Candidate's Choice
                          </span>
                          <span
                            v-if="opt.is_correct"
                            class="px-2 py-0.5 rounded text-[10px] font-bold uppercase tracking-wider bg-success-bg text-success border border-success-border"
                          >
                            Correct Answer
                          </span>
                        </div>
                      </div>
                    </div>

                    <!-- Structured / Written Open-Ended Response -->
                    <div
                      v-else-if="ans.question_type === 'structured'"
                      class="pt-1 space-y-2"
                    >
                      <div class="text-[11px] font-bold uppercase tracking-wider text-text-muted">
                        Candidate's Response:
                      </div>
                      <div class="p-3 bg-surface-raised rounded-lg border border-border text-xs text-text-strong whitespace-pre-line leading-relaxed">
                        {{ ans.answer || '(No answer written)' }}
                      </div>
                    </div>

                    <!-- Explanation Callout (if available) -->
                    <div
                      v-if="ans.explanation"
                      class="p-2.5 rounded-lg bg-surface-raised/70 border border-border flex items-start gap-2 text-xs text-text-secondary"
                    >
                      <Lightbulb class="h-4 w-4 text-warning shrink-0 mt-0.5" />
                      <div>
                        <span class="font-bold text-text-strong">Explanation:</span>
                        {{ ans.explanation }}
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>

            <!-- Fallback if attempt could not be loaded -->
            <div v-else class="py-12 text-center space-y-2">
              <AlertCircle class="h-8 w-8 text-text-subtle mx-auto" />
              <p class="text-xs sm:text-sm text-text-muted">No assessment results could be loaded for this applicant.</p>
            </div>
          </div>

          <!-- Modal Footer with Quick Pipeline Actions -->
          <div class="px-5 sm:px-6 py-3.5 border-t border-border bg-surface flex flex-col sm:flex-row items-center justify-between gap-3">
            <!-- Pipeline Quick Actions -->
            <div v-if="selectedApplicationForQuiz" class="flex flex-wrap items-center gap-1.5 self-start sm:self-auto">
              <span class="text-[11px] font-semibold text-text-muted mr-1">Move Candidate:</span>
              <button
                v-if="['applied', 'under_review'].includes(selectedApplicationForQuiz.status)"
                type="button"
                @click="handleUpdateStatus(selectedApplicationForQuiz, 'shortlisted')"
                class="px-2.5 py-1 text-xs font-semibold rounded-md bg-primary-light text-primary border border-primary/30 hover:bg-primary/15 transition cursor-pointer"
              >
                Shortlist
              </button>
              <button
                v-if="['under_review', 'shortlisted'].includes(selectedApplicationForQuiz.status)"
                type="button"
                @click="handleUpdateStatus(selectedApplicationForQuiz, 'interview')"
                class="px-2.5 py-1 text-xs font-semibold rounded-md bg-primary-light text-primary border border-primary/30 hover:bg-primary/15 transition cursor-pointer"
              >
                Interview
              </button>
              <button
                v-if="selectedApplicationForQuiz.status !== 'rejected'"
                type="button"
                @click="handleUpdateStatus(selectedApplicationForQuiz, 'rejected')"
                class="px-2.5 py-1 text-xs font-semibold rounded-md alert-error hover:bg-danger-bg transition cursor-pointer"
              >
                Reject
              </button>
            </div>
            <div v-else></div>

            <!-- Close Button -->
            <button
              type="button"
              @click="closeQuizModal"
              class="w-full sm:w-auto px-4 py-2 text-xs font-semibold rounded-lg border border-border bg-card text-text-strong hover:bg-surface-raised transition shadow-xs cursor-pointer"
            >
              Close Results
            </button>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>