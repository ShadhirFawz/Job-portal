<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { profileService } from '@/services/profile.service'
import {
  uploadProfileImage,
  deleteProfileImage,
  getProfileImageUrl
} from '@/composables/useProfileImage'
import type { AppUser, ProfileUpdateInput, UsernameCheckResult } from '@/types/user'
import {
  User,
  Mail,
  Phone,
  MapPin,
  Briefcase,
  Building2,
  Globe,
  Linkedin,
  Github,
  Link as LinkIcon,
  Camera,
  CheckCircle2,
  XCircle,
  AlertCircle,
  Loader2,
  ArrowLeft,
  Save,
  AtSign,
  FileText,
  Sparkles
} from 'lucide-vue-next'
import { toast } from 'vue-sonner'

const router = useRouter()
const auth = useAuthStore()

const loading = ref(true)
const saving = ref(false)
const profile = ref<AppUser | null>(null)

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
const stateProvince = ref('')
const city = ref('')
const websiteUrl = ref('')
const linkedinUrl = ref('')
const githubUrl = ref('')
const portfolioUrl = ref('')

// Image uploads
const avatarFile = ref<File | null>(null)
const coverFile = ref<File | null>(null)
const avatarPreview = ref<string | null>(null)
const coverPreview = ref<string | null>(null)

// Username validation state
const usernameStatus = ref<'idle' | 'checking' | 'valid' | 'invalid' | 'current'>('idle')
const usernameFeedback = ref<string>('')
let usernameDebounceTimer: ReturnType<typeof setTimeout> | null = null

const userAvatarUrl = computed(() => {
  return (
    avatarPreview.value ||
    (profile.value?.avatar_path ? getProfileImageUrl(profile.value.avatar_path) : undefined)
  )
})

const userCoverUrl = computed(() => {
  return (
    coverPreview.value ||
    (profile.value?.cover_path ? getProfileImageUrl(profile.value.cover_path) : undefined)
  )
})

const userInitial = computed(() => {
  const name =
    displayName.value ||
    `${firstName.value} ${lastName.value}`.trim() ||
    auth.user?.email ||
    'U'
  return name.charAt(0).toUpperCase()
})

function populateForm(data: AppUser) {
  profile.value = data
  firstName.value = data.first_name || ''
  lastName.value = data.last_name || ''
  displayName.value = data.display_name || ''
  username.value = data.username || ''
  phone.value = data.phone || ''
  headline.value = data.headline || ''
  currentJobTitle.value = data.current_job_title || ''
  currentCompany.value = data.current_company || ''
  bio.value = data.bio || ''
  countryCode.value = data.country_code || ''
  stateProvince.value = data.state_province || ''
  city.value = data.city || ''
  websiteUrl.value = data.website_url || ''
  linkedinUrl.value = data.linkedin_url || ''
  githubUrl.value = data.github_url || ''
  portfolioUrl.value = data.portfolio_url || ''

  if (data.username) {
    usernameStatus.value = 'current'
    usernameFeedback.value = 'This is your current username.'
  }
}

onMounted(async () => {
  loading.value = true
  try {
    if (!auth.user) {
      await auth.init()
    }
    if (!auth.user) {
      router.push('/login')
      return
    }

    const data = await profileService.getUserProfile(auth.user.id)
    populateForm(data || auth.user)
  } catch (err) {
    console.error('Error fetching profile:', err)
    toast.error('Failed to load profile data.')
  } finally {
    loading.value = false
  }
})

