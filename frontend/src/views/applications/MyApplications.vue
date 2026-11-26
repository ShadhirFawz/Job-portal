<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { applicationService } from '@/services/application.service'
import { assessmentService } from '@/services/assessment.service'
import { resumeService } from '@/services/resume.service'
import { getOrganizationImageUrl } from '@/composables/useOrganizationImage'
import type {
  JobApplication,
  ApplicationStatus,
  ApplicationStatusFilter,
  CandidateSelectionStage,
  CandidateQuizAttemptSummary,
} from '@/types/application'
import type { QuizAttempt } from '@/types/assessment'
import {
  Briefcase,
  Building2,
  MapPin,
  Search,
  Clock,
  CheckCircle2,
  XCircle,
  Award,
  FileText,
  Download,
  ExternalLink,
  Loader2,
  X,
  Check,
  HelpCircle,
  Lightbulb,
  Play,
  AlertCircle,
  Ban,
  ChevronRight,
  Layers,
} from 'lucide-vue-next'
import { toast } from 'vue-sonner'

const router = useRouter()
const auth = useAuthStore()

const loading = ref(true)
const applications = ref<JobApplication[]>([])
const searchQuery = ref('')
const statusFilter = ref<ApplicationStatusFilter>('all')

// Detail Modal State
const showDetailModal = ref(false)
const selectedApp = ref<JobApplication | null>(null)
const activeModalTab = ref<'overview' | 'quiz'>('overview')
const detailedQuizAttempt = ref<QuizAttempt | null>(null)
const loadingQuizDetail = ref(false)
const withdrawingId = ref<string | null>(null)
const downloadingResume = ref(false)

const filterTabs: Array<{ label: string; value: ApplicationStatusFilter }> = [
  { label: 'All', value: 'all' },
  { label: 'Applied', value: 'applied' },
  { label: 'Under Review', value: 'under_review' },
  { label: 'Shortlisted', value: 'shortlisted' },
  { label: 'Interview', value: 'interview' },
  { label: 'Offered', value: 'offered' },
  { label: 'Hired', value: 'hired' },
  { label: 'Rejected', value: 'rejected' },
  { label: 'Withdrawn', value: 'withdrawn' },
]

const pipelineSteps: Array<{ key: ApplicationStatus; label: string }> = [
  { key: 'applied', label: 'Applied' },
  { key: 'under_review', label: 'Under Review' },
  { key: 'shortlisted', label: 'Shortlisted' },
  { key: 'interview', label: 'Interview' },
  { key: 'offered', label: 'Offered' },
  { key: 'hired', label: 'Hired' },
]

onMounted(async () => {
  await loadApplications()
})

async function loadApplications() {
  if (!auth.user) return
  loading.value = true
  try {
    applications.value = await applicationService.fetchMyApplications(auth.user.id)
  } catch (err: any) {
    console.error('Failed to load my applications:', err)
    toast.error(err.message || 'Failed to load your applications')
  } finally {
    loading.value = false
  }
}

const statusCounts = computed(() => {
  const counts: Record<string, number> = { all: applications.value.length }
  for (const app of applications.value) {
    counts[app.status] = (counts[app.status] || 0) + 1
  }
  return counts
})

const activeCount = computed(() =>
  applications.value.filter((a) =>
    ['applied', 'under_review', 'shortlisted', 'interview'].includes(a.status),
  ).length,
)

const offerHiredCount = computed(() =>
  applications.value.filter((a) => ['offered', 'hired'].includes(a.status)).length,
)

const quizCount = computed(() =>
  applications.value.filter((a) => hasQuizForApp(a)).length,
)

const filteredApplications = computed(() => {
  let list = applications.value
  if (statusFilter.value !== 'all') {
    list = list.filter((a) => a.status === statusFilter.value)
  }
  if (searchQuery.value.trim()) {
    const q = searchQuery.value.toLowerCase().trim()
    list = list.filter((a) => {
      const title = (a.job?.title || '').toLowerCase()
      const org = (a.job?.organization?.name || '').toLowerCase()
      const city = (a.job?.city || '').toLowerCase()
      return title.includes(q) || org.includes(q) || city.includes(q)
    })
  }
  return list
})

function isQuizStage(stage: CandidateSelectionStage): boolean {
  return (
    ['quiz', 'screening_quiz', 'quiz_coding'].includes(stage.process_type) ||
    !!stage.question_bank_id
  )
}

function getQuizStages(app: JobApplication): CandidateSelectionStage[] {
  return (app.selection_stages || []).filter(isQuizStage)
}

function hasQuizForApp(app: JobApplication): boolean {
  return getQuizStages(app).length > 0 || (app.quiz_attempts?.length || 0) > 0
}

function getLatestQuizAttempt(app: JobApplication): CandidateQuizAttemptSummary | undefined {
  if (!app.quiz_attempts || app.quiz_attempts.length === 0) return undefined
  return (
    app.quiz_attempts.find((q) => q.status === 'graded') ||
    app.quiz_attempts.find((q) => q.status === 'submitted') ||
    app.quiz_attempts[0]
  )
}

function getStageAttempt(
  app: JobApplication,
  stageId: string,
): CandidateQuizAttemptSummary | undefined {
  return app.quiz_attempts?.find((q) => q.selection_process_id === stageId)
}

function getStepState(
  currentStatus: ApplicationStatus,
  stepKey: ApplicationStatus,
): 'done' | 'current' | 'upcoming' | 'halted' {
  if (currentStatus === 'rejected' || currentStatus === 'withdrawn') {
    return stepKey === 'applied' ? 'done' : 'halted'
  }
  const order = pipelineSteps.map((s) => s.key)
  const currIdx = order.indexOf(currentStatus)
  const stepIdx = order.indexOf(stepKey)
  if (stepIdx < currIdx) return 'done'
  if (stepIdx === currIdx) return 'current'
  return 'upcoming'
}

function formatStatusLabel(status: string): string {
  return status.replace(/_/g, ' ')
}

function formatDate(dateStr?: string | null): string {
  if (!dateStr) return ''
  try {
    return new Date(dateStr).toLocaleDateString(undefined, {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
    })
  } catch {
    return dateStr
  }
}

