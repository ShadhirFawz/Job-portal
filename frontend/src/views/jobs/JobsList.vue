<script setup lang="ts">
import { ref, onMounted, watch, computed } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { jobService } from '@/services/job.service'
import type { Job } from '@/types/job'
import { getOrganizationImageUrl } from '@/composables/useOrganizationImage'
import {
  Search,
  MapPin,
  Briefcase,
  Clock,
  SlidersHorizontal,
  X,
  ChevronLeft,
  ChevronRight,
  Building2,
  RotateCcw,
  Banknote,
  Calendar,
} from 'lucide-vue-next'

const router = useRouter()
const route = useRoute()

// Server state
const jobs = ref<Job[]>([])
const loading = ref(true)
const totalCount = ref(0)
const totalPages = ref(1)

// Filters (hydrated from route query)
const searchQuery = ref((route.query.q as string) || '')
const searchInput = ref(searchQuery.value)
const jobType = ref((route.query.type as string) || '')
const workMode = ref((route.query.mode as string) || '')
const experienceLevel = ref((route.query.level as string) || '')
const location = ref((route.query.location as string) || '')
const locationInput = ref(location.value)

// Pagination
const page = ref(Math.max(1, Number(route.query.page) || 1))
const pageSize = 10

// Mobile filter drawer state
const showMobileFilters = ref(false)

onMounted(() => {
  fetchJobs()
})

// Synchronize URL query params
function syncUrl() {
  const query: Record<string, string> = {}
  if (searchQuery.value.trim()) query.q = searchQuery.value.trim()
  if (jobType.value) query.type = jobType.value
  if (workMode.value) query.mode = workMode.value
  if (experienceLevel.value) query.level = experienceLevel.value
  if (location.value.trim()) query.location = location.value.trim()
  if (page.value > 1) query.page = String(page.value)

  router.replace({ query })
}

// Fetch jobs via backend API with server-side filtering and pagination
async function fetchJobs() {
  loading.value = true
  try {
    const result = await jobService.fetchPublishedJobs({
      q: searchQuery.value,
      type: jobType.value,
      mode: workMode.value,
      level: experienceLevel.value,
      location: location.value,
      page: page.value,
      pageSize,
    })

    jobs.value = result.items || []
    totalCount.value = result.totalCount
    totalPages.value = Math.max(1, result.totalPages)
  } catch (error) {
    console.error('Failed to load published jobs:', error)
    jobs.value = []
    totalCount.value = 0
    totalPages.value = 1
  } finally {
    loading.value = false
  }
}

// Submit search query and location
function handleSearchSubmit() {
  searchQuery.value = searchInput.value.trim()
  location.value = locationInput.value.trim()
  page.value = 1
  syncUrl()
  fetchJobs()
}

// Watch filters that trigger immediate reload
watch([jobType, workMode, experienceLevel], () => {
  page.value = 1
  syncUrl()
  fetchJobs()
})

// Watch page change
function goToPage(newPage: number) {
  if (newPage < 1 || newPage > totalPages.value || newPage === page.value) return
  page.value = newPage
  syncUrl()
  fetchJobs()
  window.scrollTo({ top: 0, behavior: 'smooth' })
}

// Reset all filters
function clearFilters() {
  searchQuery.value = ''
  searchInput.value = ''
  jobType.value = ''
  workMode.value = ''
  experienceLevel.value = ''
  location.value = ''
  locationInput.value = ''
  page.value = 1
  syncUrl()
  fetchJobs()
}

// Active filters count
const activeFiltersCount = computed(() => {
  let count = 0
  if (searchQuery.value) count++
  if (jobType.value) count++
  if (workMode.value) count++
  if (experienceLevel.value) count++
  if (location.value) count++
  return count
})

