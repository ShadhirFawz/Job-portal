<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useJobsStore } from '@/stores/jobs'
import { applicationService } from '@/services/application.service'
import { resumeService } from '@/services/resume.service'
import { getProfileImageUrl } from '@/composables/useProfileImage'
import { getOrganizationImageUrl } from '@/composables/useOrganizationImage'
import { formatJobDate } from '@/utils/date'
import type { JobApplication } from '@/types/application'
import type { Resume } from '@/types/resume'
import {
  CheckCircle2,
  Clock,
  ArrowLeft,
  Send,
  AlertCircle,
  FileText,
  User,
  Mail,
  MapPin,
  Sparkles,
  ArrowUpFromLineIcon,
} from 'lucide-vue-next'
import { toast } from 'vue-sonner'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const jobsStore = useJobsStore()

const jobId = route.params.id as string

const loading = ref(true)
const submitting = ref(false)
const errorMsg = ref('')
const successMsg = ref('')
const alreadyApplied = ref(false)
const existingApplication = ref<JobApplication | null>(null)

const coverLetter = ref('')
const customAnswers = ref<Record<string, any>>({})
const userResumes = ref<Resume[]>([])
const selectedResumeId = ref<string | null>(null)
const loadingResumes = ref(false)
const appliedResume = ref<Resume | null>(null)

function formatFileSize(bytes: number | null | undefined): string {
  if (!bytes) return ''
  if (bytes < 1024) return bytes + ' B'
  if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB'
  return (bytes / (1024 * 1024)).toFixed(1) + ' MB'
}

const job = computed(() => jobsStore.currentJob)

// Status & deadline checks
const isDeadlinePassed = computed(() => {
  if (!job.value?.application_deadline) return false
  return new Date(job.value.application_deadline) < new Date()
})

const canApply = computed(() => {
  if (!job.value) return false
  if (job.value.status !== 'published') return false
  if (job.value.accept_applications === false) return false
  if (isDeadlinePassed.value) return false
  if (alreadyApplied.value) return false
  return true
})

const applicantDisplayName = computed(() => {
  if (!auth.user) return ''
  return (
    auth.user.display_name ||
    `${auth.user.first_name || ''} ${auth.user.last_name || ''}`.trim() ||
    auth.user.email
  )
})

const applicantInitial = computed(() => {
  const name = applicantDisplayName.value || 'U'
  return name.charAt(0).toUpperCase()
})

onMounted(async () => {
  loading.value = true
  try {
    await jobsStore.fetchPublicJob(jobId)

    if (!jobsStore.currentJob) {
      await jobsStore.fetchJob(jobId)
    }

    if (auth.user) {
      const applied = await applicationService.checkUserApplied(jobId, auth.user.id)
      alreadyApplied.value = applied

      if (applied) {
        existingApplication.value = await applicationService.fetchUserApplication(
          jobId,
          auth.user.id
        )
        if (existingApplication.value?.resume_id) {
          appliedResume.value = await resumeService.fetchResumeById(
            existingApplication.value.resume_id
          )
        }
      } else {
        // Fetch user's active resumes for applying
        try {
          loadingResumes.value = true
          userResumes.value = await resumeService.fetchUserResumes(auth.user.id)
          const primary = userResumes.value.find((r) => r.is_primary)
          const firstResume = userResumes.value[0]
          if (primary) {
            selectedResumeId.value = primary.id
          } else if (firstResume) {
            selectedResumeId.value = firstResume.id
          }
        } catch (resErr) {
          console.warn('Could not load resumes:', resErr)
        } finally {
          loadingResumes.value = false
        }
      }
    }
  } catch (err: any) {
    errorMsg.value = err.message || 'Failed to load job details.'
  } finally {
    loading.value = false
  }
})