function formatDateTime(dateStr?: string | null): string {
  if (!dateStr) return ''
  try {
    return new Date(dateStr).toLocaleString(undefined, {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
      hour: 'numeric',
      minute: '2-digit',
    })
  } catch {
    return dateStr
  }
}

function formatFileSize(bytes?: number | null): string {
  if (!bytes) return ''
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
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

function isCandidateSelected(answer: any, optId: string): boolean {
  if (answer === undefined || answer === null) return false
  if (Array.isArray(answer)) {
    return answer.some((v) => String(v).trim().toLowerCase() === optId.toLowerCase())
  }
  return String(answer).trim().toLowerCase() === optId.toLowerCase()
}

async function openApplicationModal(
  app: JobApplication,
  initialTab: 'overview' | 'quiz' = 'overview',
) {
  selectedApp.value = app
  activeModalTab.value = initialTab
  showDetailModal.value = true
  detailedQuizAttempt.value = null

  const latestAttempt = getLatestQuizAttempt(app)
  if (latestAttempt && auth.user) {
    await loadQuizDetail(latestAttempt.id)
  }
}

async function loadQuizDetail(attemptId: string) {
  if (!auth.user) return
  loadingQuizDetail.value = true
  try {
    detailedQuizAttempt.value = await assessmentService.getAttempt(attemptId, auth.user.id)
  } catch (err: any) {
    console.error('Failed to load detailed quiz attempt:', err)
  } finally {
    loadingQuizDetail.value = false
  }
}

function closeDetailModal() {
  showDetailModal.value = false
  selectedApp.value = null
  detailedQuizAttempt.value = null
}

function goToQuiz(app: JobApplication, stage?: CandidateSelectionStage) {
  const jobSlug = app.job?.slug || app.job_id
  const targetStage = stage || getQuizStages(app)[0]
  if (!targetStage) return
  const procSlug = targetStage.slug || targetStage.id
  router.push(`/jobs/${jobSlug}/assessments/${procSlug}`)
}

async function handleDownloadResume(app: JobApplication) {
  if (!app.resume?.file_path) return
  downloadingResume.value = true
  try {
    const url = await resumeService.getDownloadUrl(app.resume.file_path)
    if (url) {
      window.open(url, '_blank')
    } else {
      toast.error('Could not generate resume download link.')
    }
  } catch (err: any) {
    toast.error(err.message || 'Failed to download resume.')
  } finally {
    downloadingResume.value = false
  }
}

async function handleWithdraw(app: JobApplication) {
  if (!auth.user) return
  if (!confirm(`Are you sure you want to withdraw your application for "${app.job?.title || 'this job'}"?`)) {
    return
  }

  withdrawingId.value = app.id
  try {
    await applicationService.withdrawApplication(app.slug || app.id, auth.user.id)
    app.status = 'withdrawn'
    app.withdrawn_at = new Date().toISOString()
    toast.success('Application withdrawn successfully.')
  } catch (err: any) {
    console.error('Error withdrawing application:', err)
    toast.error(err.message || 'Failed to withdraw application.')
  } finally {
    withdrawingId.value = null
  }
}
</script>

<template>
  <div class="min-h-screen bg-surface py-6 sm:py-8 lg:py-10 px-4 sm:px-6 lg:px-8">
    <div class="max-w-6xl mx-auto space-y-6">
      <!-- Page Header -->
      <div class="bg-card rounded-2xl border border-border p-5 sm:p-7 shadow-sm space-y-5">
        <div class="flex flex-col md:flex-row md:items-center justify-between gap-4">
          <div class="space-y-1">
            <div
              class="flex items-center gap-2 text-xs font-semibold uppercase tracking-wider text-primary"
            >
              <Briefcase class="h-3.5 w-3.5" />
              <span>Candidate Portal</span>
            </div>
            <h1 class="text-xl sm:text-2xl lg:text-3xl font-bold text-text-strong tracking-tight">
              My Applications & Assessments
            </h1>
            <p class="text-xs sm:text-sm text-text-muted">
              Track your job application statuses, hiring pipeline progress, and quiz results in one place.
            </p>
          </div>

          <router-link
            to="/jobs"
            class="inline-flex items-center justify-center gap-2 px-4 py-2.5 text-xs sm:text-sm font-semibold rounded-xl bg-primary text-white hover:bg-primary-hover transition shadow-sm shrink-0"
          >
            <Search class="h-4 w-4" />
            <span>Browse Jobs</span>
          </router-link>
        </div>

        <!-- Summary Metrics -->
        <div class="grid grid-cols-2 lg:grid-cols-4 gap-3 pt-2">
          <div class="rounded-xl border border-border bg-surface-raised/50 p-3.5">
            <span class="text-[11px] font-semibold uppercase tracking-wider text-text-muted">
              Total Applied
            </span>
            <p class="text-xl sm:text-2xl font-bold text-text-strong mt-1">
              {{ applications.length }}
            </p>
          </div>
          <div class="rounded-xl border border-border bg-surface-raised/50 p-3.5">
            <span class="text-[11px] font-semibold uppercase tracking-wider text-text-muted">
              In Progress
            </span>
            <p class="text-xl sm:text-2xl font-bold text-primary mt-1">
              {{ activeCount }}
            </p>
          </div>
          <div class="rounded-xl border border-border bg-surface-raised/50 p-3.5">
            <span class="text-[11px] font-semibold uppercase tracking-wider text-text-muted">
              With Assessments
            </span>
            <p class="text-xl sm:text-2xl font-bold text-warning mt-1">
              {{ quizCount }}
            </p>
          </div>
          <div class="rounded-xl border border-border bg-surface-raised/50 p-3.5">
            <span class="text-[11px] font-semibold uppercase tracking-wider text-text-muted">
              Offers & Hired
            </span>
            <p class="text-xl sm:text-2xl font-bold text-success mt-1">
              {{ offerHiredCount }}
            </p>
          </div>
        </div>

        <!-- Search & Status Filter Controls -->
        <div
          class="pt-4 border-t border-border flex flex-col lg:flex-row items-stretch lg:items-center justify-between gap-4"
        >
          <div class="relative w-full lg:w-76 shrink-0">
            <Search
              class="absolute left-3.5 top-1/2 -translate-y-1/2 h-4 w-4 text-text-subtle pointer-events-none"
            />
            <input
              v-model="searchQuery"
              type="text"
              placeholder="Search by job title, company, or city..."
              class="w-full pl-10 pr-4 py-2.5 text-xs sm:text-sm rounded-xl border border-border bg-card text-text-strong placeholder:text-text-subtle focus:border-primary focus:ring-2 focus:ring-primary/25 shadow-sm transition"
            />
          </div>

          <div
            class="w-full lg:w-auto overflow-x-auto pb-1 lg:pb-0 [-ms-overflow-style:none] [scrollbar-width:none] [&::-webkit-scrollbar]:hidden"
          >
            <div class="flex items-center gap-1.5 min-w-max">
              <button
                v-for="tab in filterTabs"
                :key="tab.value"
                type="button"
                @click="statusFilter = tab.value"
                class="px-3 py-1.5 text-xs font-semibold rounded-lg transition whitespace-nowrap shrink-0 cursor-pointer"
                :class="
                  statusFilter === tab.value
                    ? 'bg-primary text-white shadow-sm'
                    : 'bg-surface-raised text-text-secondary hover:bg-surface-raised/70 border border-border'
                "
              >
                {{ tab.label }}
                <span
                  class="ml-1 text-[10px] px-1.5 py-0.2 rounded-full"
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

      <!-- Loading State -->
      <div
        v-if="loading"
        class="bg-card rounded-2xl border border-border p-12 text-center space-y-3"
      >
        <Loader2 class="h-8 w-8 animate-spin text-primary mx-auto" />
        <p class="text-xs sm:text-sm text-text-muted">Loading your job applications...</p>
      </div>

      <!-- Empty State -->
      <div
        v-else-if="filteredApplications.length === 0"
        class="bg-card rounded-2xl border border-border p-10 sm:p-14 text-center space-y-3"
      >
        <Briefcase class="h-10 w-10 text-text-subtle mx-auto" />
        <h3 class="text-sm sm:text-base font-bold text-text-strong">
          No Applications Found
        </h3>
        <p class="text-xs text-text-muted max-w-sm mx-auto leading-relaxed">
          <span v-if="searchQuery">
            No applications match "{{ searchQuery }}".
          </span>
          <span v-else-if="statusFilter !== 'all'">
            You have no applications in the "{{ formatStatusLabel(statusFilter) }}" stage.
          </span>
          <span v-else>
            You haven't applied to any jobs yet. Explore open roles to start your journey!
          </span>
        </p>
        <router-link
          v-if="applications.length === 0"
          to="/jobs"
          class="inline-flex items-center gap-1.5 px-4 py-2 text-xs font-semibold rounded-xl bg-primary text-white hover:bg-primary-hover transition shadow-sm mt-2"
        >
          Explore Open Jobs
        </router-link>
      </div>

      <!-- Applications List (Clickable Cards) -->
      <div v-else class="space-y-4">
        <div
          v-for="app in filteredApplications"
          :key="app.id"
          role="button"
          tabindex="0"
          @click="openApplicationModal(app, 'overview')"
          @keydown.enter.prevent="openApplicationModal(app, 'overview')"
          class="bg-card rounded-2xl border border-border p-4 sm:p-6 shadow-sm hover:border-primary/50 hover:shadow-md transition cursor-pointer group space-y-4"
          title="Click to view detailed application & quiz tracking"
        >
          <!-- Top Row: Job Info + Application Status -->
          <div class="flex flex-col sm:flex-row sm:items-start justify-between gap-4">
            <div class="flex items-start gap-3.5 min-w-0">
              <!-- Organization Avatar -->
              <div
                class="h-12 w-12 rounded-xl bg-surface-raised border border-border flex items-center justify-center overflow-hidden shrink-0"
              >
                <img
                  v-if="app.job?.organization?.avatar_path"
                  :src="getOrganizationImageUrl(app.job.organization.avatar_path) || ''"
                  class="h-full w-full object-cover"
                  :alt="app.job?.organization?.name || 'Organization'"
                />
                <Building2 v-else class="h-6 w-6 text-primary" />
              </div>

              <div class="min-w-0 space-y-1">
                <h3
                  class="text-sm sm:text-base font-bold text-text-strong truncate group-hover:text-primary transition"
                >
                  {{ app.job?.title || 'Job Position' }}
                </h3>
                <div class="flex flex-wrap items-center gap-x-2.5 gap-y-1 text-xs text-text-secondary">
                  <span class="font-semibold text-text-strong">
                    {{ app.job?.organization?.name || 'Organization' }}
                  </span>
                  <span v-if="app.job?.job_type">•</span>
                  <span v-if="app.job?.job_type" class="capitalize">
                    {{ app.job.job_type.replace('_', ' ') }}
                  </span>
                  <span v-if="app.job?.work_mode">•</span>
                  <span v-if="app.job?.work_mode" class="capitalize">
                    {{ app.job.work_mode }}
                  </span>
                  <span v-if="app.job?.city" class="inline-flex items-center gap-1 text-text-muted">
                    <span>•</span>
                    <MapPin class="h-3 w-3" />
                    {{ app.job.city }}
                  </span>
                </div>
                <p class="text-[11px] text-text-muted">
                  Applied {{ formatDate(app.applied_at) }}
                  <span v-if="app.updated_at && app.updated_at !== app.applied_at">
                    • Last updated {{ formatDate(app.updated_at) }}
                  </span>
                </p>
              </div>
            </div>

            <!-- Right: Status Badge + Expand Chevron -->
            <div class="flex items-center justify-between sm:justify-end gap-2.5 shrink-0">
              <span
                class="inline-flex items-center px-3 py-1 rounded-full text-[11px] font-bold uppercase tracking-wider border"
                :class="{
                  'badge-neutral': app.status === 'applied' || app.status === 'withdrawn',
                  'badge-warning': app.status === 'under_review',
                  'badge-primary': app.status === 'shortlisted' || app.status === 'interview',
                  'badge-success': app.status === 'offered' || app.status === 'hired',
                  'badge-danger': app.status === 'rejected',
                }"
              >
                {{ formatStatusLabel(app.status) }}
              </span>
              <ChevronRight
                class="h-4 w-4 text-text-subtle group-hover:text-primary group-hover:translate-x-0.5 transition"
              />
            </div>
          </div>

          <!-- Quiz / Assessment Summary Bar (if job has quiz stage or attempt) -->
          <div
            v-if="hasQuizForApp(app)"
            class="pt-3 border-t border-border flex flex-col sm:flex-row sm:items-center justify-between gap-3"
          >
            <div class="flex items-center gap-2.5 min-w-0">
              <div
                class="h-8 w-8 rounded-lg flex items-center justify-center shrink-0"
                :class="{
                  'bg-success-bg text-success': getLatestQuizAttempt(app)?.passed === true,
                  'bg-danger-bg text-danger': getLatestQuizAttempt(app)?.passed === false,
                  'bg-warning-bg text-warning':
                    getLatestQuizAttempt(app)?.status === 'submitted' ||
                    getLatestQuizAttempt(app)?.requires_manual_review,
                  'bg-primary-light text-primary':
                    !getLatestQuizAttempt(app) ||
                    getLatestQuizAttempt(app)?.status === 'in_progress',
                }"
              >
                <Award class="h-4 w-4" />
              </div>

              <div class="min-w-0">
                <div class="flex flex-wrap items-center gap-2">
                  <span class="text-xs font-bold text-text-strong truncate">
                    {{
                      getLatestQuizAttempt(app)?.stage_title ||
                      getQuizStages(app)[0]?.title ||
                      'Assessment Quiz'
                    }}
                  </span>

                  <!-- Quiz Attempt Status -->
                  <span
                    v-if="getLatestQuizAttempt(app)?.status === 'graded'"
                    class="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[10px] font-bold uppercase tracking-wider"
                    :class="
                      getLatestQuizAttempt(app)?.passed
                        ? 'bg-success text-white'
                        : 'bg-danger text-white'
                    "
                  >
                    <CheckCircle2 v-if="getLatestQuizAttempt(app)?.passed" class="h-3 w-3" />
                    <XCircle v-else class="h-3 w-3" />
                    <span>
                      {{ getLatestQuizAttempt(app)?.passed ? 'Passed' : 'Failed' }}
                      ({{ getLatestQuizAttempt(app)?.percentage }}%)
                    </span>
                  </span>

                  <span
                    v-else-if="
                      getLatestQuizAttempt(app)?.status === 'submitted' ||
                      getLatestQuizAttempt(app)?.requires_manual_review
                    "
                    class="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[10px] font-bold uppercase tracking-wider alert-warning"
                  >
                    <Clock class="h-3 w-3" />
                    <span>Pending Manual Review</span>
                  </span>

                  <span
                    v-else-if="getLatestQuizAttempt(app)?.status === 'in_progress'"
                    class="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[10px] font-bold uppercase tracking-wider bg-primary-light text-primary border border-primary/25"
                  >
                    <Clock class="h-3 w-3" />
                    <span>In Progress</span>
                  </span>

                  <span
                    v-else
                    class="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[10px] font-bold uppercase tracking-wider alert-warning"
                  >
                    <AlertCircle class="h-3 w-3" />
                    <span>Not Started</span>
                  </span>
                </div>

                <p class="text-[11px] text-text-muted mt-0.5">
                  <template v-if="getLatestQuizAttempt(app)?.status === 'graded'">
                    Score: {{ getLatestQuizAttempt(app)?.score }} /
                    {{ getLatestQuizAttempt(app)?.max_score }} pts
                    <span v-if="getLatestQuizAttempt(app)?.reviewed_at">
                      • Reviewed {{ formatDate(getLatestQuizAttempt(app)?.reviewed_at) }}
                    </span>
                  </template>
                  <template
                    v-else-if="
                      getLatestQuizAttempt(app)?.status === 'submitted' ||
                      getLatestQuizAttempt(app)?.requires_manual_review
                    "
                  >
                    Submitted {{ formatDate(getLatestQuizAttempt(app)?.submitted_at) }} • Awaiting
                    reviewer grading for structured answers
                  </template>
                  <template v-else-if="getLatestQuizAttempt(app)?.status === 'in_progress'">
                    Started {{ formatDateTime(getLatestQuizAttempt(app)?.started_at) }}
                  </template>
                  <template v-else>
                    Click to start or view assessment details
                  </template>
                </p>
              </div>
            </div>

            <!-- Action shortcut for pending quiz -->
            <div class="flex items-center gap-2 shrink-0">
              <button
                v-if="
                  (!getLatestQuizAttempt(app) ||
                    getLatestQuizAttempt(app)?.status === 'in_progress') &&
                  !['rejected', 'withdrawn'].includes(app.status)
                "
                type="button"
                @click.stop="goToQuiz(app)"
                class="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold rounded-lg bg-primary text-white hover:bg-primary-hover transition shadow-xs cursor-pointer"
              >
                <Play class="h-3 w-3" />
                <span>
                  {{ getLatestQuizAttempt(app)?.status === 'in_progress' ? 'Resume Quiz' : 'Start Quiz' }}
                </span>
              </button>
              <button
                v-else-if="getLatestQuizAttempt(app)"
                type="button"
                @click.stop="openApplicationModal(app, 'quiz')"
                class="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold rounded-lg bg-primary-light text-primary border border-primary/25 hover:bg-primary hover:text-white transition shadow-xs cursor-pointer"
              >
                <Award class="h-3.5 w-3.5" />
                <span>Quiz Details</span>
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- =================================================================== -->
    <!-- ADVANCED APPLICATION & QUIZ TRACKER MODAL                           -->
    <!-- =================================================================== -->
    <Teleport to="body">
      <div
        v-if="showDetailModal && selectedApp"
        class="fixed inset-0 z-50 overflow-y-auto bg-black/60 backdrop-blur-xs flex items-center justify-center p-3 sm:p-6"
        @click.self="closeDetailModal"
        @keydown.esc="closeDetailModal"
      >
        <div
          class="bg-card w-full max-w-3xl rounded-2xl border border-border shadow-2xl overflow-hidden flex flex-col max-h-[90vh] my-auto"
        >
          <!-- Modal Header -->
          <div
            class="px-5 sm:px-6 py-4 border-b border-border bg-surface flex items-start justify-between gap-4"
          >
            <div class="min-w-0 space-y-1">
              <div class="flex flex-wrap items-center gap-2">
                <span
                  class="inline-flex items-center px-2.5 py-0.5 rounded-full text-[10px] font-bold uppercase tracking-wider border"
                  :class="{
                    'badge-neutral':
                      selectedApp.status === 'applied' || selectedApp.status === 'withdrawn',
                    'badge-warning': selectedApp.status === 'under_review',
                    'badge-primary':
                      selectedApp.status === 'shortlisted' || selectedApp.status === 'interview',
                    'badge-success':
                      selectedApp.status === 'offered' || selectedApp.status === 'hired',
                    'badge-danger': selectedApp.status === 'rejected',
                  }"
                >
                  {{ formatStatusLabel(selectedApp.status) }}
                </span>
                <span class="text-[11px] text-text-muted">
                  Applied {{ formatDateTime(selectedApp.applied_at) }}
                </span>
              </div>

              <h2 class="text-base sm:text-lg font-bold text-text-strong truncate">
                {{ selectedApp.job?.title || 'Application Details' }}
              </h2>
              <p class="text-xs text-text-secondary flex flex-wrap items-center gap-1.5">
                <span class="font-semibold">{{ selectedApp.job?.organization?.name }}</span>
                <span v-if="selectedApp.job?.city">• {{ selectedApp.job.city }}</span>
                <router-link
                  v-if="selectedApp.job"
                  :to="`/jobs/${selectedApp.job.slug || selectedApp.job.id}`"
                  class="inline-flex items-center gap-1 text-primary hover:underline ml-1"
                >
                  <span>View Job</span>
                  <ExternalLink class="h-3 w-3" />
                </router-link>
              </p>
            </div>

            <button
              type="button"
              @click="closeDetailModal"
              class="text-text-muted hover:text-text-strong p-1.5 rounded-lg hover:bg-surface-raised transition cursor-pointer"
              title="Close"
            >
              <X class="h-5 w-5" />
            </button>
          </div>

          <!-- Modal Tabs (Overview / Quiz Tracker) -->
          <div class="px-5 sm:px-6 border-b border-border bg-surface-raised/40 flex items-center gap-2">
            <button
              type="button"
              @click="activeModalTab = 'overview'"
              class="px-3.5 py-2.5 text-xs font-bold border-b-2 transition cursor-pointer flex items-center gap-1.5"
              :class="
                activeModalTab === 'overview'
                  ? 'border-primary text-primary'
                  : 'border-transparent text-text-muted hover:text-text-strong'
              "
            >
              <Layers class="h-3.5 w-3.5" />
              <span>Application Status & Timeline</span>
            </button>

            <button
              v-if="hasQuizForApp(selectedApp)"
              type="button"
              @click="activeModalTab = 'quiz'"
              class="px-3.5 py-2.5 text-xs font-bold border-b-2 transition cursor-pointer flex items-center gap-1.5"
              :class="
                activeModalTab === 'quiz'
                  ? 'border-primary text-primary'
                  : 'border-transparent text-text-muted hover:text-text-strong'
              "
            >
              <Award class="h-3.5 w-3.5" />
              <span>Quiz & Assessment Tracking</span>
            </button>
          </div>

          <!-- Modal Body -->
          <div class="p-5 sm:p-6 overflow-y-auto space-y-6 flex-1">
            <!-- ==================== TAB 1: OVERVIEW ==================== -->
            <div v-if="activeModalTab === 'overview'" class="space-y-6">
              <!-- Visual Application Progress Stepper -->
              <div class="rounded-2xl border border-border bg-surface-raised/40 p-4 sm:p-5 space-y-4">
                <div class="flex items-center justify-between">
                  <h3 class="text-xs font-bold uppercase tracking-wider text-text-muted">
                    Application Progress
                  </h3>
                  <span
                    v-if="selectedApp.status === 'rejected'"
                    class="text-xs font-bold text-danger flex items-center gap-1"
                  >
                    <XCircle class="h-3.5 w-3.5" />
                    Application Not Selected
                  </span>
                  <span
                    v-else-if="selectedApp.status === 'withdrawn'"
                    class="text-xs font-bold text-text-muted flex items-center gap-1"
                  >
                    <Ban class="h-3.5 w-3.5" />
                    Withdrawn on {{ formatDate(selectedApp.withdrawn_at) }}
                  </span>
                </div>

                <div class="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-6 gap-2.5">
                  <div
                    v-for="(step, idx) in pipelineSteps"
                    :key="step.key"
                    class="rounded-xl p-2.5 border text-center space-y-1 transition"
                    :class="{
                      'bg-success-bg/30 border-success-border text-success':
                        getStepState(selectedApp.status, step.key) === 'done',
                      'bg-primary-light border-primary text-primary shadow-xs':
                        getStepState(selectedApp.status, step.key) === 'current',
                      'bg-surface border-border text-text-subtle opacity-70':
                        getStepState(selectedApp.status, step.key) === 'upcoming' ||
                        getStepState(selectedApp.status, step.key) === 'halted',
                    }"
                  >
                    <div class="text-[10px] font-bold uppercase tracking-wider">
                      Step {{ idx + 1 }}
                    </div>
                    <div class="text-xs font-bold truncate">{{ step.label }}</div>
                  </div>
                </div>
              </div>

              <!-- Selection Process Stages (if configured on the job) -->
              <div
                v-if="selectedApp.selection_stages && selectedApp.selection_stages.length > 0"
                class="space-y-3"
              >
                <h3 class="text-xs font-bold uppercase tracking-wider text-text-muted">
                  Hiring Selection Stages ({{ selectedApp.selection_stages.length }})
                </h3>
                <div class="space-y-2.5">
                  <div
                    v-for="stage in selectedApp.selection_stages"
                    :key="stage.id"
                    class="p-3.5 rounded-xl border border-border bg-card flex flex-col sm:flex-row sm:items-center justify-between gap-3"
                  >
                    <div class="flex items-center gap-3 min-w-0">
                      <div
                        class="h-8 w-8 rounded-lg bg-primary-light text-primary font-bold text-xs flex items-center justify-center shrink-0"
                      >
                        {{ stage.order_index }}
                      </div>
                      <div class="min-w-0">
                        <div class="flex items-center gap-2">
                          <span class="text-xs sm:text-sm font-bold text-text-strong truncate">
                            {{ stage.title }}
                          </span>
                          <span
                            class="text-[10px] font-semibold px-2 py-0.5 rounded bg-surface-raised text-text-secondary border border-border capitalize"
                          >
                            {{ stage.process_type.replace('_', ' ') }}
                          </span>
                        </div>
                        <p class="text-[11px] text-text-muted mt-0.5">
                          <span v-if="stage.time_limit_minutes">
                            Time Limit: {{ stage.time_limit_minutes }} mins
                          </span>
                          <span v-if="stage.time_limit_minutes && stage.passing_score"> • </span>
                          <span v-if="stage.passing_score">
                            Pass Mark: {{ stage.passing_score }}%
                          </span>
                        </p>
                      </div>
                    </div>

                    <!-- Stage Attempt Action / Status -->
                    <div v-if="isQuizStage(stage)" class="shrink-0">
                      <template v-if="getStageAttempt(selectedApp, stage.id)">
                        <button
                          v-if="
                            getStageAttempt(selectedApp, stage.id)?.status === 'in_progress' &&
                            !['rejected', 'withdrawn'].includes(selectedApp.status)
                          "
                          type="button"
                          @click="goToQuiz(selectedApp, stage)"
                          class="px-3 py-1.5 text-xs font-semibold rounded-lg bg-primary text-white hover:bg-primary-hover transition cursor-pointer"
                        >
                          Resume Quiz
                        </button>
                        <button
                          v-else
                          type="button"
                          @click="activeModalTab = 'quiz'"
                          class="px-3 py-1.5 text-xs font-semibold rounded-lg border border-border bg-surface-raised text-text-strong hover:border-primary transition cursor-pointer"
                        >
                          {{
                            getStageAttempt(selectedApp, stage.id)?.status === 'graded'
                              ? `Score: ${getStageAttempt(selectedApp, stage.id)?.percentage}%`
                              : 'View Quiz Status'
                          }}
                        </button>
                      </template>
                      <button
                        v-else-if="!['rejected', 'withdrawn'].includes(selectedApp.status)"
                        type="button"
                        @click="goToQuiz(selectedApp, stage)"
                        class="px-3 py-1.5 text-xs font-semibold rounded-lg bg-primary text-white hover:bg-primary-hover transition cursor-pointer inline-flex items-center gap-1"
                      >
                        <Play class="h-3 w-3" />
                        <span>Start Assessment</span>
                      </button>
                    </div>
                  </div>
                </div>
              </div>

              <!-- Submitted Resume (Clickable Card) -->
              <div v-if="selectedApp.resume" class="space-y-2">
                <h3 class="text-xs font-bold uppercase tracking-wider text-text-muted">
                  Submitted Resume
                </h3>
                <div
                  role="button"
                  tabindex="0"
                  @click="handleDownloadResume(selectedApp)"
                  @keydown.enter.prevent="handleDownloadResume(selectedApp)"
                  class="flex items-center justify-between gap-3 p-3.5 rounded-xl border border-border bg-surface-raised/40 hover:bg-surface-raised/80 hover:border-primary/50 transition cursor-pointer group"
                >
                  <div class="flex items-center gap-3 min-w-0">
                    <div
                      class="h-9 w-9 rounded-lg bg-primary-light text-primary flex items-center justify-center shrink-0"
                    >
                      <FileText class="h-4 w-4" />
                    </div>
                    <div class="min-w-0">
                      <p class="text-xs font-bold text-text-strong truncate group-hover:text-primary transition">
                        {{ selectedApp.resume.title }}
                      </p>
                      <p class="text-[11px] text-text-muted truncate">
                        {{ selectedApp.resume.file_name }}
                        <span v-if="selectedApp.resume.file_size">
                          • {{ formatFileSize(selectedApp.resume.file_size) }}
                        </span>
                      </p>
                    </div>
                  </div>
                  <Loader2 v-if="downloadingResume" class="h-4 w-4 animate-spin text-primary shrink-0" />
                  <Download v-else class="h-4 w-4 text-text-secondary group-hover:text-primary shrink-0" />
                </div>
              </div>

              <!-- Cover Letter -->
              <div v-if="selectedApp.cover_letter" class="space-y-2">
                <h3 class="text-xs font-bold uppercase tracking-wider text-text-muted">
                  Your Cover Letter
                </h3>
                <div
                  class="p-4 rounded-xl border border-border bg-surface-raised/40 text-xs sm:text-sm text-text-secondary whitespace-pre-line leading-relaxed"
                >
                  {{ selectedApp.cover_letter }}
                </div>
              </div>
            </div>

            <!-- ==================== TAB 2: QUIZ TRACKING ==================== -->
            <div v-else-if="activeModalTab === 'quiz'" class="space-y-6">
              <div v-if="loadingQuizDetail" class="py-12 text-center space-y-3">
                <Loader2 class="h-8 w-8 animate-spin text-primary mx-auto" />
                <p class="text-xs sm:text-sm text-text-muted">
                  Loading assessment status and question breakdown...
                </p>
              </div>

              <!-- No attempt started yet -->
              <div
                v-else-if="!getLatestQuizAttempt(selectedApp)"
                class="p-8 text-center rounded-2xl border border-dashed border-border bg-surface-raised/30 space-y-3"
              >
                <Award class="h-10 w-10 text-primary mx-auto" />
                <h3 class="text-sm sm:text-base font-bold text-text-strong">
                  Assessment Not Started Yet
                </h3>
                <p class="text-xs text-text-muted max-w-md mx-auto">
                  This job includes an assessment stage. Complete it to progress in your application.
                </p>
                <button
                  v-if="!['rejected', 'withdrawn'].includes(selectedApp.status)"
                  type="button"
                  @click="goToQuiz(selectedApp)"
                  class="inline-flex items-center gap-1.5 px-4 py-2 text-xs font-bold rounded-xl bg-primary text-white hover:bg-primary-hover transition shadow-sm cursor-pointer"
                >
                  <Play class="h-3.5 w-3.5" />
                  <span>Start Assessment Now</span>
                </button>
              </div>

              <!-- Attempt In Progress -->
              <div
                v-else-if="getLatestQuizAttempt(selectedApp)?.status === 'in_progress'"
                class="p-8 text-center rounded-2xl border border-primary/30 bg-primary-light/20 space-y-3"
              >
                <Clock class="h-10 w-10 text-primary mx-auto" />
                <h3 class="text-sm sm:text-base font-bold text-text-strong">
                  Assessment In Progress
                </h3>
                <p class="text-xs text-text-muted max-w-md mx-auto">
                  You started this assessment on
                  {{ formatDateTime(getLatestQuizAttempt(selectedApp)?.started_at) }}.
                </p>
                <button
                  v-if="!['rejected', 'withdrawn'].includes(selectedApp.status)"
                  type="button"
                  @click="goToQuiz(selectedApp)"
                  class="inline-flex items-center gap-1.5 px-4 py-2 text-xs font-bold rounded-xl bg-primary text-white hover:bg-primary-hover transition shadow-sm cursor-pointer"
                >
                  <Play class="h-3.5 w-3.5" />
                  <span>Resume Assessment</span>
                </button>
              </div>

              <!-- Submitted (Pending Review) or Graded Details -->
              <div v-else-if="detailedQuizAttempt || getLatestQuizAttempt(selectedApp)" class="space-y-6">
                <!-- Scorecard Banner -->
                <div
                  class="rounded-2xl p-4 sm:p-5 border flex flex-col sm:flex-row items-center justify-between gap-4"
                  :class="{
                    'bg-success-bg/30 border-success-border':
                      (detailedQuizAttempt?.passed ?? getLatestQuizAttempt(selectedApp)?.passed) === true,
                    'bg-danger-bg/30 border-danger-border':
                      (detailedQuizAttempt?.passed ?? getLatestQuizAttempt(selectedApp)?.passed) === false,
                    'bg-warning-bg/30 border-warning-border':
                      (detailedQuizAttempt?.status ?? getLatestQuizAttempt(selectedApp)?.status) === 'submitted',
                  }"
                >
                  <div class="flex items-center gap-4 text-center sm:text-left">
                    <div
                      class="h-16 w-16 rounded-2xl flex items-center justify-center font-bold text-xl shadow-sm border"
                      :class="{
                        'bg-success text-white border-success':
                          (detailedQuizAttempt?.passed ?? getLatestQuizAttempt(selectedApp)?.passed) === true,
                        'bg-danger text-white border-danger':
                          (detailedQuizAttempt?.passed ?? getLatestQuizAttempt(selectedApp)?.passed) === false,
                        'bg-warning text-white border-warning':
                          (detailedQuizAttempt?.status ?? getLatestQuizAttempt(selectedApp)?.status) === 'submitted',
                      }"
                    >
                      {{ detailedQuizAttempt?.percentage ?? getLatestQuizAttempt(selectedApp)?.percentage ?? 0 }}%
                    </div>

                    <div>
                      <h3 class="text-base font-bold text-text-strong">
                        <template
                          v-if="
                            (detailedQuizAttempt?.status ?? getLatestQuizAttempt(selectedApp)?.status) === 'submitted'
                          "
                        >
                          Pending Manual Review
                        </template>
                        <template
                          v-else-if="
                            (detailedQuizAttempt?.passed ?? getLatestQuizAttempt(selectedApp)?.passed) === true
                          "
                        >
                          Assessment Passed
                        </template>
                        <template v-else>
                          Assessment Not Passed
                        </template>
                      </h3>
                      <p class="text-xs text-text-secondary mt-0.5">
                        Score: {{ detailedQuizAttempt?.score ?? getLatestQuizAttempt(selectedApp)?.score ?? 0 }}
                        / {{ detailedQuizAttempt?.max_score ?? getLatestQuizAttempt(selectedApp)?.max_score ?? 0 }} pts
                      </p>
                      <p
                        v-if="detailedQuizAttempt?.passing_score ?? getLatestQuizAttempt(selectedApp)?.passing_score"
                        class="text-[11px] text-text-muted mt-0.5"
                      >
                        Passing Requirement:
                        {{ detailedQuizAttempt?.passing_score ?? getLatestQuizAttempt(selectedApp)?.passing_score }}%
                      </p>
                    </div>
                  </div>

                  <div class="text-xs text-text-muted text-center sm:text-right space-y-1">
                    <div v-if="detailedQuizAttempt?.submitted_at || getLatestQuizAttempt(selectedApp)?.submitted_at">
                      Submitted:
                      {{ formatDateTime(detailedQuizAttempt?.submitted_at ?? getLatestQuizAttempt(selectedApp)?.submitted_at) }}
                    </div>
                    <div v-if="detailedQuizAttempt?.reviewed_at || getLatestQuizAttempt(selectedApp)?.reviewed_at">
                      Reviewed:
                      {{ formatDateTime(detailedQuizAttempt?.reviewed_at ?? getLatestQuizAttempt(selectedApp)?.reviewed_at) }}
                    </div>
                  </div>
                </div>

                <!-- Pending Manual Review Info Banner -->
                <div
                  v-if="(detailedQuizAttempt?.status ?? getLatestQuizAttempt(selectedApp)?.status) === 'submitted'"
                  class="rounded-xl p-4 bg-warning-bg/30 border border-warning-border flex items-start gap-3"
                >
                  <Clock class="h-5 w-5 text-warning shrink-0 mt-0.5" />
                  <p class="text-xs text-text-secondary leading-relaxed">
                    Your objective questions have been auto-scored. Your structured open-ended answers are currently awaiting review by the hiring team. Final pass/fail status will update once reviewed.
                  </p>
                </div>

                <!-- Question-by-Question Breakdown -->
                <div
                  v-if="detailedQuizAttempt?.answers && detailedQuizAttempt.answers.length > 0"
                  class="space-y-4"
                >
                  <h4 class="text-xs font-bold uppercase tracking-wider text-text-muted border-b border-border pb-2">
                    Your Answers & Breakdown ({{ detailedQuizAttempt.answers.length }})
                  </h4>

                  <div
                    v-for="(ans, index) in detailedQuizAttempt.answers"
                    :key="ans.question_id"
                    class="rounded-xl border border-border bg-card p-4 space-y-3 shadow-xs"
                  >
                    <div class="flex flex-wrap items-center justify-between gap-2">
                      <div class="flex items-center gap-2">
                        <span class="text-xs font-bold text-text-muted">#{{ index + 1 }}</span>
                        <span
                          class="text-[11px] font-semibold px-2 py-0.5 rounded-md bg-surface-raised text-text-secondary border border-border"
                        >
                          {{ formatQuestionType(ans.question_type) }}
                        </span>
                      </div>

                      <span
                        class="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-bold"
                        :class="{
                          'bg-success-bg text-success border border-success-border': ans.is_correct === true,
                          'bg-danger-bg text-danger border border-danger-border': ans.is_correct === false,
                          'bg-warning-bg text-warning border border-warning-border':
                            ans.is_correct === null || ans.is_correct === undefined,
                        }"
                      >
                        <CheckCircle2 v-if="ans.is_correct === true" class="h-3 w-3" />
                        <XCircle v-else-if="ans.is_correct === false" class="h-3 w-3" />
                        <HelpCircle v-else class="h-3 w-3" />
                        <span>{{ ans.points_awarded }} / {{ ans.max_points }} pts</span>
                      </span>
                    </div>

                    <div class="text-xs sm:text-sm font-semibold text-text-strong whitespace-pre-line">
                      {{ ans.question_body }}
                    </div>

                    <!-- Choice Options -->
                    <div
                      v-if="ans.options && ans.options.length > 0 && ans.question_type !== 'structured'"
                      class="space-y-1.5"
                    >
                      <div
                        v-for="opt in ans.options"
                        :key="opt.id"
                        class="flex items-center justify-between gap-2 p-2.5 rounded-lg border text-xs"
                        :class="{
                          'bg-success-bg/40 border-success text-success font-medium':
                            opt.is_correct && isCandidateSelected(ans.answer, opt.id),
                          'bg-danger-bg/40 border-danger text-danger font-medium':
                            !opt.is_correct && isCandidateSelected(ans.answer, opt.id),
                          'bg-success-bg/15 border-success/40 text-text-strong font-medium':
                            opt.is_correct && !isCandidateSelected(ans.answer, opt.id),
                          'bg-surface-raised/40 border-border text-text-secondary':
                            !opt.is_correct && !isCandidateSelected(ans.answer, opt.id),
                        }"
                      >
                        <div class="flex items-center gap-2 min-w-0">
                          <Check v-if="opt.is_correct" class="h-3.5 w-3.5 text-success shrink-0" />
                          <X
                            v-else-if="isCandidateSelected(ans.answer, opt.id)"
                            class="h-3.5 w-3.5 text-danger shrink-0"
                          />
                          <span class="break-words">{{ opt.text }}</span>
                        </div>
                        <span
                          v-if="isCandidateSelected(ans.answer, opt.id)"
                          class="text-[10px] font-bold uppercase tracking-wider px-1.5 py-0.5 rounded"
                          :class="opt.is_correct ? 'bg-success text-white' : 'bg-danger text-white'"
                        >
                          Your Answer
                        </span>
                      </div>
                    </div>

                    <!-- Structured Answer & Reviewer Feedback -->
                    <div v-else-if="ans.question_type === 'structured'" class="space-y-2">
                      <div class="text-[11px] font-bold uppercase tracking-wider text-text-muted">
                        Your Written Response:
                      </div>
                      <div
                        class="p-3 bg-surface-raised rounded-lg border border-border text-xs text-text-strong whitespace-pre-line leading-relaxed"
                      >
                        {{ ans.answer || '(No answer written)' }}
                      </div>
                      <div
                        v-if="ans.reviewer_feedback"
                        class="p-3 bg-primary-light/30 rounded-lg border border-primary/25 text-xs text-text-secondary"
                      >
                        <span class="font-bold text-text-strong block mb-0.5">Reviewer Feedback:</span>
                        <p class="whitespace-pre-line">{{ ans.reviewer_feedback }}</p>
                      </div>
                    </div>

                    <!-- Explanation -->
                    <div
                      v-if="ans.explanation && detailedQuizAttempt.status === 'graded'"
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
          </div>

          <!-- Modal Footer -->
          <div
            class="px-5 sm:px-6 py-3.5 border-t border-border bg-surface flex flex-col sm:flex-row items-center justify-between gap-3"
          >
            <div>
              <button
                v-if="!['hired', 'rejected', 'withdrawn'].includes(selectedApp.status)"
                type="button"
                :disabled="withdrawingId === selectedApp.id"
                @click="handleWithdraw(selectedApp)"
                class="px-3 py-1.5 text-xs font-semibold rounded-lg alert-error hover:bg-danger-bg transition cursor-pointer disabled:opacity-50 inline-flex items-center gap-1.5"
              >
                <Loader2 v-if="withdrawingId === selectedApp.id" class="h-3.5 w-3.5 animate-spin" />
                <Ban v-else class="h-3.5 w-3.5" />
                <span>Withdraw Application</span>
              </button>
            </div>

            <button
              type="button"
              @click="closeDetailModal"
              class="w-full sm:w-auto px-4 py-2 text-xs font-semibold rounded-lg border border-border bg-card text-text-strong hover:bg-surface-raised transition shadow-xs cursor-pointer"
            >
              Close
            </button>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>
