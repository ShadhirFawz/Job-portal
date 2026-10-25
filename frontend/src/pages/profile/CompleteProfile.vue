<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { supabase } from '@/lib/supabase'
import { uploadProfileImage, getPublicUrl } from '@/composables/useProfileImage'

const router = useRouter()
const auth = useAuthStore()

const loading = ref(false)
const errorMsg = ref('')
const successMsg = ref('')

// Form fields
const firstName = ref('')
const lastName = ref('')
const displayName = ref('')
const phone = ref('')
const headline = ref('')
const currentJobTitle = ref('')
const currentCompany = ref('')
const bio = ref('')
const countryCode = ref('')
const city = ref('')

// Images
const avatarFile = ref<File | null>(null)
const coverFile = ref<File | null>(null)
const avatarPreview = ref<string | null>(null)
const coverPreview = ref<string | null>(null)

const coverUrl = computed(() => {
  return coverPreview.value || (auth.user?.cover_path ? getPublicUrl(auth.user.cover_path) : undefined)
})

const avatarUrl = computed(() => {
  return avatarPreview.value || (auth.user?.avatar_path ? getPublicUrl(auth.user.avatar_path) : undefined)
})

function populateFields(user: any) {
  if (!user) return
  if (!firstName.value && user.first_name) firstName.value = user.first_name
  if (!lastName.value && user.last_name) lastName.value = user.last_name
  if (!displayName.value && user.display_name) displayName.value = user.display_name
  if (!phone.value && user.phone) phone.value = user.phone
  if (!headline.value && user.headline) headline.value = user.headline
  if (!currentJobTitle.value && user.current_job_title) currentJobTitle.value = user.current_job_title
  if (!currentCompany.value && user.current_company) currentCompany.value = user.current_company
  if (!bio.value && user.bio) bio.value = user.bio
  if (!countryCode.value && user.country_code) countryCode.value = user.country_code
  if (!city.value && user.city) city.value = user.city
}

watch(
  () => auth.user,
  (newUser) => {
    populateFields(newUser)
  },
  { immediate: true }
)

onMounted(async () => {
  if (!auth.user) {
    await auth.init()
  }
  populateFields(auth.user)
})

function onAvatarChange(e: Event) {
  const target = e.target as HTMLInputElement
  if (target.files?.[0]) {
    avatarFile.value = target.files[0]
    avatarPreview.value = URL.createObjectURL(target.files[0])
  }
}

function onCoverChange(e: Event) {
  const target = e.target as HTMLInputElement
  if (target.files?.[0]) {
    coverFile.value = target.files[0]
    coverPreview.value = URL.createObjectURL(target.files[0])
  }
}

