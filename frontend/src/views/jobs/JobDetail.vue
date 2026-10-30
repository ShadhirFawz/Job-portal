<script setup lang="ts">
import { onMounted, computed, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useJobsStore } from '@/stores/jobs'
import { useAuthStore } from '@/stores/auth'
import { organizationService } from '@/services/organization.service'
import { applicationService } from '@/services/application.service'
import { getOrganizationImageUrl } from '@/composables/useOrganizationImage'
import { formatJobDate } from '@/utils/date'
import {
  Briefcase,
  Building2,
  Calendar,
  CheckCircle2,
  Clock,
  Code2,
  DollarSign,
  Gift,
  Globe,
  MapPin,
  Sparkles,
  EyeOff,
  ArrowLeft,
  Share2,
  Check,
  Users,
} from 'lucide-vue-next'
import { toast } from 'vue-sonner'

const route = useRoute()
const router = useRouter()
const jobsStore = useJobsStore()
const auth = useAuthStore()

const isOrgStaffOrAdmin = ref(false)
const hasApplied = ref(false)

const job = computed(() => jobsStore.currentJob)

onMounted(async () => {
  const jobId = route.params.id as string
  await jobsStore.fetchPublicJob(jobId)

  // Fallback: If not published, org member might be previewing their draft
  if (!jobsStore.currentJob) {
    await jobsStore.fetchJob(jobId)
  }

  // Check if current user is an org staff or admin of this job's organization
  if (auth.user && job.value) {
    isOrgStaffOrAdmin.value = await organizationService.checkIsOrgAdminOrStaff(
      job.value.organization_id,
      auth.user.id,
      auth.user.role,
      job.value.created_by
    )

    // Check if user already applied
    hasApplied.value = await applicationService.checkUserApplied(
      jobId,
      auth.user.id
    )
  }
})

// Whether selection processes are allowed to be seen
const canViewSelectionProcess = computed(() => {
  if (!job.value) return false
  if (job.value.is_selection_process_public) return true
  return isOrgStaffOrAdmin.value
})

function handleShare() {
  if (navigator.clipboard) {
    navigator.clipboard.writeText(window.location.href)
    toast.success('Job link copied to clipboard!')
  }
}
</script>

