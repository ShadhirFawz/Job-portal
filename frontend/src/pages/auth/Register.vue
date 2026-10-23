<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import AuthLayout from '@/layouts/AuthLayout.vue'

const router = useRouter()
const auth = useAuthStore()

const firstName = ref('')
const lastName = ref('')
const email = ref('')
const password = ref('')
const loading = ref(false)
const errorMsg = ref('')
const successMsg = ref('')

async function handleRegister() {
  loading.value = true
  errorMsg.value = ''
  successMsg.value = ''

  const { error } = await auth.signUp(email.value, password.value, {
    first_name: firstName.value,
    last_name: lastName.value
  })

  if (error) {
    errorMsg.value = error.message
    loading.value = false
    return
  }

  successMsg.value = 'Registration successful! Please check your email to verify your account.'
  loading.value = false
}
</script>

<template>
  <AuthLayout title="Create your account" subtitle="Join the platform">
    <form class="space-y-6" @submit.prevent="handleRegister">
      <div v-if="errorMsg" class="rounded-md bg-red-50 p-4 text-sm text-red-700">
        {{ errorMsg }}
      </div>
      <div v-if="successMsg" class="rounded-md bg-green-50 p-4 text-sm text-green-700">
        {{ successMsg }}
      </div>

      <div class="grid grid-cols-2 gap-4">
        <div>
          <label class="block text-sm font-medium text-gray-700">First name</label>
          <input v-model="firstName" type="text" required
            class="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:border-indigo-500 focus:outline-none focus:ring-indigo-500 sm:text-sm" />
        </div>
        <div>
          <label class="block text-sm font-medium text-gray-700">Last name</label>
          <input v-model="lastName" type="text" required
            class="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:border-indigo-500 focus:outline-none focus:ring-indigo-500 sm:text-sm" />
        </div>
      </div>

      <div>
        <label class="block text-sm font-medium text-gray-700">Email address</label>
        <input v-model="email" type="email" required
          class="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:border-indigo-500 focus:outline-none focus:ring-indigo-500 sm:text-sm" />
      </div>

      <div>
        <label class="block text-sm font-medium text-gray-700">Password</label>
        <input v-model="password" type="password" required minlength="8"
          class="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:border-indigo-500 focus:outline-none focus:ring-indigo-500 sm:text-sm" />
      </div>

      <div>
        <button type="submit" :disabled="loading"
          class="flex w-full justify-center rounded-md border border-transparent bg-indigo-600 py-2 px-4 text-sm font-medium text-white shadow-sm hover:bg-indigo-700 disabled:opacity-50">
          {{ loading ? 'Creating account...' : 'Create account' }}
        </button>
      </div>

      <div class="text-sm text-center">
        <router-link to="/login" class="font-medium text-indigo-600 hover:text-indigo-500">
          Already have an account? Sign in
        </router-link>
      </div>
    </form>
  </AuthLayout>
</template>