async function handleSubmit() {
  if (loading.value) return

  errorMsg.value = ''
  successMsg.value = ''

  // Validate required fields
  if (!firstName.value.trim() || !lastName.value.trim()) {
    errorMsg.value = 'Please enter both your first name and last name.'
    return
  }

  loading.value = true

  try {
    // 1. Resolve user ID from Pinia store, session, or direct Supabase call
    let userId = auth.user?.id || auth.session?.user?.id
    let userEmail = auth.user?.email || auth.session?.user?.email

    if (!userId) {
      const { data: sessionData } = await supabase.auth.getSession()
      userId = sessionData.session?.user?.id
      userEmail = sessionData.session?.user?.email
    }

    if (!userId) {
      throw new Error('Your session has expired or no authenticated user was found. Please log in again.')
    }

    let avatarPath = auth.user?.avatar_path || null
    let coverPath = auth.user?.cover_path || null

    // 2. Upload avatar
    if (avatarFile.value) {
      const path = await uploadProfileImage(userId, avatarFile.value, 'avatar')
      if (!path) throw new Error('Failed to upload avatar. Please verify storage permissions.')
      avatarPath = path
    }

    // 3. Upload cover
    if (coverFile.value) {
      const path = await uploadProfileImage(userId, coverFile.value, 'cover')
      if (!path) throw new Error('Failed to upload cover. Please verify storage permissions.')
      coverPath = path
    }

    // 4. Upsert user profile
    const profilePayload = {
      id: userId,
      email: userEmail || '',
      first_name: firstName.value.trim(),
      last_name: lastName.value.trim(),
      display_name: displayName.value.trim() || `${firstName.value} ${lastName.value}`.trim(),
      phone: phone.value.trim() || null,
      headline: headline.value.trim() || null,
      current_job_title: currentJobTitle.value.trim() || null,
      current_company: currentCompany.value.trim() || null,
      bio: bio.value.trim() || null,
      country_code: countryCode.value.trim() || null,
      city: city.value.trim() || null,
      avatar_path: avatarPath,
      cover_path: coverPath,
      is_profile_complete: true,
      updated_at: new Date().toISOString()
    }

    const { error } = await supabase
      .from('users')
      .upsert(profilePayload, { onConflict: 'id' })

    if (error) throw error

    // 5. Refresh local user data in auth store
    await auth.fetchUserProfile()

    successMsg.value = 'Profile completed successfully! Redirecting...'
    
    // Redirect after short delay
    setTimeout(() => {
      router.push('/')
    }, 1000)

  } catch (err: any) {
    console.error('Error submitting profile:', err)
    errorMsg.value = err.message || 'Something went wrong while saving your profile.'
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="min-h-screen bg-gray-50 py-10 px-4 sm:px-6 lg:px-8">
    <div class="max-w-3xl mx-auto">
      <div class="bg-white shadow rounded-lg overflow-hidden">
        <!-- Cover Preview -->
        <div class="h-48 bg-gradient-to-r from-indigo-500 to-purple-600 relative">
          <img
            v-if="coverUrl"
            :src="coverUrl"
            class="w-full h-full object-cover"
          />
          <label class="absolute bottom-4 right-4 bg-white/90 hover:bg-white text-sm px-3 py-1.5 rounded cursor-pointer shadow">
            Change Cover
            <input type="file" accept="image/*" class="hidden" @change="onCoverChange" />
          </label>
        </div>

        <!-- Avatar -->
        <div class="px-6 -mt-12 flex items-end space-x-5">
          <div class="relative">
            <div class="h-24 w-24 rounded-full ring-4 ring-white bg-gray-200 overflow-hidden">
              <img
                v-if="avatarUrl"
                :src="avatarUrl"
                class="h-full w-full object-cover"
              />
              <div v-else class="h-full w-full flex items-center justify-center text-gray-400 text-2xl">
                {{ firstName.charAt(0) || '?' }}
              </div>
            </div>
            <label class="absolute bottom-0 right-0 bg-indigo-600 text-white p-1.5 rounded-full cursor-pointer shadow">
              <svg xmlns="http://www.w3.org/2000/svg" class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor">
                <path d="M13.586 3.586a2 2 0 112.828 2.828l-.793.793-2.828-2.828.793-.793zM11.379 5.793L3 14.172V17h2.828l8.38-8.379-2.83-2.828z" />
              </svg>
              <input type="file" accept="image/*" class="hidden" @change="onAvatarChange" />
            </label>
          </div>
          <div class="pb-2">
            <h1 class="text-2xl font-bold text-gray-900">Complete your profile</h1>
            <p class="text-sm text-gray-500">This information will be visible to companies</p>
          </div>
        </div>

        <!-- Form -->
        <form novalidate class="px-6 py-8 space-y-6" @submit.prevent="handleSubmit">
          <div v-if="errorMsg" class="rounded-md bg-red-50 p-4 text-sm text-red-700">
            {{ errorMsg }}
          </div>
          <div v-if="successMsg" class="rounded-md bg-green-50 p-4 text-sm text-green-700">
            {{ successMsg }}
          </div>

          <div class="grid grid-cols-1 sm:grid-cols-2 gap-6">
            <div>
              <label class="block text-sm font-medium text-gray-700">First name *</label>
              <input v-model="firstName" required type="text"
                class="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm" />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700">Last name *</label>
              <input v-model="lastName" required type="text"
                class="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm" />
            </div>
          </div>

          <div>
            <label class="block text-sm font-medium text-gray-700">Display name</label>
            <input v-model="displayName" type="text" placeholder="How your name appears publicly"
              class="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm" />
          </div>

          <div>
            <label class="block text-sm font-medium text-gray-700">Phone</label>
            <input v-model="phone" type="tel"
              class="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm" />
          </div>

          <div>
            <label class="block text-sm font-medium text-gray-700">Professional Headline</label>
            <input v-model="headline" type="text" placeholder="e.g. Full Stack Developer | Open to opportunities"
              class="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm" />
          </div>

          <div class="grid grid-cols-1 sm:grid-cols-2 gap-6">
            <div>
              <label class="block text-sm font-medium text-gray-700">Current Job Title</label>
              <input v-model="currentJobTitle" type="text"
                class="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm" />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700">Current Company</label>
              <input v-model="currentCompany" type="text"
                class="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm" />
            </div>
          </div>

          <div class="grid grid-cols-1 sm:grid-cols-2 gap-6">
            <div>
              <label class="block text-sm font-medium text-gray-700">Country Code (ISO)</label>
              <input v-model="countryCode" type="text" maxlength="2" placeholder="IN, US, GB..."
                class="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm" />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700">City</label>
              <input v-model="city" type="text"
                class="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm" />
            </div>
          </div>

          <div>
            <label class="block text-sm font-medium text-gray-700">Bio</label>
            <textarea v-model="bio" rows="4"
              class="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
              placeholder="Tell companies a bit about yourself..."></textarea>
          </div>

          <!-- Bottom feedback alerts -->
          <div v-if="errorMsg" class="rounded-md bg-red-50 p-4 text-sm text-red-700">
            {{ errorMsg }}
          </div>
          <div v-if="successMsg" class="rounded-md bg-green-50 p-4 text-sm text-green-700">
            {{ successMsg }}
          </div>

          <div class="pt-2">
            <button
              type="submit"
              :disabled="loading"
              @click="handleSubmit"
              class="w-full flex items-center justify-center py-3 px-4 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-indigo-600 hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 disabled:opacity-50 cursor-pointer transition"
            >
              <svg v-if="loading" class="animate-spin -ml-1 mr-3 h-5 w-5 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8H4z"></path>
              </svg>
              {{ loading ? 'Saving profile...' : 'Complete Profile & Continue' }}
            </button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>