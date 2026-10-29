<script setup lang="ts">
import { onMounted, computed } from 'vue'
import { useRoute } from 'vue-router'
import { useJobsStore } from '@/stores/jobs'
import { getOrganizationImageUrl } from '@/composables/useOrganizationImage'

const route = useRoute()
const jobsStore = useJobsStore()

const job = computed(() => jobsStore.currentJob)

onMounted(() => {
  jobsStore.fetchPublicJob(route.params.id as string)
})
</script>

<template>
  <div v-if="jobsStore.loading" class="p-10 text-center">Loading job...</div>

  <div v-else-if="job" class="min-h-screen bg-gray-50 py-10 px-4">
    <div class="max-w-4xl mx-auto">
      <div class="bg-white rounded-lg shadow overflow-hidden">
        <div class="p-6 sm:p-8">
          <div class="flex items-start gap-4">
            <div class="h-14 w-14 rounded-lg bg-gray-100 overflow-hidden shrink-0">
              <img
                v-if="job.organization?.avatar_path"
                :src="getOrganizationImageUrl(job.organization.avatar_path) || ''"
                class="h-full w-full object-cover"
              />
            </div>
            <div>
              <h1 class="text-2xl font-bold text-gray-900">{{ job.title }}</h1>
              <p class="text-gray-600 mt-1">
                {{ job.organization?.name }}
                <span v-if="job.city"> • {{ job.city }}</span>
              </p>
            </div>
          </div>

          <div class="mt-6 flex flex-wrap gap-3 text-sm">
            <span class="px-3 py-1 bg-gray-100 rounded-full capitalize">{{ job.job_type.replace('_', ' ') }}</span>
            <span class="px-3 py-1 bg-gray-100 rounded-full capitalize">{{ job.work_mode }}</span>
            <span v-if="job.experience_level" class="px-3 py-1 bg-gray-100 rounded-full capitalize">{{ job.experience_level }}</span>
            <span v-if="job.show_salary && job.salary_min" class="px-3 py-1 bg-green-50 text-green-800 rounded-full">
              {{ job.salary_currency }} {{ job.salary_min }}{{ job.salary_max ? ' - ' + job.salary_max : '+' }}
            </span>
          </div>

          <div class="mt-8 prose max-w-none">
            <h3 class="text-lg font-semibold">About the role</h3>
            <p class="text-gray-700 whitespace-pre-line">{{ job.description }}</p>

            <div v-if="job.responsibilities" class="mt-6">
              <h3 class="text-lg font-semibold">Responsibilities</h3>
              <p class="text-gray-700 whitespace-pre-line">{{ job.responsibilities }}</p>
            </div>

            <div v-if="job.requirements" class="mt-6">
              <h3 class="text-lg font-semibold">Requirements</h3>
              <p class="text-gray-700 whitespace-pre-line">{{ job.requirements }}</p>
            </div>
          </div>

          <!-- Selection Processes -->
          <div v-if="job.selection_processes?.length" class="mt-10">
            <h3 class="text-lg font-semibold mb-4">Selection Process</h3>
            <ol class="space-y-3">
              <li
                v-for="(p, idx) in job.selection_processes.sort((a,b) => a.order_index - b.order_index)"
                :key="p.id"
                class="flex items-start gap-3 p-4 border rounded-lg"
              >
                <span class="flex h-7 w-7 items-center justify-center rounded-full bg-indigo-100 text-indigo-700 text-sm font-medium">
                  {{ idx + 1 }}
                </span>
                <div>
                  <p class="font-medium text-gray-900">{{ p.title }}</p>
                  <p class="text-sm text-gray-500 capitalize">{{ p.process_type.replace('_', ' + ') }}</p>
                  <p v-if="p.description" class="text-sm text-gray-600 mt-1">{{ p.description }}</p>
                </div>
              </li>
            </ol>
          </div>

          <div class="mt-10 pt-6 border-t">
            <button class="w-full sm:w-auto px-8 py-3 text-sm font-medium rounded-md text-white bg-indigo-600 hover:bg-indigo-700">
              Apply for this position
            </button>
            <p class="mt-2 text-xs text-gray-500">Application form will be implemented next.</p>
          </div>
        </div>
      </div>
    </div>
  </div>

  <div v-else class="p-10 text-center text-red-600">
    Job not found or not published.
  </div>
</template>