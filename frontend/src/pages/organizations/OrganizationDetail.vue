<script setup lang="ts">
import { onMounted, computed } from 'vue'
import { useRoute } from 'vue-router'
import { useOrganizationsStore } from '@/stores/organizations'
import { getOrganizationImageUrl } from '@/composables/useOrganizationImage'

const route = useRoute()
const orgStore = useOrganizationsStore()

const org = computed(() => orgStore.currentOrganization)

onMounted(() => {
  const id = route.params.id as string
  orgStore.fetchOrganization(id)
})
</script>

<template>
  <div v-if="orgStore.loading" class="p-10 text-center">Loading...</div>

  <div v-else-if="org" class="min-h-screen bg-gray-50">
    <!-- Cover -->
    <div class="h-48 bg-linear-to-r from-indigo-500 to-purple-600 relative">
      <img
        v-if="org.cover_path"
        :src="getOrganizationImageUrl(org.cover_path) || ''"
        class="w-full h-full object-cover"
      />
    </div>

    <div class="max-w-5xl mx-auto px-4 -mt-16">
      <div class="bg-white rounded-lg shadow p-6 flex items-start gap-6">
        <div class="h-24 w-24 rounded-full ring-4 ring-white bg-gray-200 overflow-hidden shrink-0">
          <img
            v-if="org.avatar_path"
            :src="getOrganizationImageUrl(org.avatar_path) || ''"
            class="h-full w-full object-cover"
          />
        </div>

        <div class="flex-1">
          <div class="flex items-start justify-between">
            <div>
              <h1 class="text-2xl font-bold text-gray-900">{{ org.name }}</h1>
              <p v-if="org.tagline" class="text-gray-600">{{ org.tagline }}</p>
              <div class="mt-2 flex items-center gap-3 text-sm text-gray-500">
                <span v-if="org.industry">{{ org.industry }}</span>
                <span v-if="org.company_size">• {{ org.company_size }} employees</span>
                <span v-if="org.city">• {{ org.city }}</span>
              </div>
            </div>
            <span
              class="px-3 py-1 text-xs font-medium rounded-full"
              :class="org.status === 'active' ? 'bg-green-100 text-green-800' : 'bg-yellow-100 text-yellow-800'"
            >
              {{ org.status }}
            </span>
          </div>

          <p v-if="org.description" class="mt-4 text-gray-700">{{ org.description }}</p>
        </div>
      </div>

      <!-- Simple tabs placeholder -->
      <div class="mt-6 bg-white rounded-lg shadow p-6">
        <h2 class="text-lg font-semibold mb-4">Company Dashboard</h2>
        <p class="text-gray-600">
          Job postings, team members, and selection processes will appear here in the next steps.
        </p>
      </div>
    </div>
  </div>

  <div v-else class="p-10 text-center text-red-600">
    Organization not found
  </div>
</template>