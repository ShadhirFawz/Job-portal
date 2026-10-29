<script setup lang="ts">
import { ref, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { supabase } from '@/lib/supabase'
import { useAuthStore } from '@/stores/auth'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()

const organizationId = route.params.orgId as string

const loading = ref(false)
const errorMsg = ref('')

// Job fields
const title = ref('')
const shortDescription = ref('')
const description = ref('')
const responsibilities = ref('')
const requirements = ref('')
const benefits = ref('')
const jobType = ref('full_time')
const workMode = ref('onsite')
const experienceLevel = ref('')
const department = ref('')
const category = ref('')
const city = ref('')
const countryCode = ref('')
const salaryMin = ref<number | null>(null)
const salaryMax = ref<number | null>(null)
const salaryCurrency = ref('INR')
const showSalary = ref(false)
const applicationDeadline = ref('')
const acceptApplications = ref(true)

// Selection Processes (max 3)
const processes = ref<Array<{
  title: string
  process_type: string
  description: string
  is_mandatory: boolean
  time_limit_minutes: number | null
}>>([])

const canAddProcess = computed(() => processes.value.length < 3)

function addProcess() {
  if (processes.value.length >= 3) return
  processes.value.push({
    title: '',
    process_type: 'quiz',
    description: '',
    is_mandatory: true,
    time_limit_minutes: null
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
        benefits: benefits.value.trim() || null,
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
        slug: title.value.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/(^-|-$)/g, '')
      })
      .select()
      .single()

    if (jobError) throw jobError

    // 2. Create Selection Processes
    if (processes.value.length > 0) {
      const processRows = processes.value.map((p, index) => ({
        job_id: job.id,
        title: p.title.trim() || `Process ${index + 1}`,
        process_type: p.process_type,
        description: p.description.trim() || null,
        order_index: index + 1,
        is_mandatory: p.is_mandatory,
        time_limit_minutes: p.time_limit_minutes
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
  <div class="min-h-screen bg-gray-50 py-10 px-4">
    <div class="max-w-4xl mx-auto">
      <div class="mb-8">
        <button @click="router.back()" class="text-sm text-indigo-600 hover:underline">← Back</button>
        <h1 class="text-3xl font-bold text-gray-900 mt-2">Create Job Posting</h1>
        <p class="text-gray-600 mt-1">Fill in the details and optionally add up to 3 selection processes.</p>
      </div>

      <form @submit.prevent class="space-y-8">
        <div v-if="errorMsg" class="rounded-md bg-red-50 p-4 text-sm text-red-700">
          {{ errorMsg }}
        </div>

        <!-- Basic Info -->
        <div class="bg-white shadow rounded-lg p-6 space-y-5">
          <h2 class="text-lg font-semibold text-gray-900">Basic Information</h2>

          <div>
            <label class="block text-sm font-medium text-gray-700">Job Title *</label>
            <input v-model="title" required type="text"
              class="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:ring-indigo-500 sm:text-sm"
              placeholder="e.g. Senior Full Stack Developer" />
          </div>

          <div>
            <label class="block text-sm font-medium text-gray-700">Short Description</label>
            <input v-model="shortDescription" type="text"
              class="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:ring-indigo-500 sm:text-sm"
              placeholder="One-line summary for job cards" />
          </div>

          <div>
            <label class="block text-sm font-medium text-gray-700">Full Description</label>
            <textarea v-model="description" rows="5"
              class="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:ring-indigo-500 sm:text-sm"></textarea>
          </div>

          <div class="grid grid-cols-1 sm:grid-cols-2 gap-5">
            <div>
              <label class="block text-sm font-medium text-gray-700">Responsibilities</label>
              <textarea v-model="responsibilities" rows="4"
                class="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:ring-indigo-500 sm:text-sm"></textarea>
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700">Requirements</label>
              <textarea v-model="requirements" rows="4"
                class="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:ring-indigo-500 sm:text-sm"></textarea>
            </div>
          </div>

          <div>
            <label class="block text-sm font-medium text-gray-700">Benefits</label>
            <textarea v-model="benefits" rows="3"
              class="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:ring-indigo-500 sm:text-sm"></textarea>
          </div>
        </div>

        <!-- Classification -->
        <div class="bg-white shadow rounded-lg p-6 space-y-5">
          <h2 class="text-lg font-semibold text-gray-900">Classification & Location</h2>

          <div class="grid grid-cols-1 sm:grid-cols-3 gap-5">
            <div>
              <label class="block text-sm font-medium text-gray-700">Job Type</label>
              <select v-model="jobType" class="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 sm:text-sm">
                <option value="full_time">Full-time</option>
                <option value="part_time">Part-time</option>
                <option value="contract">Contract</option>
                <option value="internship">Internship</option>
                <option value="freelance">Freelance</option>
                <option value="temporary">Temporary</option>
              </select>
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700">Work Mode</label>
              <select v-model="workMode" class="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 sm:text-sm">
                <option value="onsite">On-site</option>
                <option value="remote">Remote</option>
                <option value="hybrid">Hybrid</option>
              </select>
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700">Experience Level</label>
              <select v-model="experienceLevel" class="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 sm:text-sm">
                <option value="">Select</option>
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
              <label class="block text-sm font-medium text-gray-700">Department</label>
              <input v-model="department" type="text" class="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 sm:text-sm" />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700">Category</label>
              <input v-model="category" type="text" placeholder="Engineering, Design, Sales..." class="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 sm:text-sm" />
            </div>
          </div>

          <div class="grid grid-cols-1 sm:grid-cols-2 gap-5">
            <div>
              <label class="block text-sm font-medium text-gray-700">City</label>
              <input v-model="city" type="text" class="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 sm:text-sm" />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700">Country Code</label>
              <input v-model="countryCode" type="text" maxlength="2" placeholder="IN" class="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 sm:text-sm" />
            </div>
          </div>
        </div>

        <!-- Compensation -->
        <div class="bg-white shadow rounded-lg p-6 space-y-5">
          <h2 class="text-lg font-semibold text-gray-900">Compensation</h2>
          <div class="grid grid-cols-1 sm:grid-cols-3 gap-5">
            <div>
              <label class="block text-sm font-medium text-gray-700">Min Salary</label>
              <input v-model.number="salaryMin" type="number" class="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 sm:text-sm" />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700">Max Salary</label>
              <input v-model.number="salaryMax" type="number" class="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 sm:text-sm" />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700">Currency</label>
              <input v-model="salaryCurrency" type="text" maxlength="3" class="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 sm:text-sm" />
            </div>
          </div>
          <div class="flex items-center gap-2">
            <input id="showSalary" v-model="showSalary" type="checkbox" class="rounded border-gray-300" />
            <label for="showSalary" class="text-sm text-gray-700">Show salary on job posting</label>
          </div>
        </div>

        <!-- Selection Processes -->
        <div class="bg-white shadow rounded-lg p-6 space-y-5">
          <div class="flex items-center justify-between">
            <div>
              <h2 class="text-lg font-semibold text-gray-900">Selection Processes</h2>
              <p class="text-sm text-gray-500">Add up to 3 stages (Quiz, Screening, Coding, combinations...)</p>
            </div>
            <button
              type="button"
              :disabled="!canAddProcess"
              @click="addProcess"
              class="px-3 py-1.5 text-sm font-medium rounded-md text-white bg-indigo-600 hover:bg-indigo-700 disabled:opacity-50"
            >
              + Add Process
            </button>
          </div>

          <div v-if="processes.length === 0" class="text-sm text-gray-500 py-4 text-center border border-dashed rounded-lg">
            No selection processes added. Candidates will only submit an application.
          </div>

          <div v-for="(process, index) in processes" :key="index" class="border rounded-lg p-4 space-y-4 relative">
            <button
              type="button"
              @click="removeProcess(index)"
              class="absolute top-3 right-3 text-gray-400 hover:text-red-500"
            >×</button>

            <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <div>
                <label class="block text-sm font-medium text-gray-700">Process Title</label>
                <input v-model="process.title" type="text" placeholder="e.g. Aptitude Test"
                  class="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 sm:text-sm" />
              </div>
              <div>
                <label class="block text-sm font-medium text-gray-700">Type</label>
                <select v-model="process.process_type" class="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 sm:text-sm">
                  <option value="quiz">Quiz only</option>
                  <option value="screening">Screening only</option>
                  <option value="coding">Coding only</option>
                  <option value="screening_quiz">Screening + Quiz</option>
                  <option value="screening_coding">Screening + Coding</option>
                  <option value="quiz_coding">Quiz + Coding</option>
                  <option value="custom">Custom</option>
                </select>
              </div>
            </div>

            <div>
              <label class="block text-sm font-medium text-gray-700">Description</label>
              <textarea v-model="process.description" rows="2"
                class="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 sm:text-sm"></textarea>
            </div>

            <div class="flex flex-wrap items-center gap-6">
              <div class="flex items-center gap-2">
                <input :id="`mandatory-${index}`" v-model="process.is_mandatory" type="checkbox" class="rounded border-gray-300" />
                <label :for="`mandatory-${index}`" class="text-sm text-gray-700">Mandatory</label>
              </div>
              <div class="flex items-center gap-2">
                <label class="text-sm text-gray-700">Time limit (minutes)</label>
                <input v-model.number="process.time_limit_minutes" type="number" min="1" placeholder="No limit"
                  class="w-24 rounded-md border border-gray-300 px-2 py-1 text-sm" />
              </div>
            </div>
          </div>
        </div>

        <!-- Actions -->
        <div class="flex flex-col sm:flex-row gap-3 justify-end">
          <button
            type="button"
            :disabled="loading"
            @click="handleSubmit(false)"
            class="px-6 py-2.5 text-sm font-medium rounded-md border border-gray-300 bg-white text-gray-700 hover:bg-gray-50 disabled:opacity-50"
          >
            Save as Draft
          </button>
          <button
            type="button"
            :disabled="loading"
            @click="handleSubmit(true)"
            class="px-6 py-2.5 text-sm font-medium rounded-md text-white bg-indigo-600 hover:bg-indigo-700 disabled:opacity-50"
          >
            {{ loading ? 'Publishing...' : 'Publish Job' }}
          </button>
        </div>
      </form>
    </div>
  </div>
</template>