// Visible pagination window
const visiblePages = computed(() => {
  const current = page.value
  const total = totalPages.value
  const maxButtons = 5

  if (total <= maxButtons) {
    return Array.from({ length: total }, (_, i) => i + 1)
  }

  let start = Math.max(1, current - 2)
  let end = Math.min(total, start + maxButtons - 1)

  if (end - start < maxButtons - 1) {
    start = Math.max(1, end - maxButtons + 1)
  }

  const pages: (number | string)[] = []
  if (start > 1) {
    pages.push(1)
    if (start > 2) pages.push('...')
  }
  for (let i = start; i <= end; i++) {
    pages.push(i)
  }
  if (end < total) {
    if (end < total - 1) pages.push('...')
    pages.push(total)
  }

  return pages
})

// Format salary display
function formatSalary(job: Job): string | null {
  if (!job.show_salary || job.salary_min == null) return null
  const currency = job.salary_currency || 'INR'
  const period = job.salary_period ? `/${job.salary_period}` : ''
  if (job.salary_max != null) {
    return `${currency} ${job.salary_min.toLocaleString()} - ${job.salary_max.toLocaleString()}${period}`
  }
  return `${currency} ${job.salary_min.toLocaleString()}+${period}`
}

// Relative date format
function formatDate(iso: string | null | undefined): string {
  if (!iso) return ''
  const date = new Date(iso)
  const now = new Date()
  const diffDays = Math.floor((now.getTime() - date.getTime()) / (1000 * 60 * 60 * 24))
  if (diffDays === 0) return 'Today'
  if (diffDays === 1) return 'Yesterday'
  if (diffDays < 7) return `${diffDays} days ago`
  return date.toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' })
}

// Helper display labels
function getJobTypeLabel(val: string): string {
  const map: Record<string, string> = {
    full_time: 'Full-time',
    part_time: 'Part-time',
    contract: 'Contract',
    internship: 'Internship',
    freelance: 'Freelance',
    temporary: 'Temporary',
  }
  return map[val] || val.replace('_', ' ')
}

function getWorkModeLabel(val: string): string {
  const map: Record<string, string> = {
    onsite: 'On-site',
    remote: 'Remote',
    hybrid: 'Hybrid',
  }
  return map[val] || val
}

function getExperienceLabel(val: string): string {
  const map: Record<string, string> = {
    entry: 'Entry Level',
    junior: 'Junior',
    mid: 'Mid-Level',
    senior: 'Senior',
    lead: 'Lead',
    executive: 'Executive',
  }
  return map[val] || val
}
</script>

