<script setup lang="ts">
import { onMounted, computed, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useJobsStore } from '@/stores/jobs'
import { useAuthStore } from '@/stores/auth'
import { supabase } from '@/lib/supabase'
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
} from 'lucide-vue-next'
import { toast } from 'vue-sonner'

const route = useRoute()
const router = useRouter()
const jobsStore = useJobsStore()
const auth = useAuthStore()

const isOrgStaffOrAdmin = ref(false)

const job = computed(() => jobsStore.currentJob)

onMounted(async () => {
  const jobId = route.params.id as string
  await jobsStore.fetchPublicJob(jobId)

  // Check if current user is an org staff or admin of this job's organization
  if (auth.user && job.value) {
    if (auth.user.role === 'super_admin' || job.value.created_by === auth.user.id) {
      isOrgStaffOrAdmin.value = true
    } else {
      const { data: member } = await supabase
        .from('organization_members')
        .select('role, status')
        .eq('organization_id', job.value.organization_id)
        .eq('user_id', auth.user.id)
        .eq('status', 'active')
        .maybeSingle()

      if (member && (member.role === 'org_admin' || member.role === 'org_staff')) {
        isOrgStaffOrAdmin.value = true
      }
    }
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
  <div v-if="jobsStore.loading" class="flex items-center justify-center min-h-[60vh] text-gray-500">
    <div class="text-center">
      <div class="h-8 w-8 animate-spin rounded-full border-2 border-indigo-600 border-t-transparent mx-auto mb-3"></div>
      <p class="text-sm">Loading job details…</p>
    </div>
  </div>

  <div v-else-if="job" class="min-h-screen bg-gray-50/70 py-8 px-4 sm:px-6 lg:px-8">
    <div class="max-w-4xl mx-auto space-y-6">
      <!-- Top Bar / Navigation -->
      <div class="flex items-center justify-between">
        <button
          type="button"
          @click="router.back()"
          class="inline-flex items-center gap-1.5 text-sm font-medium text-gray-600 hover:text-gray-900 transition"
        >
          <ArrowLeft class="h-4 w-4" /> Back
        </button>

        <button
          type="button"
          @click="handleShare"
          class="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold text-gray-700 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 transition shadow-2xs"
        >
          <Share2 class="h-3.5 w-3.5 text-gray-500" /> Share
        </button>
      </div>

      <!-- Main Job Card -->
      <div class="bg-white rounded-2xl shadow-sm border border-gray-200 overflow-hidden">
        <!-- Header banner -->
        <div class="p-6 sm:p-8 border-b border-gray-100">
          <div class="flex flex-col sm:flex-row sm:items-start justify-between gap-6">
            <div class="flex items-start gap-4">
              <!-- Org Logo / Initial -->
              <div class="h-16 w-16 rounded-2xl ring-2 ring-gray-100 bg-linear-to-br from-indigo-600 to-purple-700 overflow-hidden shrink-0 flex items-center justify-center shadow-xs">
                <img
                  v-if="job.organization?.avatar_path"
                  :src="getOrganizationImageUrl(job.organization.avatar_path) || ''"
                  class="h-full w-full object-cover"
                  :alt="job.organization?.name || 'Company'"
                />
                <span v-else class="text-2xl font-bold text-white select-none">
                  {{ (job.organization?.name || 'C').charAt(0).toUpperCase() }}
                </span>
              </div>

              <div>
                <h1 class="text-2xl sm:text-3xl font-bold text-gray-900 tracking-tight">
                  {{ job.title }}
                </h1>
                <div class="mt-1 flex flex-wrap items-center gap-x-2 gap-y-1 text-sm text-gray-600">
                  <router-link
                    v-if="job.organization?.id"
                    :to="`/organizations/${job.organization.id}`"
                    class="font-medium text-indigo-600 hover:underline"
                  >
                    {{ job.organization?.name }}
                  </router-link>
                  <span v-if="job.city">•</span>
                  <span v-if="job.city" class="inline-flex items-center gap-1">
                    <MapPin class="h-3.5 w-3.5 text-gray-400" /> {{ job.city }}
                  </span>
                  <span>•</span>
                  <!-- Formatted Posted / Updated Date -->
                  <span class="inline-flex items-center gap-1 text-gray-500 font-medium">
                    <Clock class="h-3.5 w-3.5 text-gray-400" />
                    {{ formatJobDate(job.created_at, job.updated_at) }}
                  </span>
                </div>
              </div>
            </div>

            <!-- Job Status Badge -->
            <span
              class="self-start px-3 py-1 text-xs font-semibold rounded-full capitalize"
              :class="{
                'bg-green-100 text-green-800': job.status === 'published',
                'bg-gray-100 text-gray-800': job.status === 'draft',
                'bg-yellow-100 text-yellow-800': job.status === 'paused',
                'bg-red-100 text-red-800': job.status === 'closed',
              }"
            >
              {{ job.status }}
            </span>
          </div>

          <!-- Metadata Badges -->
          <div class="mt-6 flex flex-wrap items-center gap-2.5 text-xs font-medium">
            <span class="px-3 py-1 bg-gray-100 text-gray-800 rounded-full capitalize">
              {{ job.job_type.replace('_', ' ') }}
            </span>
            <span class="px-3 py-1 bg-gray-100 text-gray-800 rounded-full capitalize">
              {{ job.work_mode }}
            </span>
            <span v-if="job.experience_level" class="px-3 py-1 bg-gray-100 text-gray-800 rounded-full capitalize">
              {{ job.experience_level }} level
            </span>
            <span v-if="job.department" class="px-3 py-1 bg-gray-100 text-gray-800 rounded-full">
              {{ job.department }}
            </span>
            <span
              v-if="job.show_salary && (job.salary_min || job.salary_max)"
              class="px-3 py-1 bg-emerald-50 text-emerald-800 border border-emerald-200 rounded-full font-semibold"
            >
              {{ job.salary_currency }}
              {{ job.salary_min?.toLocaleString() }}{{ job.salary_max ? ' - ' + job.salary_max.toLocaleString() : '+' }}
            </span>
          </div>
        </div>

        <!-- Body content -->
        <div class="p-6 sm:p-8 space-y-8">
          <!-- Short Description -->
          <div v-if="job.short_description" class="p-4 bg-indigo-50/50 border border-indigo-100 rounded-xl text-sm text-indigo-900 leading-relaxed font-medium">
            {{ job.short_description }}
          </div>

          <!-- TECH STACKS HIGHLIGHTED SEPARATELY -->
          <div
            v-if="job.tech_stacks && job.tech_stacks.length > 0"
            class="p-5 rounded-2xl bg-linear-to-r from-gray-50 via-indigo-50/30 to-purple-50/30 border border-gray-200 shadow-2xs space-y-3"
          >
            <div class="flex items-center gap-2 text-sm font-bold text-gray-900 uppercase tracking-wide">
              <Code2 class="h-4 w-4 text-indigo-600" />
              <span>Tech Stacks you'll work with</span>
            </div>
            <div class="flex flex-wrap gap-2 pt-1">
              <span
                v-for="tech in job.tech_stacks"
                :key="tech"
                class="inline-flex items-center gap-1.5 px-3.5 py-1.5 rounded-xl text-xs font-semibold bg-white text-indigo-800 border border-indigo-100 shadow-2xs hover:border-indigo-300 transition-colors"
              >
                <span class="h-1.5 w-1.5 rounded-full bg-indigo-500"></span>
                {{ tech }}
              </span>
            </div>
          </div>

          <!-- About the Role / Full Description -->
          <div v-if="job.description" class="space-y-2">
            <h2 class="text-base font-bold text-gray-900">About the role</h2>
            <div class="rich-text-preview text-sm text-gray-700 leading-relaxed" v-html="job.description"></div>
          </div>

          <!-- Responsibilities -->
          <div v-if="job.responsibilities" class="space-y-2 pt-2 border-t border-gray-100">
            <h2 class="text-base font-bold text-gray-900">Responsibilities</h2>
            <div class="rich-text-preview text-sm text-gray-700 leading-relaxed" v-html="job.responsibilities"></div>
          </div>

          <!-- Requirements -->
          <div v-if="job.requirements" class="space-y-2 pt-2 border-t border-gray-100">
            <h2 class="text-base font-bold text-gray-900">Requirements</h2>
            <div class="rich-text-preview text-sm text-gray-700 leading-relaxed" v-html="job.requirements"></div>
          </div>

          <!-- Nice to Have -->
          <div v-if="job.nice_to_have" class="space-y-2 pt-2 border-t border-gray-100">
            <h2 class="text-base font-bold text-gray-900 flex items-center gap-1.5">
              <Check class="h-4 w-4 text-gray-700" /> Nice to have
            </h2>
            <div class="rich-text-preview text-sm text-gray-700 leading-relaxed" v-html="job.nice_to_have"></div>
          </div>

          <!-- What We Offer -->
          <div v-if="job.what_we_offer || job.benefits" class="space-y-2 pt-2 border-t border-gray-100">
            <h2 class="text-base font-bold text-gray-900 flex items-center gap-1.5">
              <Gift class="h-4 w-4 text-gray-700" /> What we offer
            </h2>
            <div
              class="rich-text-preview text-sm text-gray-700 leading-relaxed"
              v-html="job.what_we_offer || job.benefits"
            ></div>
          </div>

          <!-- HIRING PROCESS / SELECTION STAGES -->
          <div class="pt-4 border-t border-gray-100">
            <!-- OPTION A: Custom Hiring Instructions -->
            <div v-if="job.has_custom_hiring_process" class="bg-indigo-50/40 rounded-2xl p-6 border border-indigo-100 space-y-3">
              <h2 class="text-base font-bold text-gray-900 flex items-center gap-2">
                <CheckCircle2 class="h-5 w-5 text-indigo-600" /> How to Apply
              </h2>
              <p class="text-xs text-gray-500">
                This company manages their application process directly. Please follow the instructions below:
              </p>
              <div
                v-if="job.how_to_apply"
                class="rich-text-preview text-sm text-gray-800 leading-relaxed mt-2"
                v-html="job.how_to_apply"
              ></div>
              <p v-else class="text-sm text-gray-600">
                Please visit the company website or contact {{ job.organization?.name }} directly to submit your application.
              </p>
            </div>

            <!-- OPTION B: Platform Selection Stages -->
            <div v-else-if="job.selection_processes?.length && canViewSelectionProcess" class="space-y-4">
              <div class="flex items-center justify-between">
                <div>
                  <h2 class="text-base font-bold text-gray-900">Selection Process Stages</h2>
                  <p class="text-xs text-gray-500">Stages applicants will navigate through for this role.</p>
                </div>
                <span
                  v-if="!job.is_selection_process_public && isOrgStaffOrAdmin"
                  class="inline-flex items-center gap-1 px-2.5 py-1 text-xs font-semibold rounded-md bg-amber-50 text-amber-700 border border-amber-200"
                  title="Hidden from public applicants"
                >
                  <EyeOff class="h-3.5 w-3.5" /> Staff Only (Hidden from candidates)
                </span>
              </div>

              <ol class="space-y-3">
                <li
                  v-for="(p, idx) in job.selection_processes.sort((a, b) => a.order_index - b.order_index)"
                  :key="p.id || idx"
                  class="flex items-start gap-4 p-4 border border-gray-200 rounded-xl bg-gray-50/50 hover:bg-gray-50 transition shadow-2xs"
                >
                  <span class="flex h-7 w-7 items-center justify-center rounded-full bg-indigo-600 text-white text-xs font-bold shrink-0">
                    {{ idx + 1 }}
                  </span>
                  <div class="flex-1 min-w-0">
                    <div class="flex items-center justify-between gap-2">
                      <p class="font-semibold text-gray-900 text-sm">{{ p.title }}</p>
                      <span v-if="p.is_mandatory" class="text-[10px] uppercase font-bold text-indigo-600 bg-indigo-50 px-2 py-0.5 rounded">
                        Mandatory
                      </span>
                    </div>
                    <p class="text-xs text-gray-500 capitalize mt-0.5">
                      {{ p.process_type.replace('_', ' + ') }}
                      <span v-if="p.time_limit_minutes">• {{ p.time_limit_minutes }} mins</span>
                    </p>
                    <p v-if="p.description" class="text-xs text-gray-600 mt-1.5 leading-relaxed">
                      {{ p.description }}
                    </p>
                  </div>
                </li>
              </ol>
            </div>
          </div>

          <!-- Bottom Apply Section -->
          <div class="pt-6 border-t border-gray-100 flex flex-col sm:flex-row items-center justify-between gap-4">
            <div>
              <p class="text-sm font-semibold text-gray-900">Interested in this role?</p>
              <p class="text-xs text-gray-500">
                {{ job.has_custom_hiring_process ? 'Follow the instructions in "How to Apply" above.' : 'Submit your profile and advance through the evaluation stages.' }}
              </p>
            </div>

            <button
              type="button"
              class="w-full sm:w-auto px-8 py-3 text-sm font-semibold rounded-xl text-white bg-indigo-600 hover:bg-indigo-700 shadow-sm transition"
            >
              {{ job.has_custom_hiring_process ? 'Apply via Instructions' : 'Apply for this position' }}
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>

  <div v-else class="p-16 text-center text-gray-500">
    <Building2 class="h-12 w-12 text-gray-300 mx-auto mb-3" />
    <h2 class="text-lg font-bold text-gray-800">Job Not Found</h2>
    <p class="text-sm text-gray-500 mt-1">This job posting may have been paused, closed, or removed.</p>
  </div>
</template>