<template>
  <div v-if="jobsStore.loading" class="flex items-center justify-center min-h-[60vh] text-text-muted px-4">
    <div class="text-center">
      <div class="h-8 w-8 animate-spin rounded-full border-2 border-primary border-t-transparent mx-auto mb-3"></div>
      <p class="text-xs sm:text-sm">Loading job details…</p>
    </div>
  </div>

  <div v-else-if="job" class="min-h-screen bg-page py-6 sm:py-10 px-3 sm:px-6 lg:px-8">
    <div class="max-w-4xl mx-auto space-y-6">
      
      <!-- Top Bar / Navigation -->
      <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-2 sm:gap-4">
        <button
          type="button"
          @click="router.back()"
          class="inline-flex items-center gap-1.5 text-xs sm:text-sm font-medium text-text-muted hover:text-text-strong transition focus:outline-none focus:ring-2 focus:ring-primary/25 rounded-md"
        >
          <ArrowLeft class="h-4 w-4" /> Back
        </button>

        <div class="flex items-center flex-wrap gap-2">
          <router-link
            v-if="isOrgStaffOrAdmin"
            :to="`/jobs/${job.id}/applications`"
            class="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold text-primary bg-primary-light border border-primary/30 rounded-lg hover:bg-primary/10 transition shadow-xs"
          >
            <Users class="h-3.5 w-3.5" />
            Applications ({{ job.applications_count || 0 }})
          </router-link>

          <button
            type="button"
            @click="handleShare"
            class="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold text-text-secondary bg-card border border-border rounded-lg hover:bg-surface-raised transition shadow-xs"
          >
            <Share2 class="h-3.5 w-3.5 text-text-muted" /> Share
          </button>
        </div>
      </div>

      <!-- Main Job Card -->
      <div class="bg-card rounded-xl sm:rounded-2xl shadow-sm border border-border overflow-hidden">
        
        <!-- Header banner -->
        <div class="p-4 sm:p-6 lg:p-8 border-b border-border">
          <div class="flex flex-col lg:flex-row lg:items-start justify-between gap-4 sm:gap-6">
            <div class="flex items-start gap-3.5 sm:gap-4 min-w-0">
              
              <!-- Org Logo / Initial -->
              <div class="h-14 w-14 sm:h-16 sm:w-16 rounded-xl sm:rounded-2xl ring-2 ring-border bg-gradient-to-br from-indigo-600 to-indigo-800 overflow-hidden flex-shrink-0 flex items-center justify-center shadow-sm">
                <img
                  v-if="job.organization?.avatar_path"
                  :src="getOrganizationImageUrl(job.organization.avatar_path) || ''"
                  class="h-full w-full object-cover"
                  :alt="job.organization?.name || 'Company'"
                />
                <span v-else class="text-xl sm:text-2xl font-bold text-white select-none">
                  {{ (job.organization?.name || 'C').charAt(0).toUpperCase() }}
                </span>
              </div>

              <div class="min-w-0 flex-1">
                <h1 class="text-sm sm:text-2xl lg:text-3xl font-bold text-text-strong tracking-tight truncate">
                  {{ job.title }}
                </h1>
                <div class="mt-1.5 flex flex-wrap items-center gap-x-2 gap-y-1 text-xs sm:text-sm text-text-secondary">
                  <router-link
                    v-if="job.organization?.id"
                    :to="`/organizations/${job.organization.id}`"
                    class="font-medium text-primary hover:underline truncate"
                  >
                    {{ job.organization?.name }}
                  </router-link>
                  <span v-if="job.city">•</span>
                  <span v-if="job.city" class="inline-flex items-center gap-1">
                    <MapPin class="h-3.5 w-3.5 text-text-subtle flex-shrink-0" /> {{ job.city }}
                  </span>
                  <span>•</span>
                  <span class="inline-flex items-center gap-1 text-text-muted font-medium whitespace-nowrap">
                    <Clock class="h-3.5 w-3.5 text-text-subtle flex-shrink-0" />
                    {{ formatJobDate(job.created_at, job.updated_at) }}
                  </span>
                </div>
              </div>
            </div>

            <!-- Job Status Badge -->
            <span
              class="self-start px-2.5 py-1 text-[11px] sm:text-xs font-semibold rounded-full capitalize border"
              :class="{
                'badge-success': job.status === 'published',
                'badge-neutral': job.status === 'draft',
                'badge-warning': job.status === 'paused',
                'badge-danger': job.status === 'closed',
              }"
            >
              {{ job.status }}
            </span>
          </div>

          <!-- Metadata Badges -->
          <div class="mt-5 sm:mt-6 flex flex-wrap items-center gap-2 text-xs font-medium">
            <span class="px-3 py-1 bg-surface-raised text-text-secondary rounded-md capitalize border border-border">
              {{ job.job_type.replace('_', ' ') }}
            </span>
            <span class="px-3 py-1 bg-surface-raised text-text-secondary rounded-md capitalize border border-border">
              {{ job.work_mode }}
            </span>
            <span v-if="job.experience_level" class="px-3 py-1 bg-surface-raised text-text-secondary rounded-md capitalize border border-border">
              {{ job.experience_level }} level
            </span>
            <span v-if="job.department" class="px-3 py-1 bg-surface-raised text-text-secondary rounded-md border border-border">
              {{ job.department }}
            </span>
            <span
              v-if="job.show_salary && (job.salary_min || job.salary_max)"
              class="px-3 py-1 bg-primary-light text-primary border border-primary/30 rounded-full font-semibold"
            >
              {{ job.salary_currency }}
              {{ job.salary_min?.toLocaleString() }}{{ job.salary_max ? ' - ' + job.salary_max.toLocaleString() : '+' }}
            </span>
          </div>
        </div>

        <!-- Body content -->
        <div class="p-4 sm:p-6 lg:p-8 space-y-6 sm:space-y-8">
          
          <!-- Short Description -->
          <div v-if="job.short_description" class="p-4 bg-primary-light border border-primary/20 rounded-xl text-xs sm:text-sm text-text-secondary leading-relaxed font-medium">
            {{ job.short_description }}
          </div>

          <!-- TECH STACKS HIGHLIGHTED SEPARATELY -->
          <div
            v-if="job.tech_stacks && job.tech_stacks.length > 0"
            class="p-4 sm:p-5 rounded-xl sm:rounded-2xl bg-surface-raised border border-border shadow-xs space-y-3"
          >
            <div class="flex items-center gap-2 text-xs sm:text-sm font-bold text-text-strong uppercase tracking-wide">
              <Code2 class="h-4 w-4 text-primary flex-shrink-0" />
              <span>Tech Stacks you'll work with</span>
            </div>
            <div class="flex flex-wrap gap-1.5 sm:gap-2 pt-1">
              <span
                v-for="tech in job.tech_stacks"
                :key="tech"
                class="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-lg sm:rounded-xl text-[10px] font-semibold bg-card text-primary border border-primary/30 shadow-xs hover:border-primary transition-colors"
              >
                <span class="h-1.5 w-1.5 rounded-full bg-primary"></span>
                {{ tech }}
              </span>
            </div>
          </div>

          <!-- About the Role / Full Description -->
          <div v-if="job.description" class="space-y-2">
            <h2 class="text-sm sm:text-base font-bold text-text-strong">About the role</h2>
            <div class="rich-text-preview text-xs sm:text-sm leading-relaxed" v-html="job.description"></div>
          </div>

          <!-- Responsibilities -->
          <div v-if="job.responsibilities" class="space-y-2 pt-4 border-t border-border">
            <h2 class="text-sm sm:text-base font-bold text-text-strong">Responsibilities</h2>
            <div class="rich-text-preview text-xs sm:text-sm leading-relaxed" v-html="job.responsibilities"></div>
          </div>

          <!-- Requirements -->
          <div v-if="job.requirements" class="space-y-2 pt-4 border-t border-border">
            <h2 class="text-sm sm:text-base font-bold text-text-strong">Requirements</h2>
            <div class="rich-text-preview text-xs sm:text-sm leading-relaxed" v-html="job.requirements"></div>
          </div>

          <!-- Nice to Have -->
          <div v-if="job.nice_to_have" class="space-y-2 pt-4 border-t border-border">
            <h2 class="text-sm sm:text-base font-bold text-text-strong flex items-center gap-1.5">
              <Check class="h-4 w-4 text-text-secondary flex-shrink-0" /> Nice to have
            </h2>
            <div class="rich-text-preview text-xs sm:text-sm leading-relaxed" v-html="job.nice_to_have"></div>
          </div>

          <!-- What We Offer -->
          <div v-if="job.what_we_offer || job.benefits" class="space-y-2 pt-4 border-t border-border">
            <h2 class="text-sm sm:text-base font-bold text-text-strong flex items-center gap-1.5">
              <Gift class="h-4 w-4 text-text-secondary flex-shrink-0" /> What we offer
            </h2>
            <div class="rich-text-preview text-xs sm:text-sm leading-relaxed" v-html="job.what_we_offer || job.benefits"></div>
          </div>

          <!-- HIRING PROCESS / SELECTION STAGES -->
          <div class="pt-4 border-t border-border">
            <!-- OPTION A: Custom Hiring Instructions -->
            <div v-if="job.has_custom_hiring_process" class="bg-primary-light rounded-xl sm:rounded-2xl p-4 sm:p-6 border border-primary/20 space-y-3">
              <h2 class="text-sm sm:text-base font-bold text-text-strong flex items-center gap-2">
                <CheckCircle2 class="h-5 w-5 text-primary flex-shrink-0" /> How to Apply
              </h2>
              <p class="text-xs text-text-muted leading-relaxed">
                This company manages their application process directly. Please follow the instructions below:
              </p>
              <div
                v-if="job.how_to_apply"
                class="rich-text-preview text-xs sm:text-sm leading-relaxed mt-2"
                v-html="job.how_to_apply"
              ></div>
              <p v-else class="text-xs sm:text-sm text-text-secondary">
                Please visit the company website or contact {{ job.organization?.name }} directly to submit your application.
              </p>
            </div>

            <!-- OPTION B: Platform Selection Stages -->
            <div v-else-if="job.selection_processes?.length && canViewSelectionProcess" class="space-y-4">
              <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-2">
                <div>
                  <h2 class="text-sm sm:text-base font-bold text-text-strong">Selection Process Stages</h2>
                  <p class="text-xs text-text-muted">Stages applicants will navigate through for this role.</p>
                </div>
                <span
                  v-if="!job.is_selection_process_public && isOrgStaffOrAdmin"
                  class="inline-flex items-center gap-1 px-2.5 py-1 text-[11px] font-semibold rounded-md alert-warning self-start sm:self-auto"
                  title="Hidden from public applicants"
                >
                  <EyeOff class="h-3.5 w-3.5 flex-shrink-0" /> Staff Only (Hidden from candidates)
                </span>
              </div>

              <ol class="space-y-3">
                <li
                  v-for="(p, idx) in job.selection_processes.sort((a, b) => a.order_index - b.order_index)"
                  :key="p.id || idx"
                  class="flex items-start gap-3.5 sm:gap-4 p-3.5 sm:p-4 border border-border rounded-xl bg-surface-raised hover:bg-card transition shadow-xs"
                >
                  <span class="flex h-6 w-6 sm:h-7 sm:w-7 items-center justify-center rounded-full bg-primary text-white text-xs font-bold flex-shrink-0">
                    {{ idx + 1 }}
                  </span>
                  <div class="flex-1 min-w-0">
                    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-1 sm:gap-2">
                      <p class="font-semibold text-text-strong text-xs sm:text-sm truncate">{{ p.title }}</p>
                      <span v-if="p.is_mandatory" class="text-[10px] uppercase font-bold text-primary bg-primary-light px-2 py-0.5 rounded self-start sm:self-auto">
                        Mandatory
                      </span>
                    </div>
                    <p class="text-[11px] sm:text-xs text-text-muted capitalize mt-0.5">
                      {{ p.process_type.replace('_', ' + ') }}
                      <span v-if="p.time_limit_minutes">• {{ p.time_limit_minutes }} mins</span>
                    </p>
                    <p v-if="p.description" class="text-xs text-text-secondary mt-1.5 leading-relaxed">
                      {{ p.description }}
                    </p>
                  </div>
                </li>
              </ol>
            </div>
          </div>

          <!-- Bottom Apply Section -->
          <div class="pt-6 border-t border-border flex flex-col lg:flex-row lg:items-center justify-between gap-4">
            <div class="space-y-0.5">
              <p class="text-xs sm:text-sm font-semibold text-text-strong">
                {{ isOrgStaffOrAdmin ? 'Company Management' : 'Interested in this role?' }}
              </p>
              <p class="text-xs text-text-muted leading-relaxed">
                <span v-if="isOrgStaffOrAdmin">
                  You have {{ job.applications_count || 0 }} candidate applications for this role.
                </span>
                <span v-else-if="hasApplied">
                  You have already applied to this position.
                </span>
                <span v-else>
                  {{ job.has_custom_hiring_process ? 'Follow the instructions in "How to Apply" above.' : 'Submit your profile and advance through the evaluation stages.' }}
                </span>
              </p>
            </div>

            <div class="flex items-center gap-3 w-full lg:w-auto">
              <router-link
                v-if="isOrgStaffOrAdmin"
                :to="`/jobs/${job.id}/applications`"
                class="w-full lg:w-auto inline-flex justify-center items-center gap-2 px-6 py-3 text-xs sm:text-sm font-semibold rounded-xl text-white bg-primary hover:bg-primary-hover shadow-sm transition"
              >
                <Users class="h-4 w-4" />
                View Applications ({{ job.applications_count || 0 }})
              </router-link>

              <router-link
                v-else-if="hasApplied"
                :to="`/jobs/${job.id}/apply`"
                class="w-full lg:w-auto inline-flex justify-center items-center gap-2 px-6 py-3 text-xs sm:text-sm font-semibold rounded-xl text-success-text bg-success-bg hover:bg-success/15 border border-success-border shadow-sm transition"
              >
                <Check class="h-4 w-4" />
                View Submitted Application
              </router-link>

              <router-link
                v-else
                :to="`/jobs/${job.id}/apply`"
                class="w-full lg:w-auto inline-flex justify-center items-center px-8 py-3 text-xs sm:text-sm font-semibold rounded-xl text-white bg-primary hover:bg-primary-hover shadow-sm transition"
              >
                {{ job.has_custom_hiring_process ? 'Apply via Instructions' : 'Apply for this position' }}
              </router-link>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>

  <div v-else class="p-12 sm:p-16 text-center text-text-muted">
    <Building2 class="h-12 w-12 text-text-subtle mx-auto mb-3" />
    <h2 class="text-base sm:text-lg font-bold text-text-strong">Job Not Found</h2>
    <p class="text-xs sm:text-sm text-text-muted mt-1">This job posting may have been paused, closed, or removed.</p>
  </div>
</template>