<template>
  <div class="min-h-screen bg-page text-text-body transition-colors">
    <!-- Header Banner -->
    <header class="bg-card border-b border-border py-8 sm:py-12">
      <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div class="max-w-3xl">
          <h1 class="text-2xl sm:text-3xl lg:text-4xl font-bold text-text-strong tracking-tight">
            Find your next opportunity
          </h1>
          <p class="mt-2 text-sm sm:text-base text-text-muted">
            Explore verified opportunities across top engineering, product, and business organizations.
          </p>
        </div>

        <!-- Search & Location Bar -->
        <form
          @submit.prevent="handleSearchSubmit"
          class="mt-6 flex flex-col md:flex-row gap-3 bg-card md:p-2 md:rounded-xl md:border md:border-border md:shadow-xs"
        >
          <!-- Keyword input -->
          <div class="relative flex-1">
            <Search class="absolute left-3.5 top-1/2 -translate-y-1/2 h-5 w-5 text-text-muted pointer-events-none" />
            <input
              v-model="searchInput"
              type="text"
              placeholder="Search by job title, skill, or keywords..."
              class="w-full bg-card rounded-lg md:rounded-md border border-border md:border-0 pl-11 pr-4 py-2.5 text-sm text-text-strong placeholder:text-text-subtle focus:outline-none focus:ring-2 focus:ring-primary/20 transition"
            />
          </div>

          <!-- Divider for desktop -->
          <div class="hidden md:block w-px bg-border my-1"></div>

          <!-- Location input -->
          <div class="relative md:w-72">
            <MapPin class="absolute left-3.5 top-1/2 -translate-y-1/2 h-5 w-5 text-text-muted pointer-events-none" />
            <input
              v-model="locationInput"
              type="text"
              placeholder="City, country or region..."
              class="w-full bg-card rounded-lg md:rounded-md border border-border md:border-0 pl-11 pr-4 py-2.5 text-sm text-text-strong placeholder:text-text-subtle focus:outline-none focus:ring-2 focus:ring-primary/20 transition"
            />
          </div>

          <!-- Actions -->
          <div class="flex items-center gap-2">
            <button
              type="submit"
              class="flex-1 md:flex-none inline-flex items-center justify-center gap-2 px-6 py-2.5 text-sm font-semibold rounded-lg bg-primary text-white hover:bg-primary-hover shadow-xs transition focus:outline-none focus:ring-2 focus:ring-primary/30"
            >
              <Search class="h-4 w-4" />
              <span>Search</span>
            </button>

            <!-- Mobile filter toggle button -->
            <button
              type="button"
              @click="showMobileFilters = !showMobileFilters"
              class="md:hidden inline-flex items-center gap-1.5 px-3 py-2.5 text-sm font-medium rounded-lg border border-border bg-card text-text-strong hover:bg-surface-raised transition"
            >
              <SlidersHorizontal class="h-4 w-4" />
              <span v-if="activeFiltersCount > 0" class="h-5 w-5 rounded-full bg-primary text-white text-xs flex items-center justify-center font-bold">
                {{ activeFiltersCount }}
              </span>
            </button>
          </div>
        </form>

        <!-- Active Filter Badges -->
        <div v-if="activeFiltersCount > 0" class="mt-4 flex flex-wrap items-center gap-2 text-xs">
          <span class="text-text-muted font-medium">Active filters:</span>

          <span
            v-if="searchQuery"
            class="inline-flex items-center gap-1 px-2.5 py-1 rounded-md bg-surface-raised border border-border text-text-strong font-medium"
          >
            "{{ searchQuery }}"
            <button type="button" @click="searchQuery = ''; searchInput = ''; page = 1; syncUrl(); fetchJobs()" class="hover:text-primary">
              <X class="h-3 w-3" />
            </button>
          </span>

          <span
            v-if="location"
            class="inline-flex items-center gap-1 px-2.5 py-1 rounded-md bg-surface-raised border border-border text-text-strong font-medium"
          >
            Location: {{ location }}
            <button type="button" @click="location = ''; locationInput = ''; page = 1; syncUrl(); fetchJobs()" class="hover:text-primary">
              <X class="h-3 w-3" />
            </button>
          </span>

          <span
            v-if="jobType"
            class="inline-flex items-center gap-1 px-2.5 py-1 rounded-md bg-surface-raised border border-border text-text-strong font-medium"
          >
            Type: {{ getJobTypeLabel(jobType) }}
            <button type="button" @click="jobType = ''; page = 1; syncUrl(); fetchJobs()" class="hover:text-primary">
              <X class="h-3 w-3" />
            </button>
          </span>

          <span
            v-if="workMode"
            class="inline-flex items-center gap-1 px-2.5 py-1 rounded-md bg-surface-raised border border-border text-text-strong font-medium"
          >
            Mode: {{ getWorkModeLabel(workMode) }}
            <button type="button" @click="workMode = ''; page = 1; syncUrl(); fetchJobs()" class="hover:text-primary">
              <X class="h-3 w-3" />
            </button>
          </span>

          <span
            v-if="experienceLevel"
            class="inline-flex items-center gap-1 px-2.5 py-1 rounded-md bg-surface-raised border border-border text-text-strong font-medium"
          >
            Level: {{ getExperienceLabel(experienceLevel) }}
            <button type="button" @click="experienceLevel = ''; page = 1; syncUrl(); fetchJobs()" class="hover:text-primary">
              <X class="h-3 w-3" />
            </button>
          </span>

          <button
            type="button"
            @click="clearFilters"
            class="inline-flex items-center gap-1 text-primary hover:underline font-semibold ml-1"
          >
            <RotateCcw class="h-3 w-3" />
            Clear all
          </button>
        </div>
      </div>
    </header>

    <!-- Main Content Area -->
    <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
      <div class="flex flex-col md:flex-row gap-8 items-start">
        
        <!-- Sidebar Filters (Desktop + Mobile Drawer) -->
        <aside
          :class="[
            'w-full md:w-64 lg:w-72 flex-shrink-0 transition-all',
            showMobileFilters ? 'block mb-6 md:mb-0' : 'hidden md:block'
          ]"
        >
          <div class="bg-card border border-border rounded-xl p-5 shadow-xs space-y-6">
            <div class="flex items-center justify-between pb-3 border-b border-border">
              <div class="flex items-center gap-2">
                <SlidersHorizontal class="h-4 w-4 text-primary" />
                <h2 class="font-semibold text-text-strong text-sm">Filter Opportunities</h2>
              </div>
              <button
                v-if="activeFiltersCount > 0"
                type="button"
                @click="clearFilters"
                class="text-xs font-medium text-primary hover:underline"
              >
                Reset
              </button>
            </div>

            <!-- Job Type Filter -->
            <div>
              <label class="block text-xs font-semibold uppercase tracking-wider text-text-muted mb-2">
                Employment Type
              </label>
              <select
                v-model="jobType"
                class="w-full bg-card rounded-lg border border-border px-3 py-2 text-sm text-text-strong focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition"
              >
                <option value="">All Employment Types</option>
                <option value="full_time">Full-time</option>
                <option value="part_time">Part-time</option>
                <option value="contract">Contract</option>
                <option value="internship">Internship</option>
                <option value="freelance">Freelance</option>
                <option value="temporary">Temporary</option>
              </select>
            </div>

            <!-- Work Mode Filter -->
            <div>
              <label class="block text-xs font-semibold uppercase tracking-wider text-text-muted mb-2">
                Workplace Mode
              </label>
              <select
                v-model="workMode"
                class="w-full bg-card rounded-lg border border-border px-3 py-2 text-sm text-text-strong focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition"
              >
                <option value="">All Workplace Modes</option>
                <option value="onsite">On-site</option>
                <option value="remote">Remote</option>
                <option value="hybrid">Hybrid</option>
              </select>
            </div>

            <!-- Experience Level Filter -->
            <div>
              <label class="block text-xs font-semibold uppercase tracking-wider text-text-muted mb-2">
                Experience Level
              </label>
              <select
                v-model="experienceLevel"
                class="w-full bg-card rounded-lg border border-border px-3 py-2 text-sm text-text-strong focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition"
              >
                <option value="">All Experience Levels</option>
                <option value="entry">Entry Level</option>
                <option value="junior">Junior</option>
                <option value="mid">Mid-Level</option>
                <option value="senior">Senior</option>
                <option value="lead">Lead</option>
                <option value="executive">Executive</option>
              </select>
            </div>

          </div>
        </aside>

        <!-- Job Results Feed -->
        <main class="flex-1 w-full min-w-0">
          <!-- Top Meta Bar -->
          <div class="flex items-center justify-between mb-4">
            <p class="text-sm text-text-muted">
              <span v-if="loading">Searching verified listings...</span>
              <span v-else-if="totalCount === 0">No matching jobs found</span>
              <span v-else>
                Showing
                <strong class="text-text-strong font-semibold">
                  {{ (page - 1) * pageSize + 1 }}
                </strong>
                -
                <strong class="text-text-strong font-semibold">
                  {{ Math.min(page * pageSize, totalCount) }}
                </strong>
                of
                <strong class="text-text-strong font-semibold">{{ totalCount }}</strong>
                jobs
              </span>
            </p>
          </div>

          <!-- Loading Skeleton Slices -->
          <div v-if="loading" class="space-y-4">
            <div
              v-for="i in 4"
              :key="i"
              class="bg-card border border-border rounded-xl p-5 sm:p-6 shadow-xs animate-pulse"
            >
              <div class="flex gap-4">
                <div class="h-12 w-12 rounded-lg bg-surface-raised flex-shrink-0"></div>
                <div class="flex-1 space-y-3">
                  <div class="h-5 bg-surface-raised rounded w-1/2"></div>
                  <div class="h-4 bg-surface-raised rounded w-1/3"></div>
                  <div class="h-3 bg-surface-raised rounded w-full"></div>
                  <div class="flex gap-2 pt-2">
                    <div class="h-6 w-20 bg-surface-raised rounded-md"></div>
                    <div class="h-6 w-20 bg-surface-raised rounded-md"></div>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <!-- Empty State -->
          <div
            v-else-if="jobs.length === 0"
            class="bg-card border border-border rounded-xl p-10 sm:p-14 text-center shadow-xs"
          >
            <div class="h-12 w-12 rounded-full bg-surface-raised text-text-muted mx-auto flex items-center justify-center mb-4">
              <Briefcase class="h-6 w-6" />
            </div>
            <h3 class="text-base sm:text-lg font-semibold text-text-strong">
              No jobs match your criteria
            </h3>
            <p class="mt-1 text-sm text-text-muted max-w-sm mx-auto">
              Try adjusting your search terms, removing filters, or searching for a broader location.
            </p>
            <button
              type="button"
              @click="clearFilters"
              class="mt-5 inline-flex items-center gap-1.5 px-4 py-2 text-sm font-semibold rounded-lg bg-primary text-white hover:bg-primary-hover shadow-xs transition"
            >
              <RotateCcw class="h-4 w-4" />
              <span>Reset all filters</span>
            </button>
          </div>

          <!-- Jobs List Feed -->
          <div v-else class="space-y-4">
            <article
              v-for="job in jobs"
              :key="job.id"
              @click="router.push(`/jobs/${job.id}`)"
              class="group bg-card border border-border hover:border-primary/60 rounded-xl p-5 sm:p-6 shadow-xs hover:shadow-sm transition-all cursor-pointer relative"
            >
              <div class="flex items-start gap-4">
                <!-- Company Logo Avatar -->
                <div class="h-12 w-12 rounded-lg bg-surface-raised border border-border flex items-center justify-center overflow-hidden flex-shrink-0">
                  <img
                    v-if="job.organization?.avatar_path"
                    :src="getOrganizationImageUrl(job.organization.avatar_path) || ''"
                    :alt="job.organization?.name || 'Company'"
                    class="h-full w-full object-cover"
                  />
                  <Building2 v-else class="h-6 w-6 text-text-muted" />
                </div>

                <!-- Job Information -->
                <div class="flex-1 min-w-0">
                  <div class="flex flex-col sm:flex-row sm:items-start justify-between gap-1">
                    <div>
                      <h2 class="text-base sm:text-lg font-semibold text-text-strong group-hover:text-primary transition-colors">
                        {{ job.title }}
                      </h2>
                      <div class="mt-0.5 flex flex-wrap items-center gap-x-2 text-xs sm:text-sm text-text-muted">
                        <span class="font-medium text-text-strong">
                          {{ job.organization?.name || 'Verified Company' }}
                        </span>
                        <span v-if="job.city || job.organization?.city">
                          • {{ job.city || job.organization?.city }}<span v-if="job.country_code">, {{ job.country_code }}</span>
                        </span>
                        <span v-if="job.department">
                          • {{ job.department }}
                        </span>
                      </div>
                    </div>

                    <!-- Date Tag -->
                    <div class="text-xs text-text-muted whitespace-nowrap flex items-center gap-1 self-start mt-1 sm:mt-0">
                      <Clock class="h-3.5 w-3.5" />
                      <span>{{ formatDate(job.published_at) }}</span>
                    </div>
                  </div>

                  <!-- Short snippet / excerpt -->
                  <p v-if="job.short_description" class="mt-2 text-xs sm:text-sm text-text-body line-clamp-2 leading-relaxed">
                    {{ job.short_description }}
                  </p>

                  <!-- Badges / Tags -->
                  <div class="mt-3.5 flex flex-wrap items-center gap-2">
                    <!-- Job Type -->
                    <span class="inline-flex items-center gap-1 px-2.5 py-1 rounded-md text-xs font-medium bg-surface-raised text-text-strong border border-border">
                      <Briefcase class="h-3 w-3 text-text-muted" />
                      {{ getJobTypeLabel(job.job_type) }}
                    </span>

                    <!-- Work Mode -->
                    <span class="inline-flex items-center gap-1 px-2.5 py-1 rounded-md text-xs font-medium bg-surface-raised text-text-strong border border-border">
                      <MapPin class="h-3 w-3 text-text-muted" />
                      {{ getWorkModeLabel(job.work_mode) }}
                    </span>

                    <!-- Experience Level -->
                    <span
                      v-if="job.experience_level"
                      class="inline-flex items-center px-2.5 py-1 rounded-md text-xs font-medium bg-surface-raised text-text-muted border border-border"
                    >
                      {{ getExperienceLabel(job.experience_level) }}
                    </span>

                    <!-- Salary -->
                    <span
                      v-if="formatSalary(job)"
                      class="inline-flex items-center gap-1 px-2.5 py-1 rounded-md text-xs font-semibold bg-success-bg text-success-text border border-success-border"
                    >
                      <Banknote class="h-3 w-3" />
                      {{ formatSalary(job) }}
                    </span>

                    <!-- Deadline indicator -->
                    <span
                      v-if="job.application_deadline"
                      class="hidden sm:inline-flex items-center gap-1 px-2.5 py-1 rounded-md text-xs text-text-muted"
                    >
                      <Calendar class="h-3 w-3" />
                      Deadline: {{ formatDate(job.application_deadline) }}
                    </span>
                  </div>
                </div>
              </div>
            </article>
          </div>

          <!-- Server Pagination Controls -->
          <div
            v-if="totalPages > 1"
            class="mt-8 pt-6 border-t border-border flex flex-col sm:flex-row items-center justify-between gap-4"
          >
            <p class="text-xs sm:text-sm text-text-muted">
              Page <span class="font-semibold text-text-strong">{{ page }}</span> of
              <span class="font-semibold text-text-strong">{{ totalPages }}</span>
            </p>

            <nav aria-label="Pagination Navigation" class="inline-flex items-center gap-1">
              <!-- Previous Button -->
              <button
                type="button"
                :disabled="page <= 1"
                @click="goToPage(page - 1)"
                class="inline-flex items-center gap-1 px-3 py-1.5 text-xs sm:text-sm font-medium rounded-lg border border-border bg-card text-text-strong hover:bg-surface-raised disabled:opacity-40 disabled:pointer-events-none transition"
              >
                <ChevronLeft class="h-4 w-4" />
                <span class="hidden sm:inline">Previous</span>
              </button>

              <!-- Page Number Buttons -->
              <template v-for="p in visiblePages" :key="p">
                <span
                  v-if="p === '...'"
                  class="px-2 py-1 text-xs sm:text-sm text-text-subtle select-none"
                >
                  ...
                </span>
                <button
                  v-else
                  type="button"
                  @click="goToPage(Number(p))"
                  :class="[
                    'h-8 w-8 sm:h-9 sm:w-9 rounded-lg text-xs sm:text-sm font-medium transition flex items-center justify-center',
                    page === p
                      ? 'bg-primary text-white shadow-xs font-bold'
                      : 'bg-card border border-border text-text-strong hover:bg-surface-raised'
                  ]"
                >
                  {{ p }}
                </button>
              </template>

              <!-- Next Button -->
              <button
                type="button"
                :disabled="page >= totalPages"
                @click="goToPage(page + 1)"
                class="inline-flex items-center gap-1 px-3 py-1.5 text-xs sm:text-sm font-medium rounded-lg border border-border bg-card text-text-strong hover:bg-surface-raised disabled:opacity-40 disabled:pointer-events-none transition"
              >
                <span class="hidden sm:inline">Next</span>
                <ChevronRight class="h-4 w-4" />
              </button>
            </nav>
          </div>
        </main>
      </div>
    </div>
  </div>
</template>