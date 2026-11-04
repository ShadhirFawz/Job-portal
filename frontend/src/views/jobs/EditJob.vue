<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { jobService } from '@/services/job.service'
import { questionBankService } from '@/services/questionBank.service'
import { useConfirm } from '@/composables/useConfirm'
import type { Job, JobType, WorkMode, ExperienceLevel } from '@/types/job'
import type { QuestionBank } from '@/types/assessment'
import RichTextEditor from '@/components/ui/RichTextEditor.vue'
import {
  Briefcase,
  Layers,
  FileText,
  DollarSign,
  MapPin,
  Clock,
  Sparkles,
  Plus,
  Trash2,
  AlertCircle,
  Loader2,
  ArrowLeft,
  Save,
  Send,
  ShieldAlert,
} from 'lucide-vue-next'
import { toast } from 'vue-sonner'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const { confirm: showConfirm } = useConfirm()

const jobId = route.params.id as string

const loadingJob = ref(true)
const saving = ref(false)
const errorMsg = ref('')
const job = ref<Job | null>(null)

// Question banks for selection processes
const questionBanks = ref<QuestionBank[]>([])
const loadingBanks = ref(false)

// Form fields
const title = ref('')
const shortDescription = ref('')
const description = ref('')
const responsibilities = ref('')
const requirements = ref('')
const niceToHave = ref('')
const whatWeOffer = ref('')
const benefits = ref('')

const techStacks = ref<string[]>([])
const techStackInput = ref('')
const commonTechStacks = [
  'TypeScript', 'JavaScript', 'Vue.js', 'React', 'Node.js',
  'Python', 'Go', 'Java', 'C# / .NET', 'PostgreSQL',
  'Docker', 'Kubernetes', 'AWS', 'GCP', 'Tailwind CSS',
]

const hasCustomHiringProcess = ref(false)
const howToApply = ref('')
const isSelectionProcessPublic = ref(true)

interface FormProcess {
  id?: string
  title: string
  process_type: string
  description: string
  is_mandatory: boolean
  time_limit_minutes: number | null
  question_bank_id: string | null
}

const processes = ref<FormProcess[]>([])

const jobType = ref<JobType>('full_time')
const workMode = ref<WorkMode>('onsite')
const experienceLevel = ref<ExperienceLevel | ''>('mid')
const department = ref('')
const category = ref('')
const countryCode = ref('')
const city = ref('')

const salaryMin = ref<number | null>(null)
const salaryMax = ref<number | null>(null)
const salaryCurrency = ref('INR')
const showSalary = ref(false)

const applicationDeadline = ref('')
const acceptApplications = ref(true)
const jobStatus = ref('draft')

const isOwner = computed(() => {
  if (!auth.user || !job.value) return false
  return job.value.created_by === auth.user.id
})

