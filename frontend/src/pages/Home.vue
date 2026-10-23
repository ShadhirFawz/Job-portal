<script setup lang="ts">
import { computed } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { getPublicUrl } from '@/composables/useProfileImage'

const router = useRouter()
const auth = useAuthStore()

const user = computed(() => auth.user)
const userAvatar = computed(() => {
  return user.value?.avatar_path ? getPublicUrl(user.value.avatar_path) : undefined
})

const displayName = computed(() => {
  if (!user.value) return 'User'
  return user.value.display_name || `${user.value.first_name || ''} ${user.value.last_name || ''}`.trim() || user.value.email
})

async function handleSignOut() {
  await auth.signOut()
  router.push('/login')
}
</script>

<template>
  <div class="min-h-screen bg-gray-50 flex flex-col">
    <!-- Navbar -->
    <header class="bg-white shadow-xs border-b border-gray-200">
      <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div class="flex justify-between h-16 items-center">
          <div class="flex items-center space-x-3">
            <div class="h-9 w-9 rounded-lg bg-indigo-600 flex items-center justify-center text-white font-bold text-lg shadow-sm">
              RP
            </div>
            <span class="text-xl font-bold text-gray-900 tracking-tight">Recruitment Platform</span>
          </div>

          <div class="flex items-center space-x-4">
            <router-link
              to="/profile/complete"
              class="text-sm font-medium text-gray-700 hover:text-indigo-600 transition"
            >
              Edit Profile
            </router-link>

            <div class="flex items-center space-x-3 pl-3 border-l border-gray-200">
              <div class="h-9 w-9 rounded-full bg-gray-200 overflow-hidden ring-2 ring-indigo-500/20">
                <img
                  v-if="userAvatar"
                  :src="userAvatar"
                  alt="Avatar"
                  class="h-full w-full object-cover"
                />
                <div v-else class="h-full w-full flex items-center justify-center text-gray-500 font-semibold text-sm">
                  {{ displayName.charAt(0).toUpperCase() }}
                </div>
              </div>
              <span class="text-sm font-medium text-gray-800 hidden sm:inline-block">
                {{ displayName }}
              </span>
            </div>

            <button
              @click="handleSignOut"
              class="text-sm px-3.5 py-1.5 rounded-md font-medium text-gray-700 bg-gray-100 hover:bg-gray-200 transition"
            >
              Sign out
            </button>
          </div>
        </div>
      </div>
    </header>

    <!-- Main Content -->
    <main class="flex-1 max-w-7xl w-full mx-auto px-4 sm:px-6 lg:px-8 py-8">
      <!-- Welcome Header -->
      <div class="bg-gradient-to-r from-indigo-600 to-purple-600 rounded-2xl shadow-md p-8 text-white mb-8">
        <h1 class="text-3xl font-extrabold tracking-tight">
          Welcome back, {{ displayName }}! 👋
        </h1>
        <p class="mt-2 text-indigo-100 max-w-2xl text-base">
          {{ user?.headline || 'Explore opportunities, manage applications, and connect with top teams.' }}
        </p>

        <div class="mt-6 flex flex-wrap gap-3">
          <router-link
            to="/profile/complete"
            class="inline-flex items-center px-4 py-2 rounded-lg bg-white text-indigo-700 font-semibold text-sm shadow hover:bg-indigo-50 transition"
          >
            Update Profile
          </router-link>
        </div>
      </div>

      <!-- Quick Stats / Cards -->
      <div class="grid grid-cols-1 md:grid-cols-3 gap-6">
        <div class="bg-white p-6 rounded-xl shadow-xs border border-gray-200">
          <div class="text-xs font-semibold uppercase tracking-wider text-gray-500">Profile Status</div>
          <div class="mt-2 flex items-center justify-between">
            <span class="text-lg font-bold text-gray-900">
              {{ user?.is_profile_complete ? 'Complete' : 'Incomplete' }}
            </span>
            <span
              class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium"
              :class="user?.is_profile_complete ? 'bg-green-100 text-green-800' : 'bg-yellow-100 text-yellow-800'"
            >
              {{ user?.is_profile_complete ? 'Active' : 'Action Needed' }}
            </span>
          </div>
          <p class="mt-2 text-xs text-gray-500">
            {{ user?.is_profile_complete ? 'Your profile is ready and visible to recruiters.' : 'Complete your details to increase visibility.' }}
          </p>
        </div>

        <div class="bg-white p-6 rounded-xl shadow-xs border border-gray-200">
          <div class="text-xs font-semibold uppercase tracking-wider text-gray-500">Account Role</div>
          <div class="mt-2 text-lg font-bold text-gray-900 capitalize">
            {{ user?.role ? user.role.replace('_', ' ') : 'Candidate' }}
          </div>
          <p class="mt-2 text-xs text-gray-500">Registered with {{ user?.email }}</p>
        </div>

        <div class="bg-white p-6 rounded-xl shadow-xs border border-gray-200">
          <div class="text-xs font-semibold uppercase tracking-wider text-gray-500">Location</div>
          <div class="mt-2 text-lg font-bold text-gray-900">
            {{ user?.city ? `${user.city}${user.country_code ? ', ' + user.country_code : ''}` : 'Not specified' }}
          </div>
          <p class="mt-2 text-xs text-gray-500">Location helps match with local jobs</p>
        </div>
      </div>
    </main>
  </div>
</template>
