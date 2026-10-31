<script setup lang="ts">
import { ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { memberService } from '@/services/member.service'
import { ArrowLeft, UserPlus, Mail, Briefcase, ShieldCheck } from 'lucide-vue-next'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()

const orgId = route.params.id as string
const email = ref('')
const title = ref('')
const role = ref<'org_staff'>('org_staff')
const loading = ref(false)
const errorMsg = ref('')
const successMsg = ref('')

async function handleInvite() {
  loading.value = true
  errorMsg.value = ''
  successMsg.value = ''

  try {
    const trimmedEmail = email.value.trim().toLowerCase()
    if (!trimmedEmail) {
      throw new Error('Please enter a valid email address.')
    }

    if (auth.user && auth.user.email.toLowerCase() === trimmedEmail) {
      throw new Error('You cannot invite yourself to the organization.')
    }

    await memberService.inviteMember({
      organizationId: orgId,
      email: trimmedEmail,
      role: role.value,
      title: title.value.trim() || null,
      inviterId: auth.user!.id,
    })

    successMsg.value = 'Invitation sent successfully! The user is now listed in the Team tab.'
    email.value = ''
    title.value = ''
  } catch (err: any) {
    errorMsg.value = err.message || 'Failed to send invitation. Please try again.'
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="min-h-screen bg-page py-6 sm:py-10 px-3 sm:px-6 lg:px-8">
    <div class="max-w-lg mx-auto space-y-6">
      
      <!-- Back Navigation & Header -->
      <div class="space-y-2">
        <button
          type="button"
          @click="router.push(`/organizations/${orgId}?tab=team`)"
          class="inline-flex items-center gap-1.5 text-xs sm:text-sm font-medium text-text-muted hover:text-text-strong transition focus:outline-none focus:ring-2 focus:ring-primary/25 rounded-md"
        >
          <ArrowLeft class="h-4 w-4" /> Back to company team
        </button>
        <h1 class="text-xl sm:text-2xl lg:text-3xl font-bold text-text-strong tracking-tight flex items-center gap-2">
          <UserPlus class="h-6 w-6 text-primary flex-shrink-0" /> Invite Team Member
        </h1>
        <p class="text-xs sm:text-sm text-text-secondary leading-relaxed">
          Invite an existing user to join your organization as staff.
        </p>
      </div>

      <!-- Main Form Card -->
      <form @submit.prevent="handleInvite" class="bg-card shadow-sm border border-border rounded-xl sm:rounded-2xl p-4 sm:p-6 lg:p-8 space-y-5">
        
        <!-- Error Alert -->
        <div v-if="errorMsg" class="alert alert-error p-3.5 sm:p-4 text-xs sm:text-sm flex items-start gap-2.5">
          <span class="leading-relaxed font-medium">{{ errorMsg }}</span>
        </div>
        
        <!-- Success Alert -->
        <div v-if="successMsg" class="alert alert-success p-4 text-xs sm:text-sm space-y-3">
          <p class="font-semibold leading-relaxed">{{ successMsg }}</p>
          <div class="pt-1">
            <button
              type="button"
              @click="router.push(`/organizations/${orgId}?tab=team`)"
              class="inline-flex items-center gap-1.5 px-3.5 py-2 border border-success-border text-xs font-semibold rounded-xl text-success-text bg-success-bg hover:bg-success/15 shadow-xs transition"
            >
              View Team Members →
            </button>
          </div>
        </div>

        <!-- Email Field -->
        <div>
          <label class="block text-xs sm:text-sm font-medium text-text-secondary mb-1.5">
            Email address <span class="text-danger">*</span>
          </label>
          <div class="relative">
            <input
              v-model="email"
              type="email"
              required
              class="pl-10"
              placeholder="colleague@company.com"
            />
            <Mail class="h-4 w-4 text-text-subtle absolute left-3.5 top-1/2 -translate-y-1/2 pointer-events-none" />
          </div>
        </div>

        <!-- Title / Position Field -->
        <div>
          <label class="block text-xs sm:text-sm font-medium text-text-secondary mb-1.5">
            Title / Position
          </label>
          <div class="relative">
            <input
              v-model="title"
              type="text"
              class="pl-10"
              placeholder="e.g. HR Manager, Technical Recruiter"
            />
            <Briefcase class="h-4 w-4 text-text-subtle absolute left-3.5 top-1/2 -translate-y-1/2 pointer-events-none" />
          </div>
        </div>

        <!-- Role Select Field -->
        <div>
          <label class="block text-xs sm:text-sm font-medium text-text-secondary mb-1.5">
            Role
          </label>
          <div class="relative">
            <select
              v-model="role"
              class="pl-10"
            >
              <option value="org_staff">Staff</option>
            </select>
            <ShieldCheck class="h-4 w-4 text-text-subtle absolute left-3.5 top-1/2 -translate-y-1/2 pointer-events-none" />
          </div>
        </div>

        <!-- Submit Button -->
        <div class="pt-2">
          <button
            type="submit"
            :disabled="loading"
            class="w-full btn btn-primary py-3 text-xs sm:text-sm font-semibold rounded-xl shadow-sm transition flex items-center justify-center gap-2"
          >
            <UserPlus class="h-4 w-4" />
            {{ loading ? 'Sending Invite...' : 'Send Invitation' }}
          </button>
        </div>
      </form>
    </div>
  </div>
</template>