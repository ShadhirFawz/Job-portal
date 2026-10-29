<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { supabase } from '@/lib/supabase'

const route = useRoute()
const router = useRouter()

const status = ref<'loading' | 'success' | 'error' | 'expired'>('loading')
const message = ref('')
const orgName = ref('')

onMounted(async () => {
  const token = route.query.token as string

  if (!token) {
    status.value = 'error'
    message.value = 'Invalid verification link'
    return
  }

  try {
    // 1. Find the token
    const { data: verification, error } = await supabase
      .from('organization_email_verifications')
      .select('*, organization:organizations(id, name, official_email)')
      .eq('token', token)
      .is('verified_at', null)
      .single()

    if (error || !verification) {
      status.value = 'error'
      message.value = 'Invalid or already used verification link'
      return
    }

    // 2. Check expiry
    if (new Date(verification.expires_at) < new Date()) {
      status.value = 'expired'
      message.value = 'This verification link has expired. Please request a new one.'
      return
    }

    // 3. Mark as verified
    const { error: updateTokenError } = await supabase
      .from('organization_email_verifications')
      .update({ verified_at: new Date().toISOString() })
      .eq('id', verification.id)

    if (updateTokenError) throw updateTokenError

    // 4. Update organization
    const { error: orgError } = await supabase
      .from('organizations')
      .update({
        official_email_verified: true,
        status: 'active',
        verified_at: new Date().toISOString()
      })
      .eq('id', verification.organization_id)

    if (orgError) throw orgError

    orgName.value = verification.organization?.name || 'Organization'
    status.value = 'success'
    message.value = 'Official email verified successfully! Your company is now active.'

  } catch (err: any) {
    status.value = 'error'
    message.value = err.message || 'Verification failed'
  }
})
</script>

<template>
  <div class="min-h-screen bg-gray-50 flex items-center justify-center px-4">
    <div class="max-w-md w-full bg-white rounded-lg shadow p-8 text-center">
      <div v-if="status === 'loading'">
        <div class="animate-spin rounded-full h-10 w-10 border-b-2 border-indigo-600 mx-auto"></div>
        <p class="mt-4 text-gray-600">Verifying email...</p>
      </div>

      <div v-else-if="status === 'success'">
        <div class="mx-auto flex items-center justify-center h-12 w-12 rounded-full bg-green-100">
          <svg class="h-6 w-6 text-green-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
          </svg>
        </div>
        <h2 class="mt-4 text-xl font-bold text-gray-900">Email Verified!</h2>
        <p class="mt-2 text-gray-600">{{ message }}</p>
        <p class="mt-1 text-sm text-gray-500">{{ orgName }} is now active.</p>
        <button
          @click="router.push('/')"
          class="mt-6 inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded-md text-white bg-indigo-600 hover:bg-indigo-700"
        >
          Go to Dashboard
        </button>
      </div>

      <div v-else>
        <div class="mx-auto flex items-center justify-center h-12 w-12 rounded-full bg-red-100">
          <svg class="h-6 w-6 text-red-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
          </svg>
        </div>
        <h2 class="mt-4 text-xl font-bold text-gray-900">
          {{ status === 'expired' ? 'Link Expired' : 'Verification Failed' }}
        </h2>
        <p class="mt-2 text-gray-600">{{ message }}</p>
        <button
          @click="router.push('/')"
          class="mt-6 inline-flex items-center px-4 py-2 border border-gray-300 text-sm font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50"
        >
          Back to Home
        </button>
      </div>
    </div>
  </div>
</template>