// Debounced live username check
watch(username, (val) => {
  if (usernameDebounceTimer) {
    clearTimeout(usernameDebounceTimer)
  }

  const clean = val.trim().toLowerCase()

  if (!clean) {
    usernameStatus.value = 'idle'
    usernameFeedback.value = 'Set a unique username for your public profile URL (/u/username).'
    return
  }

  if (profile.value?.username && clean === profile.value.username.toLowerCase()) {
    usernameStatus.value = 'current'
    usernameFeedback.value = 'This is your current username.'
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
  usernameFeedback.value = 'Checking availability...'

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

async function handleSave() {
  if (saving.value) return

  if (!firstName.value.trim() || !lastName.value.trim()) {
    toast.error('First name and last name are required.')
    return
  }

  if (username.value.trim()) {
    if (usernameStatus.value === 'checking') {
      toast.info('Please wait for username validation to finish.')
      return
    }
    if (usernameStatus.value === 'invalid') {
      toast.error('Please choose a valid and available username.')
      return
    }
  }

  saving.value = true

  try {
    const userId = auth.user?.id
    if (!userId) {
      throw new Error('User session not found.')
    }

    let avatarPath = profile.value?.avatar_path || auth.user?.avatar_path || null
    let coverPath = profile.value?.cover_path || auth.user?.cover_path || null

    if (avatarFile.value) {
      await deleteProfileImage(avatarPath)
      const path = await uploadProfileImage(userId, avatarFile.value, 'avatar')
      if (path) avatarPath = path
    }

    if (coverFile.value) {
      await deleteProfileImage(coverPath)
      const path = await uploadProfileImage(userId, coverFile.value, 'cover')
      if (path) coverPath = path
    }

    const payload: ProfileUpdateInput = {
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
      state_province: stateProvince.value.trim() || null,
      city: city.value.trim() || null,
      website_url: websiteUrl.value.trim() || null,
      linkedin_url: linkedinUrl.value.trim() || null,
      github_url: githubUrl.value.trim() || null,
      portfolio_url: portfolioUrl.value.trim() || null,
      avatar_path: avatarPath,
      cover_path: coverPath,
      is_profile_complete: true
    }

    await profileService.updateUserProfile(userId, payload)
    await auth.fetchUserProfile()

    toast.success('Profile updated successfully!')
    router.push('/profile')
  } catch (err: any) {
    console.error('Error saving profile:', err)
    toast.error(err.message || 'Failed to update profile.')
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <div class="min-h-screen bg-page py-6 sm:py-10 px-4 sm:px-6 lg:px-8">
    <div class="max-w-4xl mx-auto space-y-6">

      <!-- Navigation & Page Title -->
      <div class="flex items-center justify-between gap-4">
        <router-link
          to="/profile"
          class="inline-flex items-center gap-1.5 text-xs sm:text-sm font-semibold text-text-muted hover:text-text-strong transition"
        >
          <ArrowLeft class="h-4 w-4" />
          <span>Back to Profile</span>
        </router-link>

        <button
          type="button"
          @click="handleSave"
          :disabled="saving"
          class="inline-flex items-center gap-1.5 px-4 py-2 text-xs sm:text-sm font-semibold rounded-xl text-white bg-primary hover:bg-primary-hover transition shadow-sm disabled:opacity-50 cursor-pointer"
        >
          <Loader2 v-if="saving" class="h-4 w-4 animate-spin" />
          <Save v-else class="h-4 w-4" />
          <span>{{ saving ? 'Saving Changes...' : 'Save Profile' }}</span>
        </button>
      </div>

      <!-- Loading State -->
      <div v-if="loading" class="bg-card rounded-2xl border border-border p-12 text-center shadow-sm">
        <Loader2 class="h-8 w-8 animate-spin text-primary mx-auto mb-3" />
        <p class="text-xs sm:text-sm text-text-muted">Loading profile settings...</p>
      </div>

      <!-- Edit Form -->
      <form v-else @submit.prevent="handleSave" class="space-y-6">

        <!-- Cover & Avatar Customization Card -->
        <div class="bg-card rounded-2xl border border-border shadow-sm overflow-hidden">
          <!-- Cover Image Upload Area -->
          <div class="relative h-40 sm:h-52 w-full bg-gradient-to-r from-primary-light via-surface-raised to-primary/20 overflow-hidden group">
            <img
              v-if="userCoverUrl"
              :src="userCoverUrl"
              alt="Cover Image"
              class="w-full h-full object-cover"
            />
            <div class="absolute inset-0 bg-black/30 group-hover:bg-black/40 transition"></div>

            <label class="absolute bottom-3 right-3 sm:bottom-4 sm:right-4 inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold rounded-lg bg-card/90 hover:bg-card text-text-strong shadow-md border border-border cursor-pointer transition">
              <Camera class="h-3.5 w-3.5" />
              <span>Change Cover</span>
              <input type="file" accept="image/*" class="hidden" @change="onCoverChange" />
            </label>
          </div>

          <!-- Avatar Image Upload Area -->
          <div class="px-5 sm:px-8 pb-6 pt-0 relative">
            <div class="flex items-end justify-between -mt-14 sm:-mt-16 mb-4">
              <div class="relative group">
                <div class="h-24 w-24 sm:h-28 sm:w-28 rounded-full border-4 border-card bg-surface shadow-md overflow-hidden flex items-center justify-center font-bold text-3xl text-primary">
                  <img
                    v-if="userAvatarUrl"
                    :src="userAvatarUrl"
                    :alt="displayName"
                    class="h-full w-full object-cover"
                  />
                  <span v-else>{{ userInitial }}</span>
                </div>

                <label class="absolute bottom-0 right-0 p-2 rounded-full bg-primary hover:bg-primary-hover text-white shadow-md cursor-pointer transition" title="Change Avatar">
                  <Camera class="h-4 w-4" />
                  <input type="file" accept="image/*" class="hidden" @change="onAvatarChange" />
                </label>
              </div>

              <div class="text-right text-[11px] text-text-muted">
                JPG, PNG, or WebP. Max 5MB.
              </div>
            </div>
          </div>
        </div>

        <!-- Identity & Username Section -->
        <div class="bg-card rounded-2xl border border-border p-5 sm:p-6 shadow-sm space-y-4">
          <div class="border-b border-border pb-3 flex items-center justify-between">
            <h2 class="text-xs sm:text-sm font-bold uppercase tracking-wider text-text-strong flex items-center gap-2">
              <User class="h-4 w-4 text-primary" />
              <span>Identity & Account</span>
            </h2>
            <span class="text-[11px] text-text-muted">* Required fields</span>
          </div>

          <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <!-- First Name -->
            <div>
              <label class="block text-xs font-semibold text-text-strong mb-1.5">
                First Name <span class="text-danger">*</span>
              </label>
              <input
                v-model="firstName"
                type="text"
                required
                class="w-full text-xs sm:text-sm rounded-xl border border-border bg-surface px-3 py-2 text-text-strong focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition"
                placeholder="Jane"
              />
            </div>

            <!-- Last Name -->
            <div>
              <label class="block text-xs font-semibold text-text-strong mb-1.5">
                Last Name <span class="text-danger">*</span>
              </label>
              <input
                v-model="lastName"
                type="text"
                required
                class="w-full text-xs sm:text-sm rounded-xl border border-border bg-surface px-3 py-2 text-text-strong focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition"
                placeholder="Doe"
              />
            </div>

            <!-- Display Name -->
            <div>
              <label class="block text-xs font-semibold text-text-strong mb-1.5">
                Display Name
              </label>
              <input
                v-model="displayName"
                type="text"
                class="w-full text-xs sm:text-sm rounded-xl border border-border bg-surface px-3 py-2 text-text-strong focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition"
                placeholder="How your name appears to others"
              />
            </div>

            <!-- Email (Read-only) -->
            <div>
              <label class="block text-xs font-semibold text-text-strong mb-1.5">
                Email Address
              </label>
              <div class="relative">
                <input
                  :value="auth.user?.email"
                  type="email"
                  disabled
                  class="w-full text-xs sm:text-sm rounded-xl border border-border bg-surface-raised px-3 py-2 text-text-muted cursor-not-allowed opacity-80"
                />
                <span class="absolute right-3 top-2.5 text-[10px] uppercase font-bold text-text-subtle">
                  Managed
                </span>
              </div>
            </div>
          </div>

          <!-- Real-Time Live Username Check Input -->
          <div class="pt-2">
            <label class="block text-xs font-semibold text-text-strong mb-1.5">
              Public Profile Username
            </label>
            <div class="relative rounded-xl border transition" :class="{
              'border-border focus-within:border-primary': usernameStatus === 'idle',
              'border-primary/50 focus-within:border-primary': usernameStatus === 'checking',
              'border-success focus-within:border-success ring-1 ring-success/20': usernameStatus === 'valid' || usernameStatus === 'current',
              'border-danger focus-within:border-danger ring-1 ring-danger/20': usernameStatus === 'invalid'
            }">
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
                  <CheckCircle2 v-else-if="usernameStatus === 'valid' || usernameStatus === 'current'" class="h-4 w-4 text-success" />
                  <XCircle v-else-if="usernameStatus === 'invalid'" class="h-4 w-4 text-danger" />
                  <AtSign v-else class="h-4 w-4 text-text-subtle" />
                </div>
              </div>
            </div>

            <!-- Live Availability Feedback Message -->
            <div class="flex items-center gap-1.5 mt-1.5 text-xs">
              <span
                :class="{
                  'text-text-muted': usernameStatus === 'idle',
                  'text-primary': usernameStatus === 'checking',
                  'text-success font-medium': usernameStatus === 'valid' || usernameStatus === 'current',
                  'text-danger font-medium': usernameStatus === 'invalid'
                }"
              >
                {{ usernameFeedback }}
              </span>
            </div>
          </div>
        </div>

        <!-- Professional Information Section -->
        <div class="bg-card rounded-2xl border border-border p-5 sm:p-6 shadow-sm space-y-4">
          <h2 class="text-xs sm:text-sm font-bold uppercase tracking-wider text-text-strong flex items-center gap-2 border-b border-border pb-3">
            <Briefcase class="h-4 w-4 text-primary" />
            <span>Professional Details</span>
          </h2>

          <div class="space-y-4">
            <!-- Headline -->
            <div>
              <label class="block text-xs font-semibold text-text-strong mb-1.5">
                Professional Headline
              </label>
              <input
                v-model="headline"
                type="text"
                maxlength="160"
                class="w-full text-xs sm:text-sm rounded-xl border border-border bg-surface px-3 py-2 text-text-strong focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition"
                placeholder="e.g. Senior Software Engineer | Distributed Systems & Cloud Infrastructure"
              />
              <p class="text-[11px] text-text-muted mt-1">Briefly summarize what you do in one line.</p>
            </div>

            <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <!-- Job Title -->
              <div>
                <label class="block text-xs font-semibold text-text-strong mb-1.5">
                  Current Job Title
                </label>
                <input
                  v-model="currentJobTitle"
                  type="text"
                  class="w-full text-xs sm:text-sm rounded-xl border border-border bg-surface px-3 py-2 text-text-strong focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition"
                  placeholder="e.g. Full-Stack Developer"
                />
              </div>

              <!-- Company -->
              <div>
                <label class="block text-xs font-semibold text-text-strong mb-1.5">
                  Current Company
                </label>
                <input
                  v-model="currentCompany"
                  type="text"
                  class="w-full text-xs sm:text-sm rounded-xl border border-border bg-surface px-3 py-2 text-text-strong focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition"
                  placeholder="e.g. Acme Corporation"
                />
              </div>
            </div>

            <!-- Bio -->
            <div>
              <label class="block text-xs font-semibold text-text-strong mb-1.5">
                About / Bio
              </label>
              <textarea
                v-model="bio"
                rows="4"
                class="w-full text-xs sm:text-sm rounded-xl border border-border bg-surface px-3 py-2 text-text-strong focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition resize-y"
                placeholder="Share a short summary of your background, passions, achievements, and core skills..."
              ></textarea>
            </div>
          </div>
        </div>

        <!-- Contact & Location Section -->
        <div class="bg-card rounded-2xl border border-border p-5 sm:p-6 shadow-sm space-y-4">
          <h2 class="text-xs sm:text-sm font-bold uppercase tracking-wider text-text-strong flex items-center gap-2 border-b border-border pb-3">
            <MapPin class="h-4 w-4 text-primary" />
            <span>Contact & Location</span>
          </h2>

          <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
            <!-- Phone -->
            <div class="sm:col-span-2">
              <label class="block text-xs font-semibold text-text-strong mb-1.5">
                Phone Number
              </label>
              <input
                v-model="phone"
                type="tel"
                class="w-full text-xs sm:text-sm rounded-xl border border-border bg-surface px-3 py-2 text-text-strong focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition"
                placeholder="+1 (555) 000-0000"
              />
            </div>

            <!-- City -->
            <div>
              <label class="block text-xs font-semibold text-text-strong mb-1.5">
                City
              </label>
              <input
                v-model="city"
                type="text"
                class="w-full text-xs sm:text-sm rounded-xl border border-border bg-surface px-3 py-2 text-text-strong focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition"
                placeholder="San Francisco"
              />
            </div>

            <!-- State / Province -->
            <div>
              <label class="block text-xs font-semibold text-text-strong mb-1.5">
                State / Province
              </label>
              <input
                v-model="stateProvince"
                type="text"
                class="w-full text-xs sm:text-sm rounded-xl border border-border bg-surface px-3 py-2 text-text-strong focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition"
                placeholder="California"
              />
            </div>

            <!-- Country Code -->
            <div class="sm:col-span-2">
              <label class="block text-xs font-semibold text-text-strong mb-1.5">
                Country
              </label>
              <input
                v-model="countryCode"
                type="text"
                class="w-full text-xs sm:text-sm rounded-xl border border-border bg-surface px-3 py-2 text-text-strong focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition"
                placeholder="United States"
              />
            </div>
          </div>
        </div>

        <!-- Links & Social Presence -->
        <div class="bg-card rounded-2xl border border-border p-5 sm:p-6 shadow-sm space-y-4">
          <h2 class="text-xs sm:text-sm font-bold uppercase tracking-wider text-text-strong flex items-center gap-2 border-b border-border pb-3">
            <Globe class="h-4 w-4 text-primary" />
            <span>Online Presence & Social Links</span>
          </h2>

          <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <!-- Personal Website -->
            <div>
              <label class="block text-xs font-semibold text-text-strong mb-1.5 flex items-center gap-1.5">
                <Globe class="h-3.5 w-3.5 text-primary" />
                <span>Personal Website / Blog</span>
              </label>
              <input
                v-model="websiteUrl"
                type="url"
                class="w-full text-xs sm:text-sm rounded-xl border border-border bg-surface px-3 py-2 text-text-strong focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition"
                placeholder="https://yourwebsite.com"
              />
            </div>

            <!-- LinkedIn -->
            <div>
              <label class="block text-xs font-semibold text-text-strong mb-1.5 flex items-center gap-1.5">
                <Linkedin class="h-3.5 w-3.5 text-primary" />
                <span>LinkedIn Profile URL</span>
              </label>
              <input
                v-model="linkedinUrl"
                type="url"
                class="w-full text-xs sm:text-sm rounded-xl border border-border bg-surface px-3 py-2 text-text-strong focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition"
                placeholder="https://linkedin.com/in/username"
              />
            </div>

            <!-- GitHub -->
            <div>
              <label class="block text-xs font-semibold text-text-strong mb-1.5 flex items-center gap-1.5">
                <Github class="h-3.5 w-3.5 text-primary" />
                <span>GitHub Profile URL</span>
              </label>
              <input
                v-model="githubUrl"
                type="url"
                class="w-full text-xs sm:text-sm rounded-xl border border-border bg-surface px-3 py-2 text-text-strong focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition"
                placeholder="https://github.com/username"
              />
            </div>

            <!-- Portfolio -->
            <div>
              <label class="block text-xs font-semibold text-text-strong mb-1.5 flex items-center gap-1.5">
                <LinkIcon class="h-3.5 w-3.5 text-primary" />
                <span>Portfolio / Other Link</span>
              </label>
              <input
                v-model="portfolioUrl"
                type="url"
                class="w-full text-xs sm:text-sm rounded-xl border border-border bg-surface px-3 py-2 text-text-strong focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition"
                placeholder="https://dribbble.com/username"
              />
            </div>
          </div>
        </div>

        <!-- Action Buttons Footer -->
        <div class="flex items-center justify-end gap-3 pt-2">
          <router-link
            to="/profile"
            class="px-4 py-2 text-xs sm:text-sm font-semibold rounded-xl border border-border bg-card text-text-muted hover:text-text-strong hover:bg-surface-raised transition"
          >
            Cancel
          </router-link>

          <button
            type="submit"
            :disabled="saving"
            class="inline-flex items-center gap-1.5 px-5 py-2 text-xs sm:text-sm font-semibold rounded-xl text-white bg-primary hover:bg-primary-hover transition shadow-sm disabled:opacity-50 cursor-pointer"
          >
            <Loader2 v-if="saving" class="h-4 w-4 animate-spin" />
            <Save v-else class="h-4 w-4" />
            <span>{{ saving ? 'Saving Changes...' : 'Save Profile' }}</span>
          </button>
        </div>

      </form>
    </div>
  </div>
</template>

