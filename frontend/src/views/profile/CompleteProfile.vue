<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { profileService } from '@/services/profile.service'
import { uploadProfileImage, deleteProfileImage, getProfileImageUrl } from '@/composables/useProfileImage'
import type { UsernameCheckResult } from '@/types/user'
import {
  User,
  Camera,
  CheckCircle2,
  XCircle,
  Loader2,
  AtSign,
  ArrowRight
} from 'lucide-vue-next'

const router = useRouter()
const auth = useAuthStore()

const loading = ref(false)
const errorMsg = ref('')
const successMsg = ref('')

// Form fields
const firstName = ref('')
const lastName = ref('')
const displayName = ref('')
const username = ref('')
const phone = ref('')
const headline = ref('')
const currentJobTitle = ref('')
const currentCompany = ref('')
const bio = ref('')
const countryCode = ref('')
const city = ref('')

// Username validation state
const usernameStatus = ref<'idle' | 'checking' | 'valid' | 'invalid'>('idle')
const usernameFeedback = ref<string>('Pick a unique username for your public profile (/u/username).')
let usernameDebounceTimer: ReturnType<typeof setTimeout> | null = null

// Images
const avatarFile = ref<File | null>(null)
const coverFile = ref<File | null>(null)
const avatarPreview = ref<string | null>(null)
const coverPreview = ref<string | null>(null)

const coverUrl = computed(() => {
  return coverPreview.value || (auth.user?.cover_path ? getProfileImageUrl(auth.user.cover_path) : undefined)
})

const avatarUrl = computed(() => {
  return avatarPreview.value || (auth.user?.avatar_path ? getProfileImageUrl(auth.user.avatar_path) : undefined)
})

function checkRedirectIfAlreadyCompleted(user: any) {
  if (user && user.is_profile_complete) {
    router.replace('/profile')
  }
}

