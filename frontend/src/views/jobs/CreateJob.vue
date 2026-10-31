<script setup lang="ts">
import { ref, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { supabase } from '@/lib/supabase'
import { useAuthStore } from '@/stores/auth'
import RichTextEditor from '@/components/ui/RichTextEditor.vue'
import {
  Briefcase,
  Layers,
  Sparkles,
  Gift,
  Code2,
  HelpCircle,
  Eye,
  EyeOff,
  Plus,
  X,
  ArrowLeft,
  CheckCircle2,
  Check,
  DollarSign,
  Send,
  Save,
} from 'lucide-vue-next'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()

const organizationId = route.params.orgId as string

const loading = ref(false)
const errorMsg = ref('')

// Job basic fields
const title = ref('')
const shortDescription = ref('')
const description = ref('')
const responsibilities = ref('')
const requirements = ref('')
const niceToHave = ref('')
const whatWeOffer = ref('')
const benefits = ref('')

// Tech Stacks
const techStacks = ref<string[]>([])
const newTechInput = ref('')

const popularTechStacks = [
  'TypeScript', 'JavaScript', 'Vue.js', 'React', 'Node.js',
  'Python', 'Go', 'Java', 'C# / .NET', 'PostgreSQL',
  'Docker', 'Kubernetes', 'AWS', 'GCP', 'Tailwind CSS',
]

function addTechStack(name?: string) {
  const val = (name || newTechInput.value).trim()
  if (!val) return
  if (!techStacks.value.includes(val)) {
    techStacks.value.push(val)
  }
  newTechInput.value = ''
}

function removeTechStack(index: number) {
  techStacks.value.splice(index, 1)
}

// Classification & location
const jobType = ref('full_time')
const workMode = ref('onsite')
const experienceLevel = ref('')
const department = ref('')
const category = ref('')
const city = ref('')
const countryCode = ref('')

// Compensation
const salaryMin = ref<number | null>(null)
const salaryMax = ref<number | null>(null)
const salaryCurrency = ref('LKR')
const showSalary = ref(false)
const applicationDeadline = ref('')
const acceptApplications = ref(true)

// Hiring Process Selection
const hasCustomHiringProcess = ref(false)
const howToApply = ref('')

// Selection Process Visibility
const isSelectionProcessPublic = ref(true)

// Built-in Selection Processes (max 3)
const processes = ref<
  Array<{
    title: string
    process_type: string
    description: string
    is_mandatory: boolean
    time_limit_minutes: number | null
  }>
>([])

const canAddProcess = computed(() => processes.value.length < 3)

function addProcess() {
  if (processes.value.length >= 3) return
  processes.value.push({
    title: '',
    process_type: 'quiz',
    description: '',
    is_mandatory: true,
    time_limit_minutes: null,
  })
}

function removeProcess(index: number) {
  processes.value.splice(index, 1)
}

async function handleSubmit(publish = false) {
  if (!auth.user) return
  if (!title.value.trim()) {
    errorMsg.value = 'Job title is required'
    return
  }

  loading.value = true
  errorMsg.value = ''

  try {
    // 1. Create Job
    const { data: job, error: jobError } = await supabase
      .from('jobs')
      .insert({
        organization_id: organizationId,
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
        status: publish ? 'published' : 'draft',
        published_at: publish ? new Date().toISOString() : null,
        created_by: auth.user.id,
        updated_by: auth.user.id,
        slug: title.value
          .toLowerCase()
          .replace(/[^a-z0-9]+/g, '-')
          .replace(/(^-|-$)/g, ''),
      })
      .select()
      .single()

    if (jobError) throw jobError

    // 2. Create Selection Processes if standard pathway is used
    if (!hasCustomHiringProcess.value && processes.value.length > 0) {
      const processRows = processes.value.map((p, index) => ({
        job_id: job.id,
        title: p.title.trim() || `Stage ${index + 1}`,
        process_type: p.process_type,
        description: p.description.trim() || null,
        order_index: index + 1,
        is_mandatory: p.is_mandatory,
        time_limit_minutes: p.time_limit_minutes,
      }))

      const { error: procError } = await supabase
        .from('job_selection_processes')
        .insert(processRows)

      if (procError) throw procError
    }

    // Success
    router.push(`/organizations/${organizationId}?tab=jobs`)
  } catch (err: any) {
    console.error(err)
    errorMsg.value = err.message || 'Failed to create job'
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="min-h-screen bg-page py-6 sm:py-10 px-3 sm:px-6 lg:px-8">
    <div class="max-w-4xl mx-auto space-y-6 sm:space-y-8">
      
      <!-- HEADER SECTION -->
      <div class="mb-4 sm:mb-8">
        <button
          type="button"
          @click="router.back()"
          class="inline-flex items-center gap-1.5 text-xs sm:text-sm font-medium text-text-muted hover:text-text-strong transition-colors mb-3 focus:outline-none focus:ring-2 focus:ring-primary/20 rounded-md"
        >
          <ArrowLeft class="h-4 w-4" /> Back to organization
        </button>
        <h1 class="text-2xl sm:text-3xl lg:text-4xl font-bold tracking-tight text-text-strong">
          Create Job Posting
        </h1>
        <p class="text-xs sm:text-sm lg:text-base text-text-secondary mt-1.5 leading-relaxed">
          Specify role requirements, rich descriptions, compensation, and your preferred hiring pathway.
        </p>
      </div>

      <form @submit.prevent class="space-y-6 sm:space-y-8">
        <!-- ERROR MESSAGE ALERT -->
        <div 
          v-if="errorMsg" 
          role="alert" 
          class="alert alert-error p-3.5 sm:p-4 text-xs sm:text-sm rounded-xl flex items-center gap-2 border border-danger/30 bg-danger/10 text-danger"
        >
          <span class="font-medium">{{ errorMsg }}</span>
        </div>

        <!-- 1. BASIC INFORMATION -->
        <section class="bg-card shadow-sm border border-border rounded-xl sm:rounded-2xl p-4 sm:p-6 lg:p-8 space-y-5 sm:space-y-6">
          <div class="border-b border-border pb-4">
            <h2 class="text-base sm:text-lg lg:text-xl font-semibold text-text-strong flex items-center gap-2">
              <Briefcase class="h-5 w-5 text-primary flex-shrink-0" /> Basic Information
            </h2>
            <p class="text-xs sm:text-sm text-text-muted mt-1">
              Title and role overview visible on job cards and search listings.
            </p>
          </div>

          <div class="space-y-4 sm:space-y-5">
            <!-- Job Title -->
            <div>
              <label class="block text-xs sm:text-sm font-medium text-text-secondary mb-1.5">
                Job Title <span class="text-danger">*</span>
              </label>
              <input
                v-model="title"
                required
                type="text"
                class="block w-full rounded-lg sm:rounded-xl border border-border bg-card text-text-strong px-3.5 py-2.5 text-xs sm:text-sm shadow-sm placeholder:text-text-subtle focus:border-primary focus:ring-2 focus:ring-primary/20 transition"
                placeholder="e.g. Senior Full Stack Engineer"
              />
            </div>

            <!-- Short Description -->
            <div>
              <label class="block text-xs sm:text-sm font-medium text-text-secondary mb-1.5">
                Short Description
              </label>
              <input
                v-model="shortDescription"
                type="text"
                class="block w-full rounded-lg sm:rounded-xl border border-border bg-card text-text-strong px-3.5 py-2.5 text-xs sm:text-sm shadow-sm placeholder:text-text-subtle focus:border-primary focus:ring-2 focus:ring-primary/20 transition"
                placeholder="One-line summary for job cards and search previews"
              />
            </div>

            <!-- Rich Text: Description -->
            <div>
              <label class="block text-xs sm:text-sm font-medium text-text-secondary mb-1">
                Full Description (About the Role)
              </label>
              <p class="text-[11px] sm:text-xs text-text-muted mb-2">
                Use rich text formatting to clearly present your company culture and role summary.
              </p>
              <RichTextEditor
                v-model="description"
                placeholder="Describe the opportunity, key missions, team context..."
                min-height="160px"
              />
            </div>

            <!-- Rich Text: Responsibilities & Requirements -->
            <div class="grid grid-cols-1 gap-5 sm:gap-6">
              <div>
                <label class="block text-xs sm:text-sm font-medium text-text-secondary mb-1">
                  Responsibilities
                </label>
                <p class="text-[11px] sm:text-xs text-text-muted mb-2">
                  Bullet lists work best for day-to-day duties.
                </p>
                <RichTextEditor
                  v-model="responsibilities"
                  placeholder="• Architect and ship scalable features&#10;• Collaborate with cross-functional teams..."
                  min-height="140px"
                />
              </div>

              <div>
                <label class="block text-xs sm:text-sm font-medium text-text-secondary mb-1">
                  Requirements (Must-haves)
                </label>
                <p class="text-[11px] sm:text-xs text-text-muted mb-2">
                  Essential skills, experience, and qualifications needed.
                </p>
                <RichTextEditor
                  v-model="requirements"
                  placeholder="• 4+ years of full-stack engineering experience&#10;• Strong proficiency in TypeScript..."
                  min-height="140px"
                />
              </div>
            </div>

            <!-- Rich Text: Nice to Have -->
            <div>
              <div class="flex items-center justify-between mb-1">
                <label class="text-xs sm:text-sm font-medium text-text-secondary flex items-center gap-1.5">
                  <Check class="h-4 w-4 text-text-secondary" /> Nice to have (Bonus skills)
                </label>
                <span class="text-[11px] sm:text-xs text-text-subtle">Optional</span>
              </div>
              <p class="text-[11px] sm:text-xs text-text-muted mb-2">
                Additional skills or domain knowledge that give candidates an extra edge.
              </p>
              <RichTextEditor
                v-model="niceToHave"
                placeholder="• Experience with distributed systems&#10;• Familiarity with Supabase or GraphQL..."
                min-height="120px"
              />
            </div>

            <!-- Rich Text: What We Offer -->
            <div>
              <div class="flex items-center justify-between mb-1">
                <label class="text-xs sm:text-sm font-medium text-text-secondary flex items-center gap-1.5">
                  <Gift class="h-4 w-4 text-text-secondary" /> What we offer (Benefits & Perks)
                </label>
                <span class="text-[11px] sm:text-xs text-text-subtle">Optional</span>
              </div>
              <p class="text-[11px] sm:text-xs text-text-muted mb-2">
                Health coverage, remote stipend, flexible hours, paid time off, learning budgets...
              </p>
              <RichTextEditor
                v-model="whatWeOffer"
                placeholder="• Competitive compensation package&#10;• Comprehensive health & dental insurance&#10;• Flexible vacation policy..."
                min-height="120px"
              />
            </div>
          </div>
        </section>

        <!-- 2. TECH STACKS -->
        <section class="bg-card shadow-sm border border-border rounded-xl sm:rounded-2xl p-4 sm:p-6 lg:p-8 space-y-5">
          <div class="border-b border-border pb-4">
            <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-1 sm:gap-4">
              <h2 class="text-base sm:text-lg lg:text-xl font-semibold text-text-strong flex items-center gap-2">
                <Code2 class="h-5 w-5 text-primary flex-shrink-0" /> Tech Stacks
              </h2>
              <span class="text-[11px] sm:text-xs text-text-subtle">Optional (for technical roles)</span>
            </div>
            <p class="text-xs sm:text-sm text-text-muted mt-1">
              Highlight the key technologies, languages, and tools relevant to this position.
            </p>
          </div>

          <div class="space-y-4">
            <!-- Add Tech Input Form -->
            <div class="flex flex-col sm:flex-row gap-2">
              <input
                v-model="newTechInput"
                type="text"
                placeholder="e.g. React, PostgreSQL, Docker"
                class="block flex-1 rounded-lg sm:rounded-xl border border-border bg-card text-text-strong px-3.5 py-2.5 text-xs sm:text-sm shadow-sm placeholder:text-text-subtle focus:border-primary focus:ring-2 focus:ring-primary/20 transition"
                @keydown.enter.prevent="addTechStack()"
              />
              <button
                type="button"
                @click="addTechStack()"
                class="inline-flex items-center justify-center gap-1.5 px-4 py-2.5 text-xs sm:text-sm font-semibold rounded-lg sm:rounded-xl text-white bg-primary hover:bg-primary-hover active:scale-[0.98] transition shadow-sm"
              >
                <Plus class="h-4 w-4" /> Add Stack
              </button>
            </div>

            <!-- Quick Add Chips -->
            <div>
              <span class="text-[11px] sm:text-xs font-medium text-text-secondary block mb-2">Popular presets:</span>
              <div class="flex flex-wrap gap-1.5">
                <button
                  v-for="preset in popularTechStacks"
                  :key="preset"
                  type="button"
                  @click="addTechStack(preset)"
                  class="px-2.5 py-1 bg-surface-raised border border-border text-text-secondary hover:bg-primary-light hover:text-primary hover:border-primary/30 rounded-md sm:rounded-lg text-[11px] sm:text-xs font-medium transition"
                >
                  + {{ preset }}
                </button>
              </div>
            </div>

            <!-- Active Selected Tech Badges -->
            <div v-if="techStacks.length > 0" class="pt-3 border-t border-border">
              <span class="text-xs font-semibold text-text-secondary block mb-2.5">Selected Technologies:</span>
              <div class="flex flex-wrap gap-2">
                <span
                  v-for="(tech, idx) in techStacks"
                  :key="tech"
                  class="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-medium bg-primary-light text-primary border border-primary/30 shadow-xs"
                >
                  <Code2 class="h-3.5 w-3.5" />
                  {{ tech }}
                  <button
                    type="button"
                    @click="removeTechStack(idx)"
                    class="hover:text-danger focus:outline-none transition-colors p-0.5 rounded-full"
                    title="Remove item"
                  >
                    <X class="h-3.5 w-3.5" />
                  </button>
                </span>
              </div>
            </div>
          </div>
        </section>

        <!-- 3. CLASSIFICATION & LOCATION -->
        <section class="bg-card shadow-sm border border-border rounded-xl sm:rounded-2xl p-4 sm:p-6 lg:p-8 space-y-5">
          <div class="border-b border-border pb-4">
            <h2 class="text-base sm:text-lg lg:text-xl font-semibold text-text-strong flex items-center gap-2">
              <Layers class="h-5 w-5 text-primary flex-shrink-0" /> Classification & Location
            </h2>
            <p class="text-xs sm:text-sm text-text-muted mt-1">
              Categorize the role so applicants can search and filter efficiently.
            </p>
          </div>

          <div class="space-y-5">
            <!-- Row 1: Type, Mode, Level -->
            <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4 sm:gap-5">
              <div>
                <label class="block text-xs sm:text-sm font-medium text-text-secondary mb-1.5">Job Type</label>
                <select
                  v-model="jobType"
                  class="block w-full rounded-lg sm:rounded-xl border border-border bg-card text-text-strong px-3 py-2.5 text-xs sm:text-sm shadow-sm focus:border-primary focus:ring-2 focus:ring-primary/20 transition"
                >
                  <option value="full_time">Full-time</option>
                  <option value="part_time">Part-time</option>
                  <option value="contract">Contract</option>
                  <option value="internship">Internship</option>
                  <option value="freelance">Freelance</option>
                  <option value="temporary">Temporary</option>
                </select>
              </div>

              <div>
                <label class="block text-xs sm:text-sm font-medium text-text-secondary mb-1.5">Work Mode</label>
                <select
                  v-model="workMode"
                  class="block w-full rounded-lg sm:rounded-xl border border-border bg-card text-text-strong px-3 py-2.5 text-xs sm:text-sm shadow-sm focus:border-primary focus:ring-2 focus:ring-primary/20 transition"
                >
                  <option value="onsite">On-site</option>
                  <option value="remote">Remote</option>
                  <option value="hybrid">Hybrid</option>
                </select>
              </div>

              <div class="sm:col-span-2 lg:col-span-1">
                <label class="block text-xs sm:text-sm font-medium text-text-secondary mb-1.5">Experience Level</label>
                <select
                  v-model="experienceLevel"
                  class="block w-full rounded-lg sm:rounded-xl border border-border bg-card text-text-strong px-3 py-2.5 text-xs sm:text-sm shadow-sm focus:border-primary focus:ring-2 focus:ring-primary/20 transition"
                >
                  <option value="">Select Level</option>
                  <option value="entry">Entry</option>
                  <option value="junior">Junior</option>
                  <option value="mid">Mid-level</option>
                  <option value="senior">Senior</option>
                  <option value="lead">Lead</option>
                  <option value="executive">Executive</option>
                </select>
              </div>
            </div>

            <!-- Row 2: Department & Category -->
            <div class="grid grid-cols-1 sm:grid-cols-2 gap-4 sm:gap-5">
              <div>
                <label class="block text-xs sm:text-sm font-medium text-text-secondary mb-1.5">Department</label>
                <input
                  v-model="department"
                  type="text"
                  placeholder="e.g. Engineering, Product"
                  class="block w-full rounded-lg sm:rounded-xl border border-border bg-card text-text-strong px-3.5 py-2.5 text-xs sm:text-sm shadow-sm placeholder:text-text-subtle focus:border-primary focus:ring-2 focus:ring-primary/20 transition"
                />
              </div>

              <div>
                <label class="block text-xs sm:text-sm font-medium text-text-secondary mb-1.5">Category</label>
                <input
                  v-model="category"
                  type="text"
                  placeholder="Software, Design, Marketing..."
                  class="block w-full rounded-lg sm:rounded-xl border border-border bg-card text-text-strong px-3.5 py-2.5 text-xs sm:text-sm shadow-sm placeholder:text-text-subtle focus:border-primary focus:ring-2 focus:ring-primary/20 transition"
                />
              </div>
            </div>

            <!-- Row 3: City & Country Code -->
            <div class="grid grid-cols-1 sm:grid-cols-2 gap-4 sm:gap-5">
              <div>
                <label class="block text-xs sm:text-sm font-medium text-text-secondary mb-1.5">City</label>
                <input
                  v-model="city"
                  type="text"
                  placeholder="e.g. San Francisco or Colombo"
                  class="block w-full rounded-lg sm:rounded-xl border border-border bg-card text-text-strong px-3.5 py-2.5 text-xs sm:text-sm shadow-sm placeholder:text-text-subtle focus:border-primary focus:ring-2 focus:ring-primary/20 transition"
                />
              </div>

              <div>
                <label class="block text-xs sm:text-sm font-medium text-text-secondary mb-1.5">Country Code</label>
                <input
                  v-model="countryCode"
                  type="text"
                  maxlength="2"
                  placeholder="US, LK, GB"
                  class="block w-full rounded-lg sm:rounded-xl border border-border bg-card text-text-strong px-3.5 py-2.5 text-xs sm:text-sm uppercase shadow-sm placeholder:text-text-subtle focus:border-primary focus:ring-2 focus:ring-primary/20 transition"
                />
              </div>
            </div>
          </div>
        </section>

        <!-- 4. COMPENSATION -->
        <section class="bg-card shadow-sm border border-border rounded-xl sm:rounded-2xl p-4 sm:p-6 lg:p-8 space-y-5">
          <div class="border-b border-border pb-4">
            <h2 class="text-base sm:text-lg lg:text-xl font-semibold text-text-strong flex items-center gap-2">
              <DollarSign class="h-5 w-5 text-primary flex-shrink-0" /> Compensation & Pay Range
            </h2>
            <p class="text-xs sm:text-sm text-text-muted mt-1">
              Provide salary details to improve applicant engagement and transparency.
            </p>
          </div>

          <div class="grid grid-cols-1 sm:grid-cols-3 gap-4 sm:gap-5">
            <div>
              <label class="block text-xs sm:text-sm font-medium text-text-secondary mb-1.5">Min Salary</label>
              <input
                v-model.number="salaryMin"
                type="number"
                placeholder="e.g. 80000"
                class="block w-full rounded-lg sm:rounded-xl border border-border bg-card text-text-strong px-3.5 py-2.5 text-xs sm:text-sm shadow-sm placeholder:text-text-subtle focus:border-primary focus:ring-2 focus:ring-primary/20 transition"
              />
            </div>

            <div>
              <label class="block text-xs sm:text-sm font-medium text-text-secondary mb-1.5">Max Salary</label>
              <input
                v-model.number="salaryMax"
                type="number"
                placeholder="e.g. 120000"
                class="block w-full rounded-lg sm:rounded-xl border border-border bg-card text-text-strong px-3.5 py-2.5 text-xs sm:text-sm shadow-sm placeholder:text-text-subtle focus:border-primary focus:ring-2 focus:ring-primary/20 transition"
              />
            </div>

            <div>
              <label class="block text-xs sm:text-sm font-medium text-text-secondary mb-1.5">Currency</label>
              <input
                v-model="salaryCurrency"
                type="text"
                maxlength="3"
                placeholder="USD / LKR"
                class="block w-full rounded-lg sm:rounded-xl border border-border bg-card text-text-strong px-3.5 py-2.5 text-xs sm:text-sm uppercase shadow-sm focus:border-primary focus:ring-2 focus:ring-primary/20 transition"
              />
            </div>
          </div>

          <div class="flex items-center gap-3 pt-2">
            <input
              id="showSalary"
              v-model="showSalary"
              type="checkbox"
              class="h-4.5 w-4.5 sm:h-5 sm:w-5 rounded border-border text-primary focus:ring-2 focus:ring-primary/20 cursor-pointer"
            />
            <label for="showSalary" class="text-xs sm:text-sm font-medium text-text-secondary cursor-pointer select-none">
              Display salary range publicly on the job posting
            </label>
          </div>
        </section>

        <!-- 5. HIRING PROCESS & SELECTION PATHWAY -->
        <section class="bg-card shadow-sm border border-border rounded-xl sm:rounded-2xl p-4 sm:p-6 lg:p-8 space-y-6">
          <div class="border-b border-border pb-4">
            <h2 class="text-base sm:text-lg lg:text-xl font-semibold text-text-strong">
              Hiring Process & Application Pathway
            </h2>
            <p class="text-xs sm:text-sm text-text-muted mt-1">
              Choose how candidates apply: through custom company instructions or our structured stage assessments.
            </p>
          </div>

          <!-- Pathway Choice Card -->
          <div class="p-4 sm:p-5 rounded-xl border border-border bg-surface-raised space-y-3">
            <div class="flex items-start gap-3">
              <input
                id="customHiringCheck"
                v-model="hasCustomHiringProcess"
                type="checkbox"
                class="mt-1 h-4.5 w-4.5 sm:h-5 sm:w-5 rounded border-border text-primary focus:ring-2 focus:ring-primary/20 cursor-pointer"
              />
              <label for="customHiringCheck" class="cursor-pointer select-none">
                <span class="block text-xs sm:text-sm font-semibold text-text-strong">
                  Perform hiring process on our own (Custom instructions)
                </span>
                <span class="block text-[11px] sm:text-xs text-text-muted mt-1 leading-relaxed">
                  Check this if you want candidates to apply externally (e.g., through your careers portal or email) rather than using platform assessment stages.
                </span>
              </label>
            </div>
          </div>

          <!-- OPTION A: Custom Hiring Instructions -->
          <div v-if="hasCustomHiringProcess" class="space-y-3 pl-3 sm:pl-4 border-l-2 border-primary ml-1">
            <label class="block text-xs sm:text-sm font-semibold text-text-strong">
              How to Apply (Instructions for Candidates) <span class="text-danger">*</span>
            </label>
            <p class="text-[11px] sm:text-xs text-text-muted mb-2">
              Explain clearly what candidates should do (e.g. submit resumes to careers@company.com, link to external application portal, required attachments, etc.).
            </p>
            <RichTextEditor
              v-model="howToApply"
              placeholder="To apply for this role, please email your CV and portfolio to careers@yourcompany.com with the subject line 'Application: [Job Title]'..."
              min-height="140px"
            />
          </div>

          <!-- OPTION B: Platform Selection Stages -->
          <div v-else class="space-y-6">
            <!-- Stage Visibility Checkbox -->
            <div class="p-4 rounded-xl border border-primary/30 bg-primary-light flex items-start gap-3">
              <input
                id="visibilityCheck"
                v-model="isSelectionProcessPublic"
                type="checkbox"
                class="mt-0.5 h-4.5 w-4.5 sm:h-5 sm:w-5 rounded border-border text-primary focus:ring-2 focus:ring-primary/20 cursor-pointer"
              />
              <label for="visibilityCheck" class="cursor-pointer select-none">
                <span class="text-xs sm:text-sm font-semibold text-text-strong flex items-center gap-1.5">
                  <component :is="isSelectionProcessPublic ? Eye : EyeOff" class="h-4 w-4 text-primary flex-shrink-0" />
                  Make selection process stages visible to candidates
                </span>
                <span class="block text-[11px] sm:text-xs text-text-secondary mt-1 leading-relaxed">
                  When unchecked, selection stages remain internal and are only visible to organization administrators and staff members.
                </span>
              </label>
            </div>

            <!-- Stages Builder (Max 3) -->
            <div class="space-y-4">
              <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-2">
                <div>
                  <h3 class="text-xs sm:text-sm font-semibold text-text-strong">
                    Platform Assessment Stages (Max 3)
                  </h3>
                  <p class="text-[11px] sm:text-xs text-text-muted">Configure progressive evaluation steps for applicants.</p>
                </div>
                <button
                  type="button"
                  :disabled="!canAddProcess"
                  @click="addProcess"
                  class="inline-flex items-center justify-center gap-1.5 px-3.5 py-2 text-xs font-semibold rounded-lg text-white bg-primary hover:bg-primary-hover disabled:opacity-50 transition shadow-xs self-start sm:self-auto"
                >
                  <Plus class="h-4 w-4" /> Add Stage
                </button>
              </div>

              <div
                v-if="processes.length === 0"
                class="text-xs text-text-muted py-8 text-center border-2 border-dashed border-border rounded-xl px-4"
              >
                No stages added. Candidates will submit a direct application without preliminary tests.
              </div>

              <div
                v-for="(process, index) in processes"
                :key="index"
                class="border border-border rounded-xl p-4 sm:p-5 space-y-4 bg-surface-raised relative shadow-xs"
              >
                <div class="flex items-center justify-between border-b border-border pb-2.5">
                  <span class="text-xs font-bold uppercase tracking-wider text-primary flex items-center gap-1.5">
                    Stage {{ index + 1 }}
                  </span>
                  <button
                    type="button"
                    @click="removeProcess(index)"
                    class="text-text-subtle hover:text-danger p-1 rounded-lg transition-colors"
                    title="Remove stage"
                  >
                    <X class="h-4.5 w-4.5" />
                  </button>
                </div>

                <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
                  <div>
                    <label class="block text-xs font-medium text-text-secondary mb-1">Stage Title</label>
                    <input
                      v-model="process.title"
                      type="text"
                      placeholder="e.g. Technical Quiz or Code Review"
                      class="block w-full rounded-lg border border-border bg-card text-text-strong px-3 py-2 text-xs sm:text-sm shadow-sm placeholder:text-text-subtle focus:border-primary focus:ring-2 focus:ring-primary/20 transition"
                    />
                  </div>
                  <div>
                    <label class="block text-xs font-medium text-text-secondary mb-1">Assessment Type</label>
                    <select
                      v-model="process.process_type"
                      class="block w-full rounded-lg border border-border bg-card text-text-strong px-3 py-2 text-xs sm:text-sm shadow-sm focus:border-primary focus:ring-2 focus:ring-primary/20 transition"
                    >
                      <option value="quiz">Quiz Assessment</option>
                      <option value="screening">Screening Questionnaire</option>
                      <option value="coding">Coding Challenge</option>
                      <option value="screening_quiz">Screening + Quiz</option>
                      <option value="screening_coding">Screening + Coding</option>
                      <option value="quiz_coding">Quiz + Coding</option>
                      <option value="custom">Custom Evaluation</option>
                    </select>
                  </div>
                </div>

                <div>
                  <label class="block text-xs font-medium text-text-secondary mb-1">Description / Brief</label>
                  <textarea
                    v-model="process.description"
                    rows="2"
                    placeholder="Brief guidance for the applicant..."
                    class="block w-full rounded-lg border border-border bg-card text-text-strong px-3 py-2 text-xs sm:text-sm shadow-sm placeholder:text-text-subtle focus:border-primary focus:ring-2 focus:ring-primary/20 transition"
                  ></textarea>
                </div>

                <div class="flex flex-col sm:flex-row sm:items-center gap-4 pt-1">
                  <div class="flex items-center gap-2">
                    <input
                      :id="`mandatory-${index}`"
                      v-model="process.is_mandatory"
                      type="checkbox"
                      class="h-4 w-4 rounded border-border text-primary focus:ring-2 focus:ring-primary/20 cursor-pointer"
                    />
                    <label :for="`mandatory-${index}`" class="text-xs font-medium text-text-secondary cursor-pointer select-none">
                      Mandatory to advance
                    </label>
                  </div>
                  <div class="flex items-center gap-2">
                    <label class="text-xs font-medium text-text-secondary whitespace-nowrap">Time limit (minutes):</label>
                    <input
                      v-model.number="process.time_limit_minutes"
                      type="number"
                      min="1"
                      placeholder="Unlimited"
                      class="w-28 rounded-lg border border-border bg-card text-text-strong px-2.5 py-1.5 text-xs placeholder:text-text-subtle focus:border-primary focus:ring-2 focus:ring-primary/20 transition"
                    />
                  </div>
                </div>
              </div>
            </div>
          </div>
        </section>

        <!-- SUBMIT ACTIONS -->
        <div class="flex flex-col-reverse sm:flex-row items-stretch sm:items-center justify-end gap-3 pt-4">
          <button
            type="button"
            :disabled="loading"
            @click="handleSubmit(false)"
            class="inline-flex items-center justify-center gap-2 px-6 py-3 text-xs sm:text-sm font-medium rounded-xl border border-border bg-card text-text-secondary hover:bg-surface-raised disabled:opacity-50 transition shadow-sm"
          >
            <Save class="h-4 w-4" /> Save as Draft
          </button>
          <button
            type="button"
            :disabled="loading"
            @click="handleSubmit(true)"
            class="inline-flex items-center justify-center gap-2 px-6 py-3 text-xs sm:text-sm font-semibold rounded-xl text-white bg-primary hover:bg-primary-hover disabled:opacity-50 active:scale-[0.99] transition shadow-sm"
          >
            <Send class="h-4 w-4" /> {{ loading ? 'Publishing...' : 'Publish Job' }}
          </button>
        </div>
      </form>
    </div>
  </div>
</template>