onMounted(async () => {
  if (!jobId) {
    loadingJob.value = false
    return
  }

  try {
    const fetched = await jobService.fetchJobById(jobId, false)
    if (!fetched) {
      errorMsg.value = 'Job posting not found.'
      return
    }

    job.value = fetched

    // Check ownership
    if (fetched.created_by !== auth.user?.id) {
      errorMsg.value = 'Unauthorized: Only the creator of this job posting can edit it.'
      return
    }

    // Populate form
    title.value = fetched.title || ''
    shortDescription.value = fetched.short_description || ''
    description.value = fetched.description || ''
    responsibilities.value = fetched.responsibilities || ''
    requirements.value = fetched.requirements || ''
    niceToHave.value = fetched.nice_to_have || ''
    whatWeOffer.value = fetched.what_we_offer || fetched.benefits || ''
    benefits.value = fetched.benefits || ''

    techStacks.value = fetched.tech_stacks || []

    hasCustomHiringProcess.value = !!fetched.has_custom_hiring_process
    howToApply.value = fetched.how_to_apply || ''
    isSelectionProcessPublic.value = fetched.is_selection_process_public !== false

    jobType.value = (fetched.job_type as JobType) || 'full_time'
    workMode.value = (fetched.work_mode as WorkMode) || 'onsite'
    experienceLevel.value = (fetched.experience_level as ExperienceLevel) || ''
    department.value = fetched.department || ''
    category.value = fetched.category || ''
    countryCode.value = fetched.country_code || ''
    city.value = fetched.city || ''

    salaryMin.value = fetched.salary_min ?? null
    salaryMax.value = fetched.salary_max ?? null
    salaryCurrency.value = fetched.salary_currency || 'INR'
    showSalary.value = !!fetched.show_salary

    if (fetched.application_deadline) {
      try {
        applicationDeadline.value = fetched.application_deadline.split('T')[0] ?? ''
      } catch {
        applicationDeadline.value = ''
      }
    }
    acceptApplications.value = fetched.accept_applications !== false
    jobStatus.value = fetched.status || 'draft'

    // Populate selection processes
    if (fetched.selection_processes && fetched.selection_processes.length > 0) {
      processes.value = fetched.selection_processes
        .slice()
        .sort((a, b) => a.order_index - b.order_index)
        .map(p => ({
          id: p.id,
          title: p.title || '',
          process_type: p.process_type || 'screening',
          description: p.description || '',
          is_mandatory: p.is_mandatory !== false,
          time_limit_minutes: p.time_limit_minutes ?? null,
          question_bank_id: p.question_bank_id || (p.config?.question_bank_id as string) || null,
        }))
    } else if (!hasCustomHiringProcess.value) {
      processes.value = [
        {
          title: 'Resume Screening',
          process_type: 'screening',
          description: 'Initial review of candidate experience and qualifications.',
          is_mandatory: true,
          time_limit_minutes: null,
          question_bank_id: null,
        }
      ]
    }

    // Load organization question banks
    if (fetched.organization_id) {
      loadingBanks.value = true
      try {
        questionBanks.value = await questionBankService.getOrganizationBanks(fetched.organization_id)
      } catch (err) {
        console.error('Failed to load question banks:', err)
      } finally {
        loadingBanks.value = false
      }
    }
  } catch (err: any) {
    errorMsg.value = err.message || 'Failed to load job posting'
  } finally {
    loadingJob.value = false
  }
})

function addTechStack(tag?: string) {
  const val = (tag || techStackInput.value).trim()
  if (!val) return
  if (!techStacks.value.includes(val)) {
    techStacks.value.push(val)
  }
  techStackInput.value = ''
}

function removeTechStack(index: number) {
  techStacks.value.splice(index, 1)
}

function addProcess() {
  if (processes.value.length >= 3) return
  processes.value.push({
    title: '',
    process_type: 'quiz',
    description: '',
    is_mandatory: true,
    time_limit_minutes: null,
    question_bank_id: null,
  })
}

async function removeProcess(index: number) {
  const proc = processes.value[index]
  if (proc && (proc.title || proc.question_bank_id)) {
    const confirmed = await showConfirm({
      title: 'Remove Stage?',
      message: `Are you sure you want to remove "${proc.title || proc.process_type}"? Any configurations for this stage will be discarded.`,
      confirmLabel: 'Remove Stage',
      variant: 'danger',
    })
    if (!confirmed) return
  }
  processes.value.splice(index, 1)
}

