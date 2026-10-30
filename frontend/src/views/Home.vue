<script setup lang="ts">
import { computed } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useOrganizationsStore } from '@/stores/organizations'
import {
  Building2,
  Briefcase,
  CheckCircle2,
  Clock,
  Mail,
  PlusCircle,
  Sparkles,
  User,
  ArrowRight,
  Shield,
} from 'lucide-vue-next'

const router = useRouter()
const auth = useAuthStore()
const orgStore = useOrganizationsStore()

const user = computed(() => auth.user)
const userRole = computed(() => auth.role || 'candidate')

const displayName = computed(() => {
  if (!user.value) return 'User'
  return (
    user.value.display_name ||
    `${user.value.first_name || ''} ${user.value.last_name || ''}`.trim() ||
    user.value.email
  )
})

const primaryOrg = computed(() => {
  return orgStore.myOrganizations.length > 0 ? orgStore.myOrganizations[0] : null
})
</script>

<template>
  <div class="py-8 px-4 sm:px-6 lg:px-8 max-w-7xl w-full mx-auto space-y-8">
    <!-- Welcome Banner with Brand Gradient -->
    <div
      class="bg-gradient-to-r from-indigo-600 to-indigo-800 dark:from-indigo-900 dark:to-slate-900 rounded-3xl shadow-sm p-8 text-white relative overflow-hidden border border-indigo-500/20"
    >
      <div class="relative z-10 max-w-3xl">
        <div class="flex items-center gap-2 mb-2">
          <span
            class="inline-flex items-center gap-1 px-3 py-1 rounded-full text-xs font-bold uppercase tracking-wider bg-white/20 text-white backdrop-blur-xs"
          >
            <Sparkles class="h-3 w-3" />
            {{ userRole.replace('_', ' ') }}
          </span>
        </div>

        <h1 class="text-3xl sm:text-4xl font-extrabold tracking-tight">
          Welcome back, {{ displayName }}! 👋
        </h1>
        <p class="mt-2 text-indigo-100 text-sm sm:text-base leading-relaxed">
          {{
            user?.headline ||
            'Explore opportunities, manage team operations, and streamline the recruitment pathway.'
          }}
        </p>

        <!-- Role-based Action Buttons -->
        <div class="mt-6 flex flex-wrap gap-3">
          <router-link
            to="/profile/complete"
            class="inline-flex items-center gap-2 px-4 py-2 rounded-xl bg-white text-indigo-700 font-semibold text-xs sm:text-sm shadow-xs hover:bg-indigo-50 transition"
          >
            <User class="h-4 w-4" />
            Profile Settings
          </router-link>

          <router-link
            v-if="primaryOrg"
            :to="`/organizations/${primaryOrg.id}`"
            class="inline-flex items-center gap-2 px-4 py-2 rounded-xl bg-white/15 hover:bg-white/25 text-white font-semibold text-xs sm:text-sm backdrop-blur-xs border border-white/20 transition"
          >
            <Building2 class="h-4 w-4" />
            {{ primaryOrg.name }}
          </router-link>

          <router-link
            v-if="primaryOrg && (userRole === 'org_admin' || userRole === 'org_staff')"
            :to="`/organizations/${primaryOrg.id}/jobs/create`"
            class="inline-flex items-center gap-2 px-4 py-2 rounded-xl bg-white/15 hover:bg-white/25 text-white font-semibold text-xs sm:text-sm backdrop-blur-xs border border-white/20 transition"
          >
            <PlusCircle class="h-4 w-4" />
            Post a Job
          </router-link>

          <router-link
            v-if="!primaryOrg && userRole === 'candidate'"
            to="/organizations/create"
            class="inline-flex items-center gap-2 px-4 py-2 rounded-xl bg-white/15 hover:bg-white/25 text-white font-semibold text-xs sm:text-sm backdrop-blur-xs border border-white/20 transition"
          >
            <Building2 class="h-4 w-4" />
            Register Company
          </router-link>
        </div>
      </div>
    </div>

    <!-- Quick Stats & Status Cards -->
    <div class="grid grid-cols-1 md:grid-cols-3 gap-6">
      <!-- Profile Card -->
      <div
        class="dark:bg-slate-900 p-6 rounded-2xl shadow-2xs border dark:border-slate-800 space-y-2"
      >
        <div class="text-xs font-bold uppercase tracking-wider dark:text-gray-400">
          Profile Readiness
        </div>
        <div class="flex items-center justify-between">
          <span class="text-xl font-bold dark:text-white">
            {{ user?.is_profile_complete ? 'Complete' : 'Incomplete' }}
          </span>
          <span
            class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold"
            :class="
              user?.is_profile_complete
                ? 'dark:bg-emerald-950/60 dark:text-emerald-300'
                : 'dark:bg-amber-950/60 dark:text-amber-300'
            "
          >
            {{ user?.is_profile_complete ? 'Active' : 'Pending Details' }}
          </span>
        </div>
        <p class="text-xs dark:text-gray-400">
          {{
            user?.is_profile_complete
              ? 'Your profile is visible to recruiters with up-to-date career info.'
              : 'Add your headline, job title, and location to boost your visibility.'
          }}
        </p>
      </div>

      <!-- Account Role Card -->
      <div
        class="dark:bg-slate-900 p-6 rounded-2xl shadow-2xs border dark:border-slate-800 space-y-2"
      >
        <div class="text-xs font-bold uppercase tracking-wider dark:text-gray-400">
          Account Role
        </div>
        <div class="flex items-center justify-between">
          <span class="text-xl font-bold dark:text-white capitalize">
            {{ userRole.replace('_', ' ') }}
          </span>
          <Shield class="h-5 w-5 dark:text-indigo-400" />
        </div>
        <p class="text-xs dark:text-gray-400 truncate">
          Signed in as {{ user?.email }}
        </p>
      </div>

      <!-- Organization / Company Card -->
      <div
        class="dark:bg-slate-900 p-6 rounded-2xl shadow-2xs border dark:border-slate-800 space-y-2"
      >
        <div class="text-xs font-bold uppercase tracking-wider text-gray-500">
          Organization Affiliation
        </div>
        <div class="flex items-center justify-between">
          <span class="text-xl font-bold dark:text-white truncate">
            {{ primaryOrg ? primaryOrg.name : 'Independent' }}
          </span>
          <Building2 class="h-5 w-5 dark:text-indigo-400" />
        </div>
        <p class="text-xs dark:text-gray-400">
          <router-link
            v-if="primaryOrg"
            :to="`/organizations/${primaryOrg.id}`"
            class="dark:text-indigo-400 font-semibold hover:underline inline-flex items-center gap-1"
          >
            Go to Company Workspace
            <ArrowRight class="h-3 w-3" />
          </router-link>
          <router-link
            v-else
            to="/organizations/create"
            class="dark:text-indigo-400 font-semibold hover:underline inline-flex items-center gap-1"
          >
            Create Company Profile
            <ArrowRight class="h-3 w-3" />
          </router-link>
        </p>
      </div>
    </div>
  </div>
</template>
