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
  <div class="min-h-screen bg-gray-50/70 py-10 px-4 sm:px-6">
    <div class="max-w-4xl mx-auto">
      <div class="mb-8">
        <button
          type="button"
          @click="router.back()"
          class="inline-flex items-center gap-1.5 text-sm font-medium text-gray-600 hover:text-gray-900 transition mb-2"
        >
          <ArrowLeft class="h-4 w-4" /> Back to organization
        </button>
        <h1 class="text-3xl font-bold text-gray-900">Create Job Posting</h1>
        <p class="text-gray-600 mt-1">
          Specify role requirements, rich descriptions, compensation, and your preferred hiring pathway.
        </p>
      </div>

      <form @submit.prevent class="space-y-8">
        <div v-if="errorMsg" class="rounded-xl bg-red-50 p-4 border border-red-200 text-sm text-red-700">
          {{ errorMsg }}
        </div>

        <!-- 1. BASIC INFORMATION -->
        <div class="bg-white shadow-sm border border-gray-200 rounded-2xl p-6 space-y-6">
          <div class="border-b border-gray-100 pb-3">
            <h2 class="text-lg font-semibold text-gray-900 flex items-center gap-2">
              <Briefcase class="h-5 w-5 text-indigo-600" /> Basic Information
            </h2>
            <p class="text-xs text-gray-500 mt-0.5">Title and role overview visible on listing cards.</p>
          </div>

          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Job Title *</label>
            <input
              v-model="title"
              required
              type="text"
              class="block w-full rounded-lg border border-gray-300 px-3.5 py-2 text-sm shadow-xs focus:ring-1 focus:ring-indigo-500"
              placeholder="e.g. Senior Full Stack Engineer"
            />
          </div>

          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Short Description</label>
            <input
              v-model="shortDescription"
              type="text"
              class="block w-full rounded-lg border border-gray-300 px-3.5 py-2 text-sm shadow-xs focus:ring-1 focus:ring-indigo-500"
              placeholder="One-line summary for job cards and search previews"
            />
          </div>

          <!-- Rich Text: Description -->
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-1">Full Description (About the Role)</label>
            <p class="text-xs text-gray-500 mb-2">Use formatting to clearly present your company culture and role summary.</p>
            <RichTextEditor
              v-model="description"
              placeholder="Describe the opportunity, key missions, team context..."
              min-height="160px"
            />
          </div>

          <!-- Rich Text: Responsibilities & Requirements -->
          <div class="grid grid-cols-1 gap-6">
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">Responsibilities</label>
              <p class="text-xs text-gray-500 mb-2">Bullet lists work great for day-to-day duties.</p>
              <RichTextEditor
                v-model="responsibilities"
                placeholder="• Architect and ship scalable features&#10;• Collaborate with cross-functional teams..."
                min-height="130px"
              />
            </div>

            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">Requirements (Must-haves)</label>
              <p class="text-xs text-gray-500 mb-2">Essential skills, experience, and background needed.</p>
              <RichTextEditor
                v-model="requirements"
                placeholder="• 4+ years of full-stack engineering experience&#10;• Strong proficiency in TypeScript..."
                min-height="130px"
              />
            </div>
          </div>

          <!-- Rich Text: Nice to Have -->
          <div>
            <div class="flex items-center justify-between mb-1">
              <label class="text-sm font-medium text-gray-700 flex items-center gap-1.5">
                <Check class="h-4 w-4 text-gray-700" /> Nice to have (Bonus skills)
              </label>
              <span class="text-xs text-gray-400">Optional</span>
            </div>
            <p class="text-xs text-gray-500 mb-2">Additional skills or domain knowledge that gives candidates an edge.</p>
            <RichTextEditor
              v-model="niceToHave"
              placeholder="• Experience with distributed systems&#10;• Familiarity with Supabase or GraphQL..."
              min-height="110px"
            />
          </div>

          <!-- Rich Text: What We Offer -->
          <div>
            <div class="flex items-center justify-between mb-1">
              <label class="text-sm font-medium text-gray-700 flex items-center gap-1.5">
                <Gift class="h-4 w-4 text-gray-700" /> What we offer (Benefits & Perks)
              </label>
              <span class="text-xs text-gray-400">Optional</span>
            </div>
            <p class="text-xs text-gray-500 mb-2">Health coverage, remote equipment stipend, paid learning, bonuses...</p>
            <RichTextEditor
              v-model="whatWeOffer"
              placeholder="• Competitive compensation package&#10;• Comprehensive health & dental insurance&#10;• Flexible vacation policy..."
              min-height="110px"
            />
          </div>
        </div>

        <!-- 2. TECH STACKS YOU'LL WORK WITH -->
        <div class="bg-white shadow-sm border border-gray-200 rounded-2xl p-6 space-y-5">
          <div class="border-b border-gray-100 pb-3">
            <div class="flex items-center justify-between">
              <h2 class="text-lg font-semibold text-gray-900 flex items-center gap-2">
                <Code2 class="h-5 w-5 text-indigo-600" /> Tech Stacks you'll work with
              </h2>
              <span class="text-xs text-gray-400">Optional (for tech-applicable roles)</span>
            </div>
            <p class="text-xs text-gray-500 mt-0.5">
              Highlight the key technologies, libraries, and tools used by this team.
            </p>
          </div>

          <div>
            <div class="flex gap-2">
              <input
                v-model="newTechInput"
                type="text"
                placeholder="Type a technology (e.g. React, PostgreSQL, Docker) and press Enter"
                class="block flex-1 rounded-lg border border-gray-300 px-3.5 py-2 text-sm shadow-xs focus:ring-1 focus:ring-indigo-500"
                @keydown.enter.prevent="addTechStack()"
              />
              <button
                type="button"
                @click="addTechStack()"
                class="inline-flex items-center gap-1 px-4 py-2 text-sm font-medium rounded-lg text-white bg-indigo-600 hover:bg-indigo-700 transition"
              >
                <Plus class="h-4 w-4" /> Add
              </button>
            </div>

            <!-- Suggested Tech Chips -->
            <div class="mt-3 flex flex-wrap items-center gap-1.5 text-xs text-gray-500">
              <span class="font-medium mr-1 text-gray-600">Quick add:</span>
              <button
                v-for="preset in popularTechStacks.slice(0, 10)"
                :key="preset"
                type="button"
                @click="addTechStack(preset)"
                class="px-2 py-0.5 bg-gray-100 hover:bg-indigo-50 hover:text-indigo-600 rounded-md transition-colors text-[11px]"
              >
                + {{ preset }}
              </button>
            </div>

            <!-- Active Tech Stack Badges -->
            <div v-if="techStacks.length > 0" class="mt-4 pt-3 border-t border-gray-100">
              <span class="text-xs font-semibold text-gray-700 block mb-2">Selected Tech Stacks:</span>
              <div class="flex flex-wrap gap-2">
                <span
                  v-for="(tech, idx) in techStacks"
                  :key="tech"
                  class="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-semibold bg-indigo-50 text-indigo-700 border border-indigo-200 shadow-2xs"
                >
                  <Code2 class="h-3 w-3 text-indigo-500" />
                  {{ tech }}
                  <button
                    type="button"
                    @click="removeTechStack(idx)"
                    class="hover:text-red-500 focus:outline-hidden"
                    title="Remove"
                  >
                    <X class="h-3 w-3" />
                  </button>
                </span>
              </div>
            </div>
          </div>
        </div>

        <!-- 3. CLASSIFICATION & LOCATION -->
        <div class="bg-white shadow-sm border border-gray-200 rounded-2xl p-6 space-y-5">
          <div class="border-b border-gray-100 pb-3">
            <h2 class="text-lg font-semibold text-gray-900 flex items-center gap-2">
              <Layers class="h-5 w-5 text-indigo-600" /> Classification & Location
            </h2>
          </div>

          <div class="grid grid-cols-1 sm:grid-cols-3 gap-5">
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">Job Type</label>
              <select
                v-model="jobType"
                class="block w-full rounded-lg border border-gray-300 px-3 py-2 text-sm shadow-xs focus:ring-1 focus:ring-indigo-500 bg-white"
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
              <label class="block text-sm font-medium text-gray-700 mb-1">Work Mode</label>
              <select
                v-model="workMode"
                class="block w-full rounded-lg border border-gray-300 px-3 py-2 text-sm shadow-xs focus:ring-1 focus:ring-indigo-500 bg-white"
              >
                <option value="onsite">On-site</option>
                <option value="remote">Remote</option>
                <option value="hybrid">Hybrid</option>
              </select>
            </div>

            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">Experience Level</label>
              <select
                v-model="experienceLevel"
                class="block w-full rounded-lg border border-gray-300 px-3 py-2 text-sm shadow-xs focus:ring-1 focus:ring-indigo-500 bg-white"
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

          <div class="grid grid-cols-1 sm:grid-cols-2 gap-5">
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">Department</label>
              <input
                v-model="department"
                type="text"
                placeholder="e.g. Engineering, Product"
                class="block w-full rounded-lg border border-gray-300 px-3 py-2 text-sm shadow-xs focus:ring-1 focus:ring-indigo-500"
              />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">Category</label>
              <input
                v-model="category"
                type="text"
                placeholder="Software, Design, Marketing..."
                class="block w-full rounded-lg border border-gray-300 px-3 py-2 text-sm shadow-xs focus:ring-1 focus:ring-indigo-500"
              />
            </div>
          </div>

          <div class="grid grid-cols-1 sm:grid-cols-2 gap-5">
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">City</label>
              <input
                v-model="city"
                type="text"
                placeholder="e.g. San Francisco"
                class="block w-full rounded-lg border border-gray-300 px-3 py-2 text-sm shadow-xs focus:ring-1 focus:ring-indigo-500"
              />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">Country Code</label>
              <input
                v-model="countryCode"
                type="text"
                maxlength="2"
                placeholder="US"
                class="block w-full rounded-lg border border-gray-300 px-3 py-2 text-sm uppercase shadow-xs focus:ring-1 focus:ring-indigo-500"
              />
            </div>
          </div>
        </div>

        <!-- 4. COMPENSATION -->
        <div class="bg-white shadow-sm border border-gray-200 rounded-2xl p-6 space-y-5">
          <h2 class="text-lg font-semibold text-gray-900">Compensation</h2>
          <div class="grid grid-cols-1 sm:grid-cols-3 gap-5">
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">Min Salary</label>
              <input
                v-model.number="salaryMin"
                type="number"
                placeholder="e.g. 80000"
                class="block w-full rounded-lg border border-gray-300 px-3 py-2 text-sm shadow-xs focus:ring-1 focus:ring-indigo-500"
              />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">Max Salary</label>
              <input
                v-model.number="salaryMax"
                type="number"
                placeholder="e.g. 120000"
                class="block w-full rounded-lg border border-gray-300 px-3 py-2 text-sm shadow-xs focus:ring-1 focus:ring-indigo-500"
              />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-1">Currency</label>
              <input
                v-model="salaryCurrency"
                type="text"
                maxlength="3"
                class="block w-full rounded-lg border border-gray-300 px-3 py-2 text-sm uppercase shadow-xs focus:ring-1 focus:ring-indigo-500"
              />
            </div>
          </div>

          <div class="flex items-center gap-2 pt-1">
            <input
              id="showSalary"
              v-model="showSalary"
              type="checkbox"
              class="h-4 w-4 rounded border-gray-300 text-indigo-600 focus:ring-indigo-500"
            />
            <label for="showSalary" class="text-sm font-medium text-gray-700 cursor-pointer">
              Show salary publicly on job posting
            </label>
          </div>
        </div>

        <!-- 5. HIRING PROCESS & SELECTION PATHWAY -->
        <div class="bg-white shadow-sm border border-gray-200 rounded-2xl p-6 space-y-6">
          <div class="border-b border-gray-100 pb-3">
            <h2 class="text-lg font-semibold text-gray-900">Hiring Process & Application Pathway</h2>
            <p class="text-xs text-gray-500 mt-0.5">
              Choose how candidates apply: through custom company instructions or our structured stage assessments.
            </p>
          </div>

          <!-- Pathway Choice Card -->
          <div class="p-4 rounded-xl border border-gray-200 bg-gray-50/70 space-y-3">
            <div class="flex items-start gap-3">
              <input
                id="customHiringCheck"
                v-model="hasCustomHiringProcess"
                type="checkbox"
                class="mt-1 h-4 w-4 rounded border-gray-300 text-indigo-600 focus:ring-indigo-500"
              />
              <label for="customHiringCheck" class="cursor-pointer">
                <span class="block text-sm font-semibold text-gray-900">
                  Perform hiring process on our own (Custom instructions)
                </span>
                <span class="block text-xs text-gray-500 mt-0.5">
                  Check this if you want candidates to apply externally (e.g. through your careers portal or email) rather than using platform assessment stages.
                </span>
              </label>
            </div>
          </div>

          <!-- OPTION A: Custom Hiring Instructions -->
          <div v-if="hasCustomHiringProcess" class="space-y-3 pl-1 border-l-2 border-indigo-400 ml-1">
            <label class="block text-sm font-semibold text-gray-900">
              How to Apply (Instructions for Candidates) *
            </label>
            <p class="text-xs text-gray-500 mb-2">
              Explain clearly what candidates should do (e.g. submit resumes to careers@company.com, link to external application portal, required attachments, etc.).
            </p>
            <RichTextEditor
              v-model="howToApply"
              placeholder="To apply for this role, please email your CV and portfolio to careers@yourcompany.com with the subject line 'Application: [Job Title]'..."
              min-height="130px"
            />
          </div>

          <!-- OPTION B: Platform Selection Stages -->
          <div v-else class="space-y-6">
            <!-- Stage Visibility Checkbox -->
            <div class="p-4 rounded-xl border border-indigo-100 bg-indigo-50/50 flex items-start justify-between gap-4">
              <div class="flex items-start gap-3">
                <input
                  id="visibilityCheck"
                  v-model="isSelectionProcessPublic"
                  type="checkbox"
                  class="mt-1 h-4 w-4 rounded border-gray-300 text-indigo-600 focus:ring-indigo-500"
                />
                <label for="visibilityCheck" class="cursor-pointer">
                  <span class="text-sm font-semibold text-gray-900 flex items-center gap-1.5">
                    <component :is="isSelectionProcessPublic ? Eye : EyeOff" class="h-4 w-4 text-indigo-600" />
                    Make selection process stages visible to candidates
                  </span>
                  <span class="block text-xs text-gray-600 mt-0.5">
                    When unchecked, selection stages remain internal and are only visible to organization administrators and staff members.
                  </span>
                </label>
              </div>
            </div>

            <!-- Stages Builder (Max 3) -->
            <div class="space-y-4">
              <div class="flex items-center justify-between">
                <div>
                  <h3 class="text-sm font-semibold text-gray-900">Platform Assessment Stages (Max 3)</h3>
                  <p class="text-xs text-gray-500">Configure progressive evaluation steps for applicants.</p>
                </div>
                <button
                  type="button"
                  :disabled="!canAddProcess"
                  @click="addProcess"
                  class="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold rounded-lg text-white bg-indigo-600 hover:bg-indigo-700 disabled:opacity-50 transition"
                >
                  <Plus class="h-3.5 w-3.5" /> Add Stage
                </button>
              </div>

              <div
                v-if="processes.length === 0"
                class="text-xs text-gray-500 py-6 text-center border-2 border-dashed border-gray-200 rounded-xl"
              >
                No stages added. Candidates will submit a direct application without preliminary tests.
              </div>

              <div
                v-for="(process, index) in processes"
                :key="index"
                class="border border-gray-200 rounded-xl p-5 space-y-4 bg-gray-50/50 relative shadow-2xs"
              >
                <div class="flex items-center justify-between border-b border-gray-200/60 pb-2">
                  <span class="text-xs font-bold uppercase tracking-wider text-indigo-700 flex items-center gap-1.5">
                    Stage {{ index + 1 }}
                  </span>
                  <button
                    type="button"
                    @click="removeProcess(index)"
                    class="text-gray-400 hover:text-red-500 p-1 rounded-md transition"
                    title="Remove stage"
                  >
                    <X class="h-4 w-4" />
                  </button>
                </div>

                <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
                  <div>
                    <label class="block text-xs font-medium text-gray-700 mb-1">Stage Title</label>
                    <input
                      v-model="process.title"
                      type="text"
                      placeholder="e.g. Technical Quiz or Code Review"
                      class="block w-full rounded-lg border border-gray-300 px-3 py-1.5 text-sm shadow-xs focus:ring-1 focus:ring-indigo-500 bg-white"
                    />
                  </div>
                  <div>
                    <label class="block text-xs font-medium text-gray-700 mb-1">Assessment Type</label>
                    <select
                      v-model="process.process_type"
                      class="block w-full rounded-lg border border-gray-300 px-3 py-1.5 text-sm shadow-xs focus:ring-1 focus:ring-indigo-500 bg-white"
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
                  <label class="block text-xs font-medium text-gray-700 mb-1">Description / Brief</label>
                  <textarea
                    v-model="process.description"
                    rows="2"
                    placeholder="Brief guidance for the applicant..."
                    class="block w-full rounded-lg border border-gray-300 px-3 py-1.5 text-sm shadow-xs focus:ring-1 focus:ring-indigo-500 bg-white"
                  ></textarea>
                </div>

                <div class="flex flex-wrap items-center gap-6 pt-1">
                  <div class="flex items-center gap-2">
                    <input
                      :id="`mandatory-${index}`"
                      v-model="process.is_mandatory"
                      type="checkbox"
                      class="h-4 w-4 rounded border-gray-300 text-indigo-600 focus:ring-indigo-500"
                    />
                    <label :for="`mandatory-${index}`" class="text-xs font-medium text-gray-700 cursor-pointer">
                      Mandatory to advance
                    </label>
                  </div>
                  <div class="flex items-center gap-2">
                    <label class="text-xs font-medium text-gray-700">Time limit (minutes):</label>
                    <input
                      v-model.number="process.time_limit_minutes"
                      type="number"
                      min="1"
                      placeholder="Unlimited"
                      class="w-24 rounded-lg border border-gray-300 px-2 py-1 text-xs bg-white"
                    />
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- ACTIONS -->
        <div class="flex flex-col sm:flex-row gap-3 justify-end pt-4">
          <button
            type="button"
            :disabled="loading"
            @click="handleSubmit(false)"
            class="px-6 py-2.5 text-sm font-medium rounded-lg border border-gray-300 bg-white text-gray-700 hover:bg-gray-50 disabled:opacity-50 transition shadow-xs"
          >
            Save as Draft
          </button>
          <button
            type="button"
            :disabled="loading"
            @click="handleSubmit(true)"
            class="px-6 py-2.5 text-sm font-semibold rounded-lg text-white bg-indigo-600 hover:bg-indigo-700 disabled:opacity-50 transition shadow-xs"
          >
            {{ loading ? 'Publishing...' : 'Publish Job' }}
          </button>
        </div>
      </form>
    </div>
  </div>
</template>