async function saveJob(publish = false) {
  if (!title.value.trim()) {
    errorMsg.value = 'Job title is required'
    toast.error('Job title is required')
    return
  }

  if (!auth.user || !job.value) return

  saving.value = true
  errorMsg.value = ''

  try {
    const payload = {
      title: title.value.trim(),
      short_description: shortDescription.value.trim() || null,
      description: description.value.trim() || null,
      responsibilities: responsibilities.value.trim() || null,
      requirements: requirements.value.trim() || null,
      nice_to_have: niceToHave.value.trim() || null,
      what_we_offer: whatWeOffer.value.trim() || null,
      benefits: benefits.value.trim() || whatWeOffer.value.trim() || null,
      tech_stacks: techStacks.value,
      has_custom_hiring_process: hasCustomHiringProcess.value,
      how_to_apply: hasCustomHiringProcess.value ? howToApply.value.trim() || null : null,
      is_selection_process_public: isSelectionProcessPublic.value,
      job_type: jobType.value,
      work_mode: workMode.value,
      experience_level: experienceLevel.value || null,
      department: department.value.trim() || null,
      category: category.value.trim() || null,
      city: city.value.trim() || null,
      country_code: countryCode.value.trim() || null,
      salary_min: salaryMin.value,
      salary_max: salaryMax.value,
      salary_currency: salaryCurrency.value,
      show_salary: showSalary.value,
      application_deadline: applicationDeadline.value || null,
      accept_applications: acceptApplications.value,
      status: publish ? 'published' : jobStatus.value,
      selection_processes: !hasCustomHiringProcess.value
        ? processes.value.map((p, index) => ({
            title: p.title.trim() || `Stage ${index + 1}`,
            process_type: p.process_type,
            description: p.description.trim() || null,
            order_index: index + 1,
            is_mandatory: p.is_mandatory,
            time_limit_minutes: p.time_limit_minutes,
            question_bank_id: p.question_bank_id || null,
          }))
        : [],
    }

    await jobService.updateJob(job.value.id, auth.user.id, payload)
    toast.success(publish ? 'Job published successfully!' : 'Job updated successfully!')
    router.push(`/jobs/${job.value.id}`)
  } catch (err: any) {
    console.error(err)
    errorMsg.value = err.message || 'Failed to update job post'
    toast.error(errorMsg.value)
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <div class="min-h-screen bg-page py-6 sm:py-10 px-3 sm:px-6 lg:px-8">
    <div class="max-w-4xl mx-auto space-y-6 sm:space-y-8">

      <!-- Loading State -->
      <div v-if="loadingJob" class="flex flex-col items-center justify-center min-h-[50vh] text-text-muted">
        <Loader2 class="h-8 w-8 animate-spin text-primary mb-3" />
        <p class="text-sm">Loading job posting details…</p>
      </div>

      <!-- Access Denied or Not Found State -->
      <div v-else-if="!isOwner" class="p-8 sm:p-12 text-center bg-card rounded-2xl border border-border shadow-sm space-y-4">
        <ShieldAlert class="h-12 w-12 text-rose-500 mx-auto" />
        <h2 class="text-lg sm:text-xl font-bold text-text-strong">Access Denied</h2>
        <p class="text-sm text-text-muted max-w-md mx-auto">
          {{ errorMsg || 'You do not have permission to edit this job posting. Only the owner/creator can make changes.' }}
        </p>
        <div class="pt-2">
          <button
            type="button"
            @click="router.back()"
            class="inline-flex items-center gap-1.5 px-4 py-2 text-xs sm:text-sm font-semibold rounded-lg bg-surface-raised border border-border text-text-strong hover:bg-border transition cursor-pointer"
          >
            <ArrowLeft class="h-4 w-4" /> Go Back
          </button>
        </div>
      </div>

      <!-- Form Content -->
      <div v-else class="space-y-6 sm:space-y-8">

        <!-- Header -->
        <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4 pb-4 border-b border-border">
          <div>
            <div class="flex items-center gap-2 mb-1.5">
              <button
                type="button"
                @click="router.push(`/jobs/${jobId}`)"
                class="inline-flex items-center gap-1 text-xs font-semibold text-text-muted hover:text-text-strong transition"
              >
                <ArrowLeft class="h-3.5 w-3.5" /> Back to Job Details
              </button>
            </div>
            <h1 class="text-2xl sm:text-3xl font-bold tracking-tight text-text-strong">
              Edit Job Posting
            </h1>
            <p class="text-xs sm:text-sm text-text-secondary mt-1">
              Update position details, requirements, and evaluation stages.
            </p>
          </div>

          <div class="flex items-center gap-2">
            <span
              class="px-2.5 py-1 text-xs font-semibold rounded-full capitalize border"
              :class="{
                'badge-success': jobStatus === 'published',
                'badge-neutral': jobStatus === 'draft',
                'badge-warning': jobStatus === 'paused',
                'badge-danger': jobStatus === 'closed',
              }"
            >
              {{ jobStatus }}
            </span>
          </div>
        </div>

        <!-- Alert messages -->
        <div v-if="errorMsg" class="p-4 rounded-xl bg-rose-500/10 border border-rose-500/20 text-rose-700 dark:text-rose-300 text-xs sm:text-sm flex items-center gap-2">
          <AlertCircle class="h-4 w-4 flex-shrink-0" />
          <span>{{ errorMsg }}</span>
        </div>

        <!-- Form Card 1: Basic Information -->
        <div class="bg-card rounded-xl sm:rounded-2xl border border-border p-5 sm:p-7 shadow-xs space-y-5">
          <div class="flex items-center gap-2.5 border-b border-border pb-3">
            <Briefcase class="h-5 w-5 text-primary flex-shrink-0" />
            <h2 class="text-base sm:text-lg font-bold text-text-strong">Basic Job Details</h2>
          </div>

          <div>
            <label class="block text-xs font-semibold text-text-secondary uppercase tracking-wider mb-1.5">
              Job Title <span class="text-rose-500">*</span>
            </label>
            <input
              v-model="title"
              type="text"
              placeholder="e.g. Senior Full-Stack Engineer"
              class="w-full rounded-lg border border-border bg-page px-3.5 py-2.5 text-xs sm:text-sm text-text-strong placeholder:text-text-subtle focus:border-primary focus:ring-1 focus:ring-primary shadow-xs"
            />
          </div>

          <div>
            <label class="block text-xs font-semibold text-text-secondary uppercase tracking-wider mb-1.5">
              Short Summary
            </label>
            <textarea
              v-model="shortDescription"
              rows="2"
              maxlength="250"
              placeholder="A concise 1-2 sentence hook highlighting the role."
              class="w-full rounded-lg border border-border bg-page px-3.5 py-2.5 text-xs sm:text-sm text-text-strong placeholder:text-text-subtle focus:border-primary focus:ring-1 focus:ring-primary shadow-xs resize-none"
            ></textarea>
            <div class="text-right text-[11px] text-text-subtle mt-1">{{ shortDescription.length }}/250</div>
          </div>

          <div class="grid grid-cols-1 sm:grid-cols-3 gap-4">
            <div>
              <label class="block text-xs font-semibold text-text-secondary uppercase tracking-wider mb-1.5">
                Employment Type
              </label>
              <select
                v-model="jobType"
                class="w-full rounded-lg border border-border bg-page px-3 py-2 text-xs sm:text-sm text-text-strong focus:border-primary focus:ring-1 focus:ring-primary shadow-xs"
              >
                <option value="full_time">Full-time</option>
                <option value="part_time">Part-time</option>
                <option value="contract">Contract</option>
                <option value="internship">Internship</option>
                <option value="freelance">Freelance</option>
              </select>
            </div>

            <div>
              <label class="block text-xs font-semibold text-text-secondary uppercase tracking-wider mb-1.5">
                Work Mode
              </label>
              <select
                v-model="workMode"
                class="w-full rounded-lg border border-border bg-page px-3 py-2 text-xs sm:text-sm text-text-strong focus:border-primary focus:ring-1 focus:ring-primary shadow-xs"
              >
                <option value="onsite">On-site</option>
                <option value="hybrid">Hybrid</option>
                <option value="remote">Remote</option>
              </select>
            </div>

            <div>
              <label class="block text-xs font-semibold text-text-secondary uppercase tracking-wider mb-1.5">
                Experience Level
              </label>
              <select
                v-model="experienceLevel"
                class="w-full rounded-lg border border-border bg-page px-3 py-2 text-xs sm:text-sm text-text-strong focus:border-primary focus:ring-1 focus:ring-primary shadow-xs"
              >
                <option value="">Not Specified</option>
                <option value="entry">Entry Level</option>
                <option value="junior">Junior</option>
                <option value="mid">Mid Level</option>
                <option value="senior">Senior</option>
                <option value="lead">Lead / Staff</option>
                <option value="executive">Executive</option>
              </select>
            </div>
          </div>

          <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div>
              <label class="block text-xs font-semibold text-text-secondary uppercase tracking-wider mb-1.5">
                Department
              </label>
              <input
                v-model="department"
                type="text"
                placeholder="e.g. Engineering, Product, Design"
                class="w-full rounded-lg border border-border bg-page px-3.5 py-2.5 text-xs sm:text-sm text-text-strong placeholder:text-text-subtle focus:border-primary focus:ring-1 focus:ring-primary shadow-xs"
              />
            </div>

            <div>
              <label class="block text-xs font-semibold text-text-secondary uppercase tracking-wider mb-1.5">
                Role Category
              </label>
              <input
                v-model="category"
                type="text"
                placeholder="e.g. Frontend Development, DevOps"
                class="w-full rounded-lg border border-border bg-page px-3.5 py-2.5 text-xs sm:text-sm text-text-strong placeholder:text-text-subtle focus:border-primary focus:ring-1 focus:ring-primary shadow-xs"
              />
            </div>
          </div>

          <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div>
              <label class="block text-xs font-semibold text-text-secondary uppercase tracking-wider mb-1.5">
                City / Location
              </label>
              <input
                v-model="city"
                type="text"
                placeholder="e.g. Bengaluru, San Francisco"
                class="w-full rounded-lg border border-border bg-page px-3.5 py-2.5 text-xs sm:text-sm text-text-strong placeholder:text-text-subtle focus:border-primary focus:ring-1 focus:ring-primary shadow-xs"
              />
            </div>

            <div>
              <label class="block text-xs font-semibold text-text-secondary uppercase tracking-wider mb-1.5">
                Country Code
              </label>
              <input
                v-model="countryCode"
                type="text"
                placeholder="e.g. IN, US, GB"
                class="w-full rounded-lg border border-border bg-page px-3.5 py-2.5 text-xs sm:text-sm text-text-strong placeholder:text-text-subtle focus:border-primary focus:ring-1 focus:ring-primary shadow-xs"
              />
            </div>
          </div>
        </div>

        <!-- Form Card 2: Tech Stacks -->
        <div class="bg-card rounded-xl sm:rounded-2xl border border-border p-5 sm:p-7 shadow-xs space-y-4">
          <div class="flex items-center gap-2.5 border-b border-border pb-3">
            <Sparkles class="h-5 w-5 text-primary flex-shrink-0" />
            <h2 class="text-base sm:text-lg font-bold text-text-strong">Tech Stacks & Skills</h2>
          </div>

          <div>
            <div class="flex gap-2">
              <input
                v-model="techStackInput"
                type="text"
                placeholder="Type a skill/technology and press Enter"
                @keydown.enter.prevent="addTechStack()"
                class="flex-1 rounded-lg border border-border bg-page px-3.5 py-2 text-xs sm:text-sm text-text-strong placeholder:text-text-subtle focus:border-primary focus:ring-1 focus:ring-primary shadow-xs"
              />
              <button
                type="button"
                @click="addTechStack()"
                class="px-4 py-2 text-xs font-semibold rounded-lg bg-surface-raised border border-border hover:bg-border text-text-strong transition cursor-pointer"
              >
                Add
              </button>
            </div>

            <!-- Active tags -->
            <div v-if="techStacks.length > 0" class="flex flex-wrap gap-2 mt-3">
              <span
                v-for="(tech, index) in techStacks"
                :key="tech"
                class="inline-flex items-center gap-1.5 px-3 py-1 text-xs font-medium rounded-lg bg-primary-light text-primary border border-primary/20"
              >
                {{ tech }}
                <button
                  type="button"
                  @click="removeTechStack(index)"
                  class="hover:text-rose-600 transition"
                >
                  &times;
                </button>
              </span>
            </div>

            <!-- Suggested tags -->
            <div class="mt-4 pt-3 border-t border-border/60">
              <span class="text-xs text-text-muted mr-2">Suggestions:</span>
              <div class="inline-flex flex-wrap gap-1.5 mt-1">
                <button
                  v-for="s in commonTechStacks"
                  :key="s"
                  type="button"
                  @click="addTechStack(s)"
                  :disabled="techStacks.includes(s)"
                  class="px-2.5 py-0.5 text-[11px] rounded-md border border-border text-text-secondary hover:bg-surface-raised disabled:opacity-30 disabled:cursor-not-allowed transition"
                >
                  + {{ s }}
                </button>
              </div>
            </div>
          </div>
        </div>

        <!-- Form Card 3: Role Content (Rich Text) -->
        <div class="bg-card rounded-xl sm:rounded-2xl border border-border p-5 sm:p-7 shadow-xs space-y-6">
          <div class="flex items-center gap-2.5 border-b border-border pb-3">
            <FileText class="h-5 w-5 text-primary flex-shrink-0" />
            <h2 class="text-base sm:text-lg font-bold text-text-strong">Job Description & Details</h2>
          </div>

          <div>
            <label class="block text-xs font-semibold text-text-secondary uppercase tracking-wider mb-2">
              Full Job Description
            </label>
            <RichTextEditor v-model="description" placeholder="Introduce the company, mission, and the role..." />
          </div>

          <div>
            <label class="block text-xs font-semibold text-text-secondary uppercase tracking-wider mb-2">
              Responsibilities
            </label>
            <RichTextEditor v-model="responsibilities" placeholder="Key responsibilities and day-to-day duties..." />
          </div>

          <div>
            <label class="block text-xs font-semibold text-text-secondary uppercase tracking-wider mb-2">
              Requirements & Qualifications
            </label>
            <RichTextEditor v-model="requirements" placeholder="Must-have qualifications, skills, and experience..." />
          </div>

          <div>
            <label class="block text-xs font-semibold text-text-secondary uppercase tracking-wider mb-2">
              Nice to Have
            </label>
            <RichTextEditor v-model="niceToHave" placeholder="Bonus qualifications, preferred domains..." />
          </div>

          <div>
            <label class="block text-xs font-semibold text-text-secondary uppercase tracking-wider mb-2">
              What We Offer / Benefits
            </label>
            <RichTextEditor v-model="whatWeOffer" placeholder="Perks, healthcare, equity, flexible work, learning budget..." />
          </div>
        </div>

        <!-- Form Card 4: Hiring Process & Stages -->
        <div class="bg-card rounded-xl sm:rounded-2xl border border-border p-5 sm:p-7 shadow-xs space-y-5">
          <div class="flex items-center justify-between border-b border-border pb-3">
            <div class="flex items-center gap-2.5">
              <Layers class="h-5 w-5 text-primary flex-shrink-0" />
              <h2 class="text-base sm:text-lg font-bold text-text-strong">Selection Process</h2>
            </div>
          </div>

          <!-- Custom hiring process toggle -->
          <div class="flex items-center gap-3 p-3.5 rounded-xl bg-surface-raised border border-border">
            <input
              id="customHiringProcess"
              v-model="hasCustomHiringProcess"
              type="checkbox"
              class="h-4 w-4 rounded border-border text-primary focus:ring-primary cursor-pointer"
            />
            <label for="customHiringProcess" class="text-xs sm:text-sm font-medium text-text-strong cursor-pointer">
              External / Custom application instructions (bypass platform stages)
            </label>
          </div>

          <!-- Custom instructions editor -->
          <div v-if="hasCustomHiringProcess" class="space-y-2 pt-2">
            <label class="block text-xs font-semibold text-text-secondary uppercase tracking-wider mb-1">
              Instructions for Candidates
            </label>
            <RichTextEditor v-model="howToApply" placeholder="Provide direct application link, email, or specific instructions..." />
          </div>

          <!-- Platform standard stages -->
          <div v-else class="space-y-4">
            <div class="flex items-center justify-between">
              <p class="text-xs text-text-muted">
                Configure evaluation stages for candidates applying via the platform (up to 3 stages).
              </p>
              <button
                v-if="processes.length < 3"
                type="button"
                @click="addProcess"
                class="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold rounded-lg bg-surface-raised border border-border text-text-strong hover:bg-border transition cursor-pointer"
              >
                <Plus class="h-3.5 w-3.5" /> Add Stage
              </button>
            </div>

            <div class="space-y-4">
              <div
                v-for="(p, index) in processes"
                :key="index"
                class="p-4 sm:p-5 rounded-xl border border-border bg-page space-y-4"
              >
                <div class="flex items-center justify-between">
                  <div class="flex items-center gap-2">
                    <span class="flex h-6 w-6 items-center justify-center rounded-full bg-primary text-white text-xs font-bold">
                      {{ index + 1 }}
                    </span>
                    <h3 class="text-xs sm:text-sm font-bold text-text-strong">
                      Stage {{ index + 1 }} Configuration
                    </h3>
                  </div>
                  <button
                    v-if="processes.length > 1"
                    type="button"
                    @click="removeProcess(index)"
                    class="p-1 rounded text-text-muted hover:text-rose-600 transition"
                    title="Remove Stage"
                  >
                    <Trash2 class="h-4 w-4" />
                  </button>
                </div>

                <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
                  <div>
                    <label class="block text-xs font-semibold text-text-secondary uppercase tracking-wider mb-1">
                      Stage Title
                    </label>
                    <input
                      v-model="p.title"
                      type="text"
                      placeholder="e.g. Technical Quiz, Screening Call"
                      class="w-full rounded-lg border border-border bg-card px-3 py-2 text-xs sm:text-sm text-text-strong placeholder:text-text-subtle focus:border-primary focus:ring-1 focus:ring-primary shadow-xs"
                    />
                  </div>

                  <div>
                    <label class="block text-xs font-semibold text-text-secondary uppercase tracking-wider mb-1">
                      Stage Type
                    </label>
                    <select
                      v-model="p.process_type"
                      class="w-full rounded-lg border border-border bg-card px-3 py-2 text-xs sm:text-sm text-text-strong focus:border-primary focus:ring-1 focus:ring-primary shadow-xs"
                    >
                      <option value="screening">Screening</option>
                      <option value="quiz">Assessment Quiz</option>
                      <option value="coding">Coding Challenge</option>
                      <option value="custom">Custom / Other</option>
                    </select>
                  </div>
                </div>

                <!-- Quiz question bank selection -->
                <div v-if="p.process_type === 'quiz' || p.process_type.includes('quiz')" class="p-3.5 rounded-lg bg-surface-raised border border-border space-y-2">
                  <div class="flex items-center justify-between">
                    <label class="block text-xs font-semibold text-text-strong">
                      Select Question Bank for Quiz
                    </label>
                  </div>
                  <select
                    v-model="p.question_bank_id"
                    class="w-full rounded-lg border border-border bg-card px-3 py-2 text-xs sm:text-sm text-text-strong focus:border-primary focus:ring-1 focus:ring-primary shadow-xs"
                  >
                    <option :value="null">-- Choose a question bank --</option>
                    <option
                      v-for="bank in questionBanks"
                      :key="bank.id"
                      :value="bank.id"
                    >
                      {{ bank.name }} ({{ bank.question_count || bank.questions?.length || 0 }} questions)
                    </option>
                  </select>
                </div>

                <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
                  <div>
                    <label class="block text-xs font-semibold text-text-secondary uppercase tracking-wider mb-1">
                      Time Limit (Minutes)
                    </label>
                    <input
                      v-model.number="p.time_limit_minutes"
                      type="number"
                      min="1"
                      placeholder="e.g. 45 (leave empty for untimed)"
                      class="w-full rounded-lg border border-border bg-card px-3 py-2 text-xs sm:text-sm text-text-strong placeholder:text-text-subtle focus:border-primary focus:ring-1 focus:ring-primary shadow-xs"
                    />
                  </div>

                  <div class="flex items-center pt-6">
                    <label class="inline-flex items-center gap-2 text-xs sm:text-sm text-text-strong cursor-pointer select-none">
                      <input
                        v-model="p.is_mandatory"
                        type="checkbox"
                        class="h-4 w-4 rounded border-border text-primary focus:ring-primary cursor-pointer"
                      />
                      Mandatory stage (must be completed to advance)
                    </label>
                  </div>
                </div>

                <div>
                  <label class="block text-xs font-semibold text-text-secondary uppercase tracking-wider mb-1">
                    Stage Description / Instructions
                  </label>
                  <textarea
                    v-model="p.description"
                    rows="2"
                    placeholder="Instructions shown to candidates for this stage..."
                    class="w-full rounded-lg border border-border bg-card px-3 py-2 text-xs sm:text-sm text-text-strong placeholder:text-text-subtle focus:border-primary focus:ring-1 focus:ring-primary shadow-xs resize-none"
                  ></textarea>
                </div>
              </div>
            </div>

            <!-- Public visibility of stages -->
            <div class="flex items-center gap-2 pt-2">
              <input
                id="isSelectionPublic"
                v-model="isSelectionProcessPublic"
                type="checkbox"
                class="h-4 w-4 rounded border-border text-primary focus:ring-primary cursor-pointer"
              />
              <label for="isSelectionPublic" class="text-xs sm:text-sm text-text-secondary cursor-pointer select-none">
                Make selection process stages publicly visible on the job detail page
              </label>
            </div>
          </div>
        </div>

        <!-- Form Card 5: Compensation & Details -->
        <div class="bg-card rounded-xl sm:rounded-2xl border border-border p-5 sm:p-7 shadow-xs space-y-4">
          <div class="flex items-center gap-2.5 border-b border-border pb-3">
            <DollarSign class="h-5 w-5 text-primary flex-shrink-0" />
            <h2 class="text-base sm:text-lg font-bold text-text-strong">Compensation & Settings</h2>
          </div>

          <div class="grid grid-cols-1 sm:grid-cols-3 gap-4">
            <div>
              <label class="block text-xs font-semibold text-text-secondary uppercase tracking-wider mb-1.5">
                Minimum Salary
              </label>
              <input
                v-model.number="salaryMin"
                type="number"
                min="0"
                step="1000"
                placeholder="e.g. 500000"
                class="w-full rounded-lg border border-border bg-page px-3 py-2 text-xs sm:text-sm text-text-strong focus:border-primary focus:ring-1 focus:ring-primary shadow-xs"
              />
            </div>

            <div>
              <label class="block text-xs font-semibold text-text-secondary uppercase tracking-wider mb-1.5">
                Maximum Salary
              </label>
              <input
                v-model.number="salaryMax"
                type="number"
                min="0"
                step="1000"
                placeholder="e.g. 900000"
                class="w-full rounded-lg border border-border bg-page px-3 py-2 text-xs sm:text-sm text-text-strong focus:border-primary focus:ring-1 focus:ring-primary shadow-xs"
              />
            </div>

            <div>
              <label class="block text-xs font-semibold text-text-secondary uppercase tracking-wider mb-1.5">
                Currency
              </label>
              <select
                v-model="salaryCurrency"
                class="w-full rounded-lg border border-border bg-page px-3 py-2 text-xs sm:text-sm text-text-strong focus:border-primary focus:ring-1 focus:ring-primary shadow-xs"
              >
                <option value="INR">INR (₹)</option>
                <option value="USD">USD ($)</option>
                <option value="EUR">EUR (€)</option>
                <option value="GBP">GBP (£)</option>
              </select>
            </div>
          </div>

          <div class="flex items-center gap-2 pt-1">
            <input
              id="showSalary"
              v-model="showSalary"
              type="checkbox"
              class="h-4 w-4 rounded border-border text-primary focus:ring-primary cursor-pointer"
            />
            <label for="showSalary" class="text-xs sm:text-sm text-text-secondary cursor-pointer select-none">
              Display salary range publicly on the job posting
            </label>
          </div>

          <div class="grid grid-cols-1 sm:grid-cols-2 gap-4 pt-3 border-t border-border">
            <div>
              <label class="block text-xs font-semibold text-text-secondary uppercase tracking-wider mb-1.5">
                Application Deadline
              </label>
              <input
                v-model="applicationDeadline"
                type="date"
                class="w-full rounded-lg border border-border bg-page px-3 py-2 text-xs sm:text-sm text-text-strong focus:border-primary focus:ring-1 focus:ring-primary shadow-xs"
              />
            </div>

            <div class="flex items-center pt-6">
              <label class="inline-flex items-center gap-2 text-xs sm:text-sm text-text-strong cursor-pointer select-none">
                <input
                  v-model="acceptApplications"
                  type="checkbox"
                  class="h-4 w-4 rounded border-border text-primary focus:ring-primary cursor-pointer"
                />
                Accepting applications for this position
              </label>
            </div>
          </div>
        </div>

        <!-- Sticky / Action Bar -->
        <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4 p-5 rounded-2xl bg-card border border-border shadow-md">
          <button
            type="button"
            @click="router.push(`/jobs/${jobId}`)"
            class="px-5 py-2.5 text-xs sm:text-sm font-semibold rounded-xl bg-surface-raised border border-border text-text-strong hover:bg-border transition cursor-pointer"
          >
            Cancel
          </button>

          <div class="flex items-center gap-3">
            <button
              v-if="jobStatus === 'draft'"
              type="button"
              @click="saveJob(true)"
              :disabled="saving"
              class="inline-flex items-center justify-center gap-2 px-6 py-2.5 text-xs sm:text-sm font-semibold rounded-xl text-white bg-primary hover:bg-primary-hover disabled:opacity-50 transition shadow-xs cursor-pointer"
            >
              <Loader2 v-if="saving" class="h-4 w-4 animate-spin" />
              <Send v-else class="h-4 w-4" />
              Save & Publish
            </button>

            <button
              type="button"
              @click="saveJob(false)"
              :disabled="saving"
              class="inline-flex items-center justify-center gap-2 px-6 py-2.5 text-xs sm:text-sm font-semibold rounded-xl text-text-strong bg-surface-raised hover:bg-border border border-border disabled:opacity-50 transition shadow-xs cursor-pointer"
            >
              <Loader2 v-if="saving" class="h-4 w-4 animate-spin" />
              <Save v-else class="h-4 w-4" />
              Save Changes
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
