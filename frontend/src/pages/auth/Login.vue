<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import AuthLayout from '@/layouts/AuthLayout.vue'

const router = useRouter()
const auth = useAuthStore()

const email = ref('')
const password = ref('')
const loading = ref(false)
const errorMsg = ref('')

async function handleLogin() {
  loading.value = true
  errorMsg.value = ''

  const { error } = await auth.signIn(email.value, password.value)

  if (error) {
    errorMsg.value = error.message
    loading.value = false
    return
  }

  // After login, check profile completeness
  if (auth.user && !auth.user.is_profile_complete) {
    router.push('/profile/complete')
  } else {
    router.push('/')
  }

  loading.value = false
}
</script>

<template>
  <AuthLayout title="Sign in to your account" subtitle="Welcome back">
    <form class="space-y-6" @submit.prevent="handleLogin">
      <div v-if="errorMsg" class="rounded-md bg-red-50 p-4 text-sm text-red-700">
        {{ errorMsg }}
      </div>

      <div>
        <label class="block text-sm font-medium text-gray-700">Email address</label>
        <input
          v-model="email"
          type="email"
          required
          class="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:border-indigo-500 focus:outline-none focus:ring-indigo-500 sm:text-sm"
        />
      </div>

      <div>
        <label class="block text-sm font-medium text-gray-700">Password</label>
        <input
          v-model="password"
          type="password"
          required
          class="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:border-indigo-500 focus:outline-none focus:ring-indigo-500 sm:text-sm"
        />
      </div>

      <div>
        <button
          type="submit"
          :disabled="loading"
          class="flex w-full justify-center rounded-md border border-transparent bg-indigo-600 py-2 px-4 text-sm font-medium text-white shadow-sm hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2 disabled:opacity-50"
        >
          {{ loading ? 'Signing in...' : 'Sign in' }}
        </button>
      </div>

      <div class="text-sm text-center">
        <router-link to="/register" class="font-medium text-indigo-600 hover:text-indigo-500">
          Don't have an account? Register
        </router-link>
      </div>
    </form>
  </AuthLayout>
</template>