async function handleApply() {
  if (!auth.user || !job.value) return

  if (alreadyApplied.value) {
    errorMsg.value = 'You have already applied to this position.'
    return
  }

  if (!canApply.value) {
    errorMsg.value = 'This job is not currently accepting applications.'
    return
  }

  submitting.value = true
  errorMsg.value = ''
  successMsg.value = ''

  try {
    const newApp = await applicationService.applyForJob({
      job_id: jobId,
      user_id: auth.user.id,
      cover_letter: coverLetter.value.trim() || null,
      resume_id: selectedResumeId.value || null,
      custom_answers: customAnswers.value,
      status: 'applied',
    })

    alreadyApplied.value = true
    existingApplication.value = newApp
    successMsg.value = 'Your application has been submitted successfully!'
    toast.success('Application submitted!')

    if (jobsStore.currentJob) {
      jobsStore.currentJob.applications_count =
        (jobsStore.currentJob.applications_count || 0) + 1
    }

    setTimeout(() => {
      router.push(`/jobs/${jobId}`)
    }, 2000)
  } catch (err: any) {
    errorMsg.value = err.message || 'Failed to submit application. Please try again.'
    toast.error(errorMsg.value)
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <div class="min-h-screen bg-page py-4 px-3 sm:py-6 sm:px-4 md:py-8 md:px-6 lg:py-10 lg:px-8">
    <div class="max-w-3xl mx-auto space-y-4 sm:space-y-5 md:space-y-6">
      <!-- Back button -->
      <button
        type="button"
        @click="router.push(`/jobs/${jobId}`)"
        class="inline-flex items-center gap-1.5 text-xs sm:text-sm font-medium text-text-muted hover:text-text-strong transition"
      >
        <ArrowLeft class="h-3.5 w-3.5 sm:h-4 sm:w-4" />
        <span>Back to Job Details</span>
      </button>

      <!-- Loading State -->
      <div
        v-if="loading"
        class="bg-card rounded-xl sm:rounded-2xl shadow-sm border border-border p-8 sm:p-10 md:p-12 text-center"
      >
        <div
          class="h-7 w-7 sm:h-8 sm:w-8 animate-spin rounded-full border-2 border-primary border-t-transparent mx-auto mb-3"
        ></div>
        <p class="text-xs sm:text-sm text-text-muted">Loading job information...</p>
      </div>

      <!-- Job Not Found -->
      <div
        v-else-if="!job"
        class="bg-card rounded-xl sm:rounded-2xl shadow-sm border border-border p-8 sm:p-10 md:p-12 text-center space-y-3"
      >
        <AlertCircle class="h-9 w-9 sm:h-10 sm:w-10 text-danger mx-auto" />
        <h2 class="text-base sm:text-lg font-bold text-text-strong">Job Not Found</h2>
        <p class="text-xs sm:text-sm text-text-muted max-w-md mx-auto">
          The job you are looking for does not exist or may have been removed.
        </p>
        <button
          type="button"
          @click="router.push('/')"
          class="inline-flex items-center px-4 py-2 text-xs font-semibold rounded-lg text-white bg-primary hover:bg-primary-hover transition"
        >
          Return Home
        </button>
      </div>

      <!-- Main Application Card -->
      <div
        v-else
        class="bg-card rounded-xl sm:rounded-2xl shadow-sm border border-border overflow-hidden"
      >
        <!-- Header banner -->
        <div class="p-4 sm:p-6 md:p-8 bg-surface-raised border-b border-border">
          <div class="flex items-start gap-3 sm:gap-4">
            <div
              class="h-11 w-11 sm:h-12 sm:w-12 md:h-14 md:w-14 rounded-xl sm:rounded-2xl ring-2 ring-border bg-gradient-to-br from-indigo-600 to-indigo-800 overflow-hidden shrink-0 flex items-center justify-center shadow-sm"
            >
              <img
                v-if="job.organization?.avatar_path"
                :src="getOrganizationImageUrl(job.organization.avatar_path) || ''"
                class="h-full w-full object-cover"
                :alt="job.organization?.name || 'Company'"
              />
              <span v-else class="text-base sm:text-lg md:text-xl font-bold text-white select-none">
                {{ (job.organization?.name || 'C').charAt(0).toUpperCase() }}
              </span>
            </div>

            <div class="min-w-0 flex-1">
              <span
                class="text-[10px] sm:text-xs font-semibold uppercase tracking-wider text-primary"
              >
                Job Application
              </span>
              <h1
                class="text-sm sm:text-lg md:text-xl lg:text-2xl font-bold text-text-strong mt-1 line-clamp-2 sm:truncate"
              >
                {{ job.title }}
              </h1>                
              <div class="flex flex-wrap items-center gap-x-1.5 gap-y-0.5 sm:gap-2">
                <span class="text-[10px] sm:text-xs text-text-subtle">•</span>
                <span class="text-[10px] sm:text-xs text-text-muted capitalize">
                  {{ job.job_type.replace('_', ' ') }}
                </span>
                <span
                  v-if="job.work_mode"
                  class="text-[10px] sm:text-xs text-text-muted capitalize"
                >
                  • {{ job.work_mode }}
                </span>
              </div>
              <p class="text-xs sm:text-sm text-text-secondary font-medium mt-0.5 line-clamp-1">
                {{ job.organization?.name }}
                <span v-if="job.city" class="text-text-subtle font-normal"> · {{ job.city }}</span>
              </p>
            </div>
          </div>
        </div>

        <div class="p-4 sm:p-6 md:p-8 space-y-5 sm:space-y-6">
          <!-- ALREADY APPLIED STATE -->
          <div
            v-if="alreadyApplied"
            class="rounded-lg sm:rounded-xl alert-success p-4 sm:p-5 md:p-6 space-y-3"
          >
            <div class="flex items-start gap-2.5 sm:gap-3">
              <CheckCircle2 class="h-5 w-5 sm:h-6 sm:w-6 shrink-0 mt-0.5" />
              <div class="space-y-1 min-w-0 flex-1">
                <h3 class="text-sm sm:text-base font-bold">Application Already Submitted</h3>
                <p class="text-xs sm:text-sm">
                  You submitted an application for this position on
                  <span class="font-semibold">
                    {{
                      existingApplication
                        ? new Date(existingApplication.applied_at).toLocaleDateString()
                        : 'earlier'
                    }}
                  </span>.
                </p>
                <div class="mt-2 flex flex-wrap items-center gap-2">
                  <span class="text-[11px] sm:text-xs font-medium">Status:</span>
                  <span
                    class="inline-flex items-center px-2 sm:px-2.5 py-0.5 rounded-full text-[10px] sm:text-xs font-semibold uppercase tracking-wide bg-success text-white"
                  >
                    {{ existingApplication?.status.replace('_', ' ') || 'Applied' }}
                  </span>
                </div>
              </div>
            </div>

            <div
              v-if="appliedResume"
              class="mt-3 sm:mt-4 pt-3 border-t border-success-border text-xs"
            >
              <span class="font-semibold block mb-1">Attached Resume:</span>
              <div
                class="inline-flex items-center gap-2 bg-card/60 px-3 py-1.5 rounded-lg border border-success-border text-xs"
              >
                <FileText class="h-4 w-4 text-success shrink-0" />
                <span class="font-medium text-text-strong">{{ appliedResume.title }}</span>
                <span v-if="appliedResume.file_size" class="text-text-muted">
                  ({{ formatFileSize(appliedResume.file_size) }})
                </span>
              </div>
            </div>

            <div
              v-if="existingApplication?.cover_letter"
              class="mt-3 sm:mt-4 pt-3 border-t border-success-border text-xs"
            >
              <span class="font-semibold block mb-1">Your Submitted Cover Letter:</span>
              <p
                class="whitespace-pre-line italic bg-card/60 p-2.5 sm:p-3 rounded-lg border border-success-border text-[11px] sm:text-xs leading-relaxed"
              >
                {{ existingApplication.cover_letter }}
              </p>
            </div>

            <div class="pt-1 sm:pt-2">
              <button
                type="button"
                @click="router.push(`/jobs/${jobId}`)"
                class="inline-flex items-center gap-1.5 px-3.5 sm:px-4 py-2 text-[11px] sm:text-xs font-semibold rounded-lg text-white bg-success hover:bg-success-hover transition"
              >
                ← View Job Details
              </button>
            </div>
          </div>

          <!-- NOT ACCEPTING APPLICATIONS / DEADLINE PASSED -->
          <div
            v-else-if="!canApply"
            class="rounded-lg sm:rounded-xl alert-warning p-4 sm:p-5 md:p-6 flex items-start gap-2.5 sm:gap-3"
          >
            <AlertCircle class="h-5 w-5 sm:h-6 sm:w-6 shrink-0 mt-0.5" />
            <div class="space-y-1 min-w-0 flex-1">
              <h3 class="text-sm sm:text-base font-bold">Applications Closed</h3>
              <p class="text-xs sm:text-sm">
                <span v-if="isDeadlinePassed">
                  The application deadline for this job posting has passed.
                </span>
                <span v-else-if="job.accept_applications === false">
                  The organization has paused or closed new applications for this role.
                </span>
                <span v-else> This job is not currently open for new applications. </span>
              </p>
              <div class="pt-1 sm:pt-2">
                <button
                  type="button"
                  @click="router.push(`/jobs/${jobId}`)"
                  class="inline-flex items-center gap-1.5 px-3 py-1.5 text-[11px] sm:text-xs font-semibold rounded-lg text-white bg-warning hover:bg-warning-hover transition"
                >
                  Return to Job Posting
                </button>
              </div>
            </div>
          </div>

          <!-- APPLICATION FORM -->
          <div v-else class="space-y-5 sm:space-y-6">
            <!-- Custom Hiring Instructions Callout -->
            <div
              v-if="job.has_custom_hiring_process && job.how_to_apply"
              class="rounded-xl sm:rounded-2xl border-2 border-primary/40 bg-primary-light p-4 sm:p-5 space-y-2.5 shadow-sm"
            >
              <div class="flex items-center gap-2 text-xs sm:text-sm font-bold text-primary">
                <Sparkles class="h-3.5 w-3.5 sm:h-4 sm:w-4 shrink-0" />
                <span>Special Application Instructions from the Company</span>
              </div>
              <p class="text-[11px] sm:text-xs text-text-secondary leading-relaxed">
                This company has outlined specific instructions for applying to this position.
                Please read carefully:
              </p>
              <div
                class="rich-text-preview text-[11px] sm:text-xs text-text-secondary bg-card/90 p-3 sm:p-4 rounded-lg sm:rounded-xl border border-primary/20 shadow-sm leading-relaxed overflow-x-auto"
                v-html="job.how_to_apply"
              ></div>
            </div>

            <!-- Selection Process Stages Preview -->
            <div
              v-if="job.selection_processes?.length && job.is_selection_process_public"
              class="rounded-lg sm:rounded-xl border border-border bg-surface-raised p-3.5 sm:p-4 space-y-3"
            >
              <div class="flex items-center justify-between gap-2">
                <h4
                  class="text-[10px] sm:text-xs font-bold uppercase tracking-wider text-text-secondary flex items-center gap-1.5"
                >
                  <Clock class="h-3 w-3 sm:h-3.5 sm:w-3.5 text-primary shrink-0" />
                  <span>Selection Pathway</span>
                </h4>
                <span class="text-[10px] sm:text-[11px] text-text-muted font-medium whitespace-nowrap">
                  {{ job.selection_processes.length }}
                  {{ job.selection_processes.length === 1 ? 'stage' : 'stages' }}
                </span>
              </div>
              <div class="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 gap-2 sm:gap-2.5">
                <div
                  v-for="(p, idx) in job.selection_processes
                    .slice()
                    .sort((a, b) => a.order_index - b.order_index)"
                  :key="p.id || idx"
                  class="p-2.5 bg-card rounded-lg border border-border text-xs space-y-1"
                >
                  <div class="flex items-center gap-1.5 min-w-0">
                    <span
                      class="h-4 w-4 rounded-full bg-primary text-white text-[10px] font-bold flex items-center justify-center shrink-0"
                    >
                      {{ idx + 1 }}
                    </span>
                    <span class="font-semibold text-text-strong truncate text-[11px] sm:text-xs">
                      {{ p.title }}
                    </span>
                  </div>
                  <p class="text-[10px] sm:text-[11px] text-text-muted capitalize">
                    {{ p.process_type.replace('_', ' + ') }}
                  </p>
                </div>
              </div>
            </div>

            <!-- APPLICANT PROFILE SUMMARY -->
            <div
              class="rounded-lg sm:rounded-xl border border-border bg-surface-raised p-3.5 sm:p-4 space-y-3"
            >
              <div class="flex items-center justify-between gap-2">
                <h4
                  class="text-[10px] sm:text-xs font-bold uppercase tracking-wider text-text-secondary flex items-center gap-1.5"
                >
                  <User class="h-3 w-3 sm:h-3.5 sm:w-3.5 text-primary shrink-0" />
                  <span>Applying As</span>
                </h4>
                <router-link
                  to="/profile/complete"
                  class="text-[10px] sm:text-[11px] font-medium text-primary hover:underline whitespace-nowrap"
                >
                  Edit Profile
                </router-link>
              </div>

              <div
                class="flex items-center gap-3 sm:gap-3.5 bg-card p-3 sm:p-3.5 rounded-lg border border-border"
              >
                <div
                  class="h-10 w-10 sm:h-11 sm:w-11 md:h-12 md:w-12 rounded-full bg-primary text-white font-bold text-base sm:text-lg flex items-center justify-center overflow-hidden shrink-0 shadow-sm"
                >
                  <img
                    v-if="auth.user?.avatar_path"
                    :src="getProfileImageUrl(auth.user.avatar_path) || ''"
                    class="h-full w-full object-cover"
                    :alt="applicantDisplayName"
                  />
                  <span v-else>{{ applicantInitial }}</span>
                </div>

                <div class="min-w-0 flex-1">
                  <h3 class="text-xs sm:text-sm font-bold text-text-strong truncate">
                    {{ applicantDisplayName }}
                  </h3>
                  <p
                    v-if="auth.user?.headline || auth.user?.current_job_title"
                    class="text-[11px] sm:text-xs text-text-secondary truncate"
                  >
                    {{ auth.user.headline || auth.user.current_job_title }}
                  </p>
                  <div
                    class="flex flex-col sm:flex-row sm:flex-wrap sm:items-center gap-x-3 gap-y-0.5 text-[10px] sm:text-xs text-text-muted mt-1"
                  >
                    <span class="inline-flex items-center gap-1 min-w-0">
                      <Mail class="h-3 w-3 text-text-subtle shrink-0" />
                      <span class="truncate">{{ auth.user?.email }}</span>
                    </span>
                    <span
                      v-if="auth.user?.city"
                      class="inline-flex items-center gap-1 min-w-0"
                    >
                      <MapPin class="h-3 w-3 text-text-subtle shrink-0" />
                      <span class="truncate">{{ auth.user.city }}</span>
                    </span>
                  </div>
                </div>
              </div>
            </div>

            <!-- Feedback Alerts -->
            <div
              v-if="errorMsg"
              class="alert alert-error p-3 sm:p-3.5 text-[11px] sm:text-xs font-medium rounded-lg"
            >
              {{ errorMsg }}
            </div>
            <div
              v-if="successMsg"
              class="alert alert-success p-3 sm:p-3.5 text-[11px] sm:text-xs font-medium rounded-lg"
            >
              {{ successMsg }}
            </div>

            <!-- Resume Selection -->
            <div class="space-y-2.5">
              <div class="flex items-center justify-between">
                <label
                  class="block text-[10px] sm:text-xs font-bold uppercase tracking-wider text-text-secondary"
                >
                  Resume / CV
                  <span class="text-text-subtle font-normal normal-case">(Recommended)</span>
                </label>
                <router-link
                  to="/resumes"
                  class="text-[10px] sm:text-[11px] font-semibold text-primary hover:underline flex items-center gap-1"
                >
                  <Plus class="h-3 w-3" />
                  <span>Manage Resumes</span>
                </router-link>
              </div>

              <!-- Loading State -->
              <div
                v-if="loadingResumes"
                class="py-3 text-xs text-text-muted flex items-center gap-2"
              >
                <div
                  class="h-3.5 w-3.5 animate-spin rounded-full border-2 border-primary border-t-transparent"
                ></div>
                <span>Loading your resumes...</span>
              </div>

              <!-- User has resumes -->
              <div v-else-if="userResumes.length > 0" class="space-y-2">
                <div class="grid grid-cols-1 sm:grid-cols-2 gap-2.5">
                  <label
                    v-for="r in userResumes"
                    :key="r.id"
                    class="relative flex items-start gap-3 p-3 rounded-xl border cursor-pointer transition select-none"
                    :class="
                      selectedResumeId === r.id
                        ? 'border-primary bg-primary-light/40 shadow-sm ring-1 ring-primary'
                        : 'border-border bg-card hover:border-border-strong hover:bg-surface-raised'
                    "
                  >
                    <input
                      type="radio"
                      name="resume-selection"
                      :value="r.id"
                      v-model="selectedResumeId"
                      class="mt-0.5 h-4 w-4 text-primary focus:ring-primary border-border"
                    />
                    <div class="min-w-0 flex-1">
                      <div class="flex items-center gap-1.5">
                        <span class="text-xs font-bold text-text-strong truncate">
                          {{ r.title }}
                        </span>
                        <span
                          v-if="r.is_primary"
                          class="px-1.5 py-0.2 text-[9px] font-semibold rounded bg-primary-light text-primary border border-primary/20 shrink-0"
                        >
                          Primary
                        </span>
                      </div>
                      <p class="text-[11px] text-text-muted truncate mt-0.5">
                        {{ r.file_name || 'Document' }}
                        <span v-if="r.file_size"> • {{ formatFileSize(r.file_size) }}</span>
                      </p>
                    </div>
                  </label>
                </div>

                <div class="flex items-center justify-between pt-0.5">
                  <button
                    v-if="selectedResumeId"
                    type="button"
                    @click="selectedResumeId = null"
                    class="text-[11px] text-text-muted hover:text-text-strong hover:underline"
                  >
                    Don't attach a resume
                  </button>
                  <span v-else class="text-[11px] text-text-muted italic">
                    No resume selected (submitting application without resume).
                  </span>
                </div>
              </div>

              <!-- No resumes uploaded yet -->
              <div
                v-else
                class="p-3.5 sm:p-4 rounded-xl border border-dashed border-border bg-surface-raised flex flex-col sm:flex-row sm:items-center justify-between gap-3"
              >
                <div class="flex items-start gap-3 min-w-0">
                  <div
                    class="h-8 w-8 rounded-lg bg-card border border-border flex items-center justify-center text-text-muted shrink-0"
                  >
                    <FileText class="h-4 w-4" />
                  </div>
                  <div class="min-w-0">
                    <p class="text-xs font-semibold text-text-strong">
                      You haven't uploaded any resumes yet
                    </p>
                    <p class="text-[11px] text-text-muted">
                      Uploading a resume helps employers assess your fit for this role.
                    </p>
                  </div>
                </div>

                <router-link
                  to="/resumes"
                  class="inline-flex items-center justify-center gap-1.5 px-3 py-1.5 text-xs font-semibold rounded-lg bg-primary text-white hover:bg-primary-hover transition shrink-0 shadow-sm"
                >
                  <ArrowUpFromLineIcon class="h-3.5 w-3.5" />
                  <span>Upload Resume</span>
                </router-link>
              </div>
            </div>

            <!-- Cover Letter Input -->
            <div class="space-y-1.5">
              <label
                for="cover-letter"
                class="block text-[10px] sm:text-xs font-bold uppercase tracking-wider text-text-secondary"
              >
                Cover Letter / Pitch
                <span class="text-text-subtle font-normal normal-case">(Optional)</span>
              </label>
              <p class="text-[11px] sm:text-xs text-text-muted leading-relaxed">
                Introduce yourself, your key accomplishments, and explain why you are the ideal
                candidate for this role.
              </p>
              <textarea
                id="cover-letter"
                v-model="coverLetter"
                rows="6"
                placeholder="Dear Hiring Team, I am writing to express my enthusiastic interest in the role..."
                class="block w-full rounded-lg sm:rounded-xl border border-border bg-card p-3 sm:p-3.5 text-xs sm:text-sm text-text-strong placeholder:text-text-subtle shadow-sm focus:ring-1 focus:ring-primary focus:border-primary resize-y leading-relaxed"
              ></textarea>
            </div>

            <!-- Submit Button -->
            <div
              class="pt-3 border-t border-border flex flex-col-reverse sm:flex-row sm:items-center sm:justify-between gap-3 sm:gap-4"
            >
              <p class="text-[10px] sm:text-xs text-text-muted leading-relaxed sm:max-w-md">
                By submitting, your profile and application details will be sent to
                {{ job.organization?.name }}.
              </p>

              <button
                type="button"
                :disabled="submitting"
                @click="handleApply"
                class="w-full sm:w-auto inline-flex items-center justify-center gap-2 px-6 sm:px-8 py-2.5 sm:py-3 text-xs sm:text-sm font-semibold rounded-lg sm:rounded-xl text-white bg-primary hover:bg-primary-hover disabled:opacity-50 transition shadow-sm shrink-0"
              >
                <div
                  v-if="submitting"
                  class="h-3.5 w-3.5 sm:h-4 sm:w-4 animate-spin rounded-full border-2 border-white border-t-transparent"
                ></div>
                <Send v-else class="h-3.5 w-3.5 sm:h-4 sm:w-4" />
                <span>{{ submitting ? 'Submitting...' : 'Submit Application' }}</span>
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