function populateFields(user: any) {
  if (!user) return
  checkRedirectIfAlreadyCompleted(user)
  if (!firstName.value && user.first_name) firstName.value = user.first_name
  if (!lastName.value && user.last_name) lastName.value = user.last_name
  if (!displayName.value && user.display_name) displayName.value = user.display_name
  if (!username.value && user.username) username.value = user.username
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

// Debounced live username check
watch(username, (val) => {
  if (usernameDebounceTimer) {
    clearTimeout(usernameDebounceTimer)
  }

  const clean = val.trim().toLowerCase()

  if (!clean) {
    usernameStatus.value = 'idle'
    usernameFeedback.value = 'Pick a unique username for your public profile (/u/username).'
    return
  }

  if (clean.length < 3) {
    usernameStatus.value = 'invalid'
    usernameFeedback.value = 'Username must be at least 3 characters.'
    return
  }

  if (clean.length > 30) {
    usernameStatus.value = 'invalid'
    usernameFeedback.value = 'Username cannot exceed 30 characters.'
    return
  }

  if (!/^[a-z0-9_]+$/.test(clean)) {
    usernameStatus.value = 'invalid'
    usernameFeedback.value = 'Only lowercase letters, numbers, and underscores are allowed.'
    return
  }

  usernameStatus.value = 'checking'
  usernameFeedback.value = 'Checking username availability...'

  usernameDebounceTimer = setTimeout(async () => {
    try {
      const res: UsernameCheckResult = await profileService.checkUsernameAvailability(
        clean,
        auth.user?.id
      )
      if (res.is_available) {
        usernameStatus.value = 'valid'
        usernameFeedback.value = res.message || 'Username is available!'
      } else {
        usernameStatus.value = 'invalid'
        usernameFeedback.value = res.message || 'Username is already taken.'
      }
    } catch {
      usernameStatus.value = 'invalid'
      usernameFeedback.value = 'Could not verify username availability.'
    }
  }, 350)
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

  if (!firstName.value.trim() || !lastName.value.trim()) {
    errorMsg.value = 'Please enter both your first name and last name.'
    return
  }

  if (username.value.trim()) {
    if (usernameStatus.value === 'checking') {
      errorMsg.value = 'Please wait for username verification to finish.'
      return
    }
    if (usernameStatus.value === 'invalid') {
      errorMsg.value = 'Please choose a valid and unique username.'
      return
    }
  }

  loading.value = true

  try {
    let userId = auth.user?.id || auth.session?.user?.id
    let userEmail = auth.user?.email || auth.session?.user?.email

    if (!userId) {
      await auth.init()
      userId = auth.user?.id || auth.session?.user?.id
      userEmail = auth.user?.email || auth.session?.user?.email
    }

    if (!userId) {
      throw new Error('Your session has expired or no authenticated user was found. Please log in again.')
    }

    let avatarPath = auth.user?.avatar_path || null
    let coverPath = auth.user?.cover_path || null

    if (avatarFile.value) {
      await deleteProfileImage(avatarPath)
      const path = await uploadProfileImage(userId, avatarFile.value, 'avatar')
      if (!path) throw new Error('Failed to upload avatar. Please verify storage permissions.')
      avatarPath = path
    }

    if (coverFile.value) {
      await deleteProfileImage(coverPath)
      const path = await uploadProfileImage(userId, coverFile.value, 'cover')
      if (!path) throw new Error('Failed to upload cover. Please verify storage permissions.')
      coverPath = path
    }

    const profilePayload = {
      id: userId,
      email: userEmail || '',
      first_name: firstName.value.trim(),
      last_name: lastName.value.trim(),
      display_name: displayName.value.trim() || `${firstName.value} ${lastName.value}`.trim(),
      username: username.value.trim().toLowerCase() || null,
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

    await profileService.updateUserProfile(userId, profilePayload)
    await auth.fetchUserProfile()

    successMsg.value = 'Profile completed successfully! Redirecting to your profile...'

    setTimeout(() => {
      router.replace('/profile')
    }, 900)

  } catch (err: any) {
    console.error('Error submitting profile:', err)
    errorMsg.value = err.message || 'Something went wrong while saving your profile.'
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="min-h-screen bg-page py-10 px-4 sm:px-6 lg:px-8">
    <div class="max-w-3xl mx-auto">
      <div class="bg-card border border-border shadow-sm rounded-2xl overflow-hidden">
        <!-- Cover Preview -->
        <div class="h-44 sm:h-52 bg-gradient-to-r from-primary-light via-surface-raised to-primary/20 relative overflow-hidden group">
          <img
            v-if="coverUrl"
            :src="coverUrl"
            class="w-full h-full object-cover"
          />
          <div class="absolute inset-0 bg-black/25"></div>
          <label class="absolute bottom-3 right-3 sm:bottom-4 sm:right-4 bg-card/90 hover:bg-card text-text-strong text-xs font-semibold px-3 py-1.5 rounded-lg cursor-pointer shadow border border-border transition flex items-center gap-1.5">
            <Camera class="h-3.5 w-3.5" />
            <span>Change Cover</span>
            <input type="file" accept="image/*" class="hidden" @change="onCoverChange" />
          </label>
        </div>

        <!-- Avatar & Header info -->
        <div class="px-6 -mt-12 flex items-end space-x-5">
          <div class="relative">
            <div class="h-24 w-24 rounded-full ring-4 ring-card bg-surface shadow-md overflow-hidden flex items-center justify-center font-bold text-2xl text-primary uppercase select-none">
              <img
                v-if="avatarUrl"
                :src="avatarUrl"
                class="h-full w-full object-cover"
              />
              <span v-else>
                {{ firstName.charAt(0) || (auth.user?.email || 'U').charAt(0) }}
              </span>
            </div>
            <label class="absolute bottom-0 right-0 bg-primary hover:bg-primary-hover text-white p-1.5 rounded-full cursor-pointer shadow transition" title="Upload avatar">
              <Camera class="h-4 w-4" />
              <input type="file" accept="image/*" class="hidden" @change="onAvatarChange" />
            </label>
          </div>
          <div class="pb-1">
            <h1 class="text-xl sm:text-2xl font-bold text-text-strong">Complete your profile</h1>
            <p class="text-xs sm:text-sm text-text-muted">Welcome! Set up your primary details before getting started</p>
          </div>
        </div>

        <!-- Form -->
        <form novalidate class="px-6 py-6 sm:py-8 space-y-6" @submit.prevent="handleSubmit">
          <div v-if="errorMsg" class="rounded-xl alert-error p-3.5 text-xs sm:text-sm flex items-center gap-2">
            <XCircle class="h-4 w-4 shrink-0" />
            <span>{{ errorMsg }}</span>
          </div>

          <div v-if="successMsg" class="rounded-xl alert-success p-3.5 text-xs sm:text-sm flex items-center gap-2">
            <CheckCircle2 class="h-4 w-4 shrink-0" />
            <span>{{ successMsg }}</span>
          </div>

          <!-- Name Inputs -->
          <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div>
              <label class="block text-xs font-semibold text-text-strong mb-1.5">First name <span class="text-danger">*</span></label>
              <input
                v-model="firstName"
                required
                type="text"
                class="w-full text-xs sm:text-sm rounded-xl border border-border bg-surface px-3 py-2 text-text-strong focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition"
                placeholder="Jane"
              />
            </div>
            <div>
              <label class="block text-xs font-semibold text-text-strong mb-1.5">Last name <span class="text-danger">*</span></label>
              <input
                v-model="lastName"
                required
                type="text"
                class="w-full text-xs sm:text-sm rounded-xl border border-border bg-surface px-3 py-2 text-text-strong focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition"
                placeholder="Doe"
              />
            </div>
          </div>

          <!-- Display Name -->
          <div>
            <label class="block text-xs font-semibold text-text-strong mb-1.5">Display name</label>
            <input
              v-model="displayName"
              type="text"
              placeholder="How your name appears to other users"
              class="w-full text-xs sm:text-sm rounded-xl border border-border bg-surface px-3 py-2 text-text-strong focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition"
            />
          </div>

          <!-- Live Real-Time Username Check -->
          <div>
            <label class="block text-xs font-semibold text-text-strong mb-1.5">Choose a Username</label>
            <div
              class="relative rounded-xl border transition"
              :class="{
                'border-border focus-within:border-primary': usernameStatus === 'idle',
                'border-primary/50 focus-within:border-primary': usernameStatus === 'checking',
                'border-success focus-within:border-success ring-1 ring-success/20': usernameStatus === 'valid',
                'border-danger focus-within:border-danger ring-1 ring-danger/20': usernameStatus === 'invalid'
              }"
            >
              <div class="flex items-center">
                <span class="pl-3.5 pr-1 text-xs font-semibold text-text-muted select-none">
                  recruitment.app/u/
                </span>
                <input
                  v-model="username"
                  type="text"
                  maxlength="30"
                  class="w-full text-xs sm:text-sm bg-transparent py-2.5 pr-10 text-text-strong focus:outline-none"
                  placeholder="your_handle"
                />
                <!-- Status Icons -->
                <div class="pr-3.5 flex items-center">
                  <Loader2 v-if="usernameStatus === 'checking'" class="h-4 w-4 animate-spin text-primary" />
                  <CheckCircle2 v-else-if="usernameStatus === 'valid'" class="h-4 w-4 text-success" />
                  <XCircle v-else-if="usernameStatus === 'invalid'" class="h-4 w-4 text-danger" />
                  <AtSign v-else class="h-4 w-4 text-text-subtle" />
                </div>
              </div>
            </div>

            <!-- Feedback -->
            <p
              class="text-xs mt-1.5 transition"
              :class="{
                'text-text-muted': usernameStatus === 'idle',
                'text-primary': usernameStatus === 'checking',
                'text-success font-medium': usernameStatus === 'valid',
                'text-danger font-medium': usernameStatus === 'invalid'
              }"
            >
              {{ usernameFeedback }}
            </p>
          </div>

          <!-- Professional Headline -->
          <div>
            <label class="block text-xs font-semibold text-text-strong mb-1.5">Professional Headline</label>
            <input
              v-model="headline"
              type="text"
              placeholder="e.g. Full Stack Developer | Open to opportunities"
              class="w-full text-xs sm:text-sm rounded-xl border border-border bg-surface px-3 py-2 text-text-strong focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition"
            />
          </div>

          <!-- Job Title & Company -->
          <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div>
              <label class="block text-xs font-semibold text-text-strong mb-1.5">Current Job Title</label>
              <input
                v-model="currentJobTitle"
                type="text"
                placeholder="Software Engineer"
                class="w-full text-xs sm:text-sm rounded-xl border border-border bg-surface px-3 py-2 text-text-strong focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition"
              />
            </div>
            <div>
              <label class="block text-xs font-semibold text-text-strong mb-1.5">Current Company</label>
              <input
                v-model="currentCompany"
                type="text"
                placeholder="Tech Corp"
                class="w-full text-xs sm:text-sm rounded-xl border border-border bg-surface px-3 py-2 text-text-strong focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition"
              />
            </div>
          </div>

          <!-- Location & Phone -->
          <div class="grid grid-cols-1 sm:grid-cols-3 gap-4">
            <div>
              <label class="block text-xs font-semibold text-text-strong mb-1.5">Phone</label>
              <input
                v-model="phone"
                type="tel"
                placeholder="+1 555-0100"
                class="w-full text-xs sm:text-sm rounded-xl border border-border bg-surface px-3 py-2 text-text-strong focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition"
              />
            </div>
            <div>
              <label class="block text-xs font-semibold text-text-strong mb-1.5">City</label>
              <input
                v-model="city"
                type="text"
                placeholder="San Francisco"
                class="w-full text-xs sm:text-sm rounded-xl border border-border bg-surface px-3 py-2 text-text-strong focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition"
              />
            </div>
            <div>
              <label class="block text-xs font-semibold text-text-strong mb-1.5">Country (ISO)</label>
              <input
                v-model="countryCode"
                type="text"
                maxlength="2"
                placeholder="US"
                class="w-full text-xs sm:text-sm rounded-xl border border-border bg-surface px-3 py-2 text-text-strong focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition uppercase"
              />
            </div>
          </div>

          <!-- Bio -->
          <div>
            <label class="block text-xs font-semibold text-text-strong mb-1.5">Bio</label>
            <textarea
              v-model="bio"
              rows="3"
              class="w-full text-xs sm:text-sm rounded-xl border border-border bg-surface px-3 py-2 text-text-strong focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition resize-y"
              placeholder="Tell companies a bit about yourself..."
            ></textarea>
          </div>

          <!-- Action Buttons -->
          <div class="pt-2 flex items-center justify-end gap-3">
            <button
              type="submit"
              :disabled="loading"
              class="inline-flex items-center justify-center gap-2 px-6 py-2.5 rounded-xl text-xs sm:text-sm font-semibold text-white bg-primary hover:bg-primary-hover shadow-sm disabled:opacity-50 cursor-pointer transition"
            >
              <Loader2 v-if="loading" class="h-4 w-4 animate-spin" />
              <span>{{ loading ? 'Saving profile...' : 'Complete Profile & Continue' }}</span>
              <ArrowRight v-if="!loading" class="h-4 w-4" />
            </button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>
