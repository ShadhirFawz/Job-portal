<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { profileService } from '@/services/profile.service'
import { getProfileImageUrl } from '@/composables/useProfileImage'
import type { AppUser } from '@/types/user'
import type { Resume } from '@/types/resume'
import {
  User,
  Mail,
  MapPin,
  Briefcase,
  Building2,
  Globe,
  Linkedin,
  Github,
  Link as LinkIcon,
  Edit3,
  Share2,
  FileText,
  Eye,
  Download,
  CheckCircle2,
  Shield,
  Calendar,
  X,
  Loader2,
  FileCheck,
  ArrowLeft,
  UserX
} from 'lucide-vue-next'
import { toast } from 'vue-sonner'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()

const loading = ref(true)
const notFound = ref(false)
const profile = ref<AppUser | null>(null)
const primaryResume = ref<Resume | null>(null)
const loadingResume = ref(false)

// Google PDF Viewer Modal State
const showPreviewModal = ref(false)
const previewLoading = ref(false)
const previewSignedUrl = ref<string | null>(null)
const previewResume = ref<Resume | null>(null)

const usernameParam = computed(() => {
  return String(route.params.username || '').trim().toLowerCase()
})

const isOwner = computed(() => {
  return !!auth.user && !!profile.value && auth.user.id === profile.value.id
})

const userAvatarUrl = computed(() => {
  return profile.value?.avatar_path ? getProfileImageUrl(profile.value.avatar_path) : undefined
})

const userCoverUrl = computed(() => {
  return profile.value?.cover_path ? getProfileImageUrl(profile.value.cover_path) : undefined
})

const displayName = computed(() => {
  if (!profile.value) return 'User'
  return (
    profile.value.display_name ||
    `${profile.value.first_name || ''} ${profile.value.last_name || ''}`.trim() ||
    profile.value.username ||
    'Professional'
  )
})

const userInitial = computed(() => {
  return (displayName.value || 'U').charAt(0).toUpperCase()
})

const roleBadgeLabel = computed(() => {
  const role = profile.value?.role || 'candidate'
  switch (role) {
    case 'super_admin':
      return 'Super Admin'
    case 'org_admin':
      return 'Organization Admin'
    case 'org_staff':
      return 'Organization Staff'
    default:
      return 'Candidate'
  }
})

const googlePdfViewerUrl = computed(() => {
  if (!previewSignedUrl.value) return ''
  return `https://docs.google.com/viewer?url=${encodeURIComponent(previewSignedUrl.value)}&embedded=true`
})

async function fetchProfileData() {
  if (!usernameParam.value) {
    notFound.value = true
    loading.value = false
    return
  }

  loading.value = true
  notFound.value = false

  try {
    const data = await profileService.getUserProfileByUsername(usernameParam.value)
    if (!data) {
      notFound.value = true
      profile.value = null
      return
    }

    profile.value = data

    // Load primary resume
    loadingResume.value = true
    try {
      primaryResume.value = await profileService.getPrimaryResume(data.id)
    } catch (rErr) {
      console.warn('Failed to load primary resume for public view:', rErr)
    } finally {
      loadingResume.value = false
    }
  } catch (err) {
    console.error('Error fetching public profile:', err)
    notFound.value = true
  } finally {
    loading.value = false
  }
}

watch(
  () => route.params.username,
  () => {
    fetchProfileData()
  }
)

onMounted(async () => {
  await fetchProfileData()
})

function formatFileSize(bytes?: number | null): string {
  if (!bytes) return ''
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}

function formatDate(dateStr?: string | null): string {
  if (!dateStr) return ''
  try {
    return new Date(dateStr).toLocaleDateString(undefined, {
      month: 'short',
      day: 'numeric',
      year: 'numeric'
    })
  } catch {
    return dateStr
  }
}

async function handleOpenPdfPreview(resume: Resume) {
  if (!resume.file_path) {
    toast.error('Resume file path is missing.')
    return
  }

  previewResume.value = resume
  showPreviewModal.value = true
  previewLoading.value = true
  previewSignedUrl.value = null

  try {
    const url = await profileService.getResumeDownloadUrl(resume.file_path)
    if (!url) {
      toast.error('Failed to generate preview URL for this resume.')
      showPreviewModal.value = false
      return
    }
    previewSignedUrl.value = url
  } catch (err: any) {
    toast.error(err.message || 'Could not load resume preview.')
    showPreviewModal.value = false
  } finally {
    previewLoading.value = false
  }
}

function closePdfModal() {
  showPreviewModal.value = false
  previewSignedUrl.value = null
  previewResume.value = null
}

async function handleDownloadResume(resume: Resume) {
  if (!resume.file_path) {
    toast.error('Resume file path is missing.')
    return
  }
  try {
    const url = await profileService.getResumeDownloadUrl(resume.file_path)
    if (url) {
      const a = document.createElement('a')
      a.href = url
      a.download = resume.file_name || 'resume.pdf'
      a.target = '_blank'
      document.body.appendChild(a)
      a.click()
      document.body.removeChild(a)
    } else {
      toast.error('Failed to download resume.')
    }
  } catch {
    toast.error('Error downloading resume.')
  }
}

async function copyPublicProfileLink() {
  try {
    await navigator.clipboard.writeText(window.location.href)
    toast.success('Public profile link copied!')
  } catch {
    toast.error('Failed to copy link.')
  }
}
</script>

<template>
  <div class="min-h-screen bg-page py-6 sm:py-10 px-4 sm:px-6 lg:px-8">
    <div class="max-w-4xl mx-auto space-y-6">

      <!-- Owner Quick-Banner -->
      <div
        v-if="isOwner"
        class="flex items-center justify-between gap-3 p-3.5 rounded-xl border border-primary/20 bg-primary-light/60 text-xs sm:text-sm text-primary shadow-xs"
      >
        <div class="flex items-center gap-2">
          <CheckCircle2 class="h-4 w-4 shrink-0" />
          <span>You are previewing your own public profile as others see it.</span>
        </div>
        <router-link
          to="/profile/edit"
          class="inline-flex items-center gap-1 font-semibold hover:underline shrink-0"
        >
          <Edit3 class="h-3.5 w-3.5" />
          <span>Edit</span>
        </router-link>
      </div>

      <!-- Loading State -->
      <div v-if="loading" class="bg-card rounded-2xl border border-border p-12 text-center shadow-sm">
        <Loader2 class="h-8 w-8 animate-spin text-primary mx-auto mb-3" />
        <p class="text-xs sm:text-sm text-text-muted">Loading profile @{{ usernameParam }}...</p>
      </div>

      <!-- 404 Not Found State -->
      <div
        v-else-if="notFound || !profile"
        class="bg-card rounded-2xl border border-border p-12 text-center shadow-sm space-y-4"
      >
        <div class="h-16 w-16 rounded-full bg-surface-raised flex items-center justify-center text-text-subtle mx-auto">
          <UserX class="h-8 w-8" />
        </div>
        <div class="space-y-1">
          <h2 class="text-lg font-bold text-text-strong">User Not Found</h2>
          <p class="text-xs sm:text-sm text-text-muted max-w-sm mx-auto">
            The profile for username <span class="font-semibold text-text-strong">@{{ usernameParam }}</span> could not be found or does not exist.
          </p>
        </div>
        <div class="pt-2">
          <router-link
            to="/jobs"
            class="inline-flex items-center gap-2 px-4 py-2 text-xs sm:text-sm font-semibold rounded-xl text-white bg-primary hover:bg-primary-hover transition shadow-sm"
          >
            <ArrowLeft class="h-4 w-4" />
            <span>Explore Jobs</span>
          </router-link>
        </div>
      </div>

      <!-- Public Profile Found -->
      <div v-else class="space-y-6">

        <!-- Header Card (Cover, Avatar, Meta, Actions) -->
        <div class="bg-card rounded-2xl border border-border shadow-sm overflow-hidden">
          <!-- Cover Banner -->
          <div class="relative h-36 sm:h-52 w-full bg-gradient-to-r from-primary-light via-surface-raised to-primary/20 overflow-hidden">
            <img
              v-if="userCoverUrl"
              :src="userCoverUrl"
              alt="Cover Image"
              class="w-full h-full object-cover"
            />
            <div class="absolute inset-0 bg-black/10"></div>
          </div>

          <!-- Profile Details Bar -->
          <div class="px-5 sm:px-8 pb-6 sm:pb-8 pt-0 relative">
            <div class="flex flex-col sm:flex-row sm:items-end justify-between gap-4 -mt-14 sm:-mt-16 mb-4">
              <!-- Avatar -->
              <div class="relative">
                <div class="h-24 w-24 sm:h-32 sm:w-32 rounded-full border-4 border-card bg-surface shadow-md overflow-hidden flex items-center justify-center font-bold text-3xl sm:text-4xl text-primary">
                  <img
                    v-if="userAvatarUrl"
                    :src="userAvatarUrl"
                    :alt="displayName"
                    class="h-full w-full object-cover"
                  />
                  <span v-else>{{ userInitial }}</span>
                </div>
              </div>

              <!-- Top Action Buttons -->
              <div class="flex flex-wrap items-center gap-2 pt-2 sm:pt-0">
                <button
                  type="button"
                  @click="copyPublicProfileLink"
                  class="inline-flex items-center gap-1.5 px-3.5 py-2 text-xs font-semibold rounded-xl border border-border bg-card text-text-strong hover:bg-surface-raised transition shadow-xs cursor-pointer"
                  title="Share Profile Link"
                >
                  <Share2 class="h-3.5 w-3.5" />
                  <span>Share</span>
                </button>

                <router-link
                  v-if="isOwner"
                  to="/profile/edit"
                  class="inline-flex items-center gap-1.5 px-4 py-2 text-xs font-semibold rounded-xl text-white bg-primary hover:bg-primary-hover transition shadow-sm"
                >
                  <Edit3 class="h-3.5 w-3.5" />
                  <span>Edit Profile</span>
                </router-link>
              </div>
            </div>

            <!-- Identity Info -->
            <div class="space-y-1.5">
              <div class="flex flex-wrap items-center gap-2.5">
                <h1 class="text-xl sm:text-2xl font-bold text-text-strong tracking-tight">
                  {{ displayName }}
                </h1>

                <!-- Role Badge -->
                <span
                  class="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-[11px] font-bold uppercase tracking-wider border"
                  :class="{
                    'badge-primary': profile.role === 'candidate',
                    'badge-success': profile.role === 'org_admin',
                    'badge-warning': profile.role === 'org_staff',
                    'badge-danger': profile.role === 'super_admin'
                  }"
                >
                  <Shield class="h-3 w-3" />
                  <span>{{ roleBadgeLabel }}</span>
                </span>

                <!-- Verified Check -->
                <span
                  v-if="profile.is_profile_complete"
                  class="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[10px] font-semibold bg-success-bg text-success border border-success-border"
                >
                  <CheckCircle2 class="h-3 w-3" />
                  <span>Verified</span>
                </span>
              </div>

              <!-- Username & Headline -->
              <div class="flex flex-wrap items-center gap-2 text-xs text-text-muted">
                <span class="font-semibold text-primary">
                  @{{ profile.username }}
                </span>
                <span v-if="profile.current_job_title || profile.current_company">•</span>
                <span v-if="profile.current_job_title || profile.current_company" class="font-medium text-text-secondary">
                  {{ profile.current_job_title }}
                  <span v-if="profile.current_job_title && profile.current_company">at</span>
                  {{ profile.current_company }}
                </span>
              </div>

              <!-- LinkedIn-style Headline -->
              <p v-if="profile.headline" class="text-xs sm:text-sm text-text-strong font-medium pt-1 max-w-2xl leading-relaxed">
                {{ profile.headline }}
              </p>

              <!-- Location & Joined Meta -->
              <div class="flex flex-wrap items-center gap-x-4 gap-y-1.5 pt-2 text-xs text-text-muted">
                <span v-if="profile.city || profile.country_code" class="inline-flex items-center gap-1">
                  <MapPin class="h-3.5 w-3.5 text-text-subtle" />
                  <span>{{ [profile.city, profile.state_province, profile.country_code].filter(Boolean).join(', ') }}</span>
                </span>

                <span v-if="profile.created_at" class="inline-flex items-center gap-1">
                  <Calendar class="h-3.5 w-3.5 text-text-subtle" />
                  <span>Member since {{ formatDate(profile.created_at) }}</span>
                </span>
              </div>
            </div>
          </div>
        </div>

        <!-- Primary Resume Card (Public display) -->
        <div v-if="primaryResume" class="bg-card rounded-2xl border border-border p-5 sm:p-6 shadow-sm space-y-4">
          <div class="flex items-center justify-between border-b border-border pb-3">
            <div class="flex items-center gap-2">
              <FileCheck class="h-4 w-4 text-primary" />
              <h2 class="text-xs sm:text-sm font-bold uppercase tracking-wider text-text-muted">
                Primary Resume
              </h2>
            </div>
            <span class="text-[11px] text-text-muted">Verified Document</span>
          </div>

          <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4 p-4 rounded-xl border border-border bg-surface-raised/40 transition hover:border-border-strong">
            <div class="flex items-start gap-3.5 min-w-0">
              <div class="h-10 w-10 rounded-lg bg-primary-light text-primary flex items-center justify-center shrink-0 shadow-xs">
                <FileText class="h-5 w-5" />
              </div>
              <div class="min-w-0 space-y-0.5">
                <div class="flex items-center gap-2">
                  <h3 class="text-xs sm:text-sm font-bold text-text-strong truncate">
                    {{ primaryResume.title }}
                  </h3>
                  <span class="inline-flex items-center px-2 py-0.2 rounded-full text-[10px] font-bold uppercase tracking-wider bg-success-bg text-success border border-success-border">
                    Primary
                  </span>
                </div>
                <p class="text-xs text-text-muted truncate">
                  {{ primaryResume.file_name }}
                  <span v-if="primaryResume.file_size">• {{ formatFileSize(primaryResume.file_size) }}</span>
                  <span v-if="primaryResume.created_at">• Uploaded {{ formatDate(primaryResume.created_at) }}</span>
                </p>
              </div>
            </div>

            <!-- Preview & Download Actions -->
            <div class="flex items-center gap-2 shrink-0 self-start sm:self-auto">
              <button
                type="button"
                @click="handleOpenPdfPreview(primaryResume)"
                class="inline-flex items-center justify-center gap-1.5 px-3.5 py-1.5 text-xs font-semibold rounded-lg bg-primary-light text-primary hover:bg-primary hover:text-white border border-primary/25 transition shadow-xs cursor-pointer"
              >
                <Eye class="h-3.5 w-3.5" />
                <span>Preview PDF</span>
              </button>

              <button
                type="button"
                @click="handleDownloadResume(primaryResume)"
                class="inline-flex items-center justify-center gap-1.5 px-3.5 py-1.5 text-xs font-semibold rounded-lg border border-border bg-card text-text-secondary hover:text-text-strong hover:bg-surface-raised transition shadow-xs cursor-pointer"
                title="Download file"
              >
                <Download class="h-3.5 w-3.5" />
                <span class="hidden sm:inline">Download</span>
              </button>
            </div>
          </div>
        </div>

        <!-- About & Bio Section -->
        <div v-if="profile.bio" class="bg-card rounded-2xl border border-border p-5 sm:p-6 shadow-sm space-y-3">
          <h2 class="text-xs sm:text-sm font-bold uppercase tracking-wider text-text-muted border-b border-border pb-2">
            About
          </h2>
          <p class="text-xs sm:text-sm text-text-secondary whitespace-pre-line leading-relaxed">
            {{ profile.bio }}
          </p>
        </div>

        <!-- Links & Social Profiles -->
        <div
          v-if="profile.website_url || profile.linkedin_url || profile.github_url || profile.portfolio_url"
          class="bg-card rounded-2xl border border-border p-5 sm:p-6 shadow-sm space-y-3"
        >
          <h2 class="text-xs sm:text-sm font-bold uppercase tracking-wider text-text-muted border-b border-border pb-2">
            Links & Online Profiles
          </h2>
          <div class="grid grid-cols-1 sm:grid-cols-2 gap-3 pt-1">
            <a
              v-if="profile.website_url"
              :href="profile.website_url"
              target="_blank"
              rel="noopener noreferrer"
              class="flex items-center gap-2.5 p-3 rounded-xl border border-border bg-surface-raised/40 hover:bg-surface-raised text-xs text-text-strong transition"
            >
              <Globe class="h-4 w-4 text-primary shrink-0" />
              <span class="truncate">{{ profile.website_url.replace(/^https?:\/\//, '') }}</span>
            </a>

            <a
              v-if="profile.linkedin_url"
              :href="profile.linkedin_url"
              target="_blank"
              rel="noopener noreferrer"
              class="flex items-center gap-2.5 p-3 rounded-xl border border-border bg-surface-raised/40 hover:bg-surface-raised text-xs text-text-strong transition"
            >
              <Linkedin class="h-4 w-4 text-primary shrink-0" />
              <span class="truncate">{{ profile.linkedin_url.replace(/^https?:\/\//, '') }}</span>
            </a>

            <a
              v-if="profile.github_url"
              :href="profile.github_url"
              target="_blank"
              rel="noopener noreferrer"
              class="flex items-center gap-2.5 p-3 rounded-xl border border-border bg-surface-raised/40 hover:bg-surface-raised text-xs text-text-strong transition"
            >
              <Github class="h-4 w-4 text-primary shrink-0" />
              <span class="truncate">{{ profile.github_url.replace(/^https?:\/\//, '') }}</span>
            </a>

            <a
              v-if="profile.portfolio_url"
              :href="profile.portfolio_url"
              target="_blank"
              rel="noopener noreferrer"
              class="flex items-center gap-2.5 p-3 rounded-xl border border-border bg-surface-raised/40 hover:bg-surface-raised text-xs text-text-strong transition"
            >
              <LinkIcon class="h-4 w-4 text-primary shrink-0" />
              <span class="truncate">{{ profile.portfolio_url.replace(/^https?:\/\//, '') }}</span>
            </a>
          </div>
        </div>

      </div>
    </div>

    <!-- Google PDF Viewer Popup Modal -->
    <Teleport to="body">
      <div
        v-if="showPreviewModal"
        class="fixed inset-0 z-50 overflow-y-auto bg-black/70 backdrop-blur-xs flex items-center justify-center p-3 sm:p-6"
        @keydown.esc="closePdfModal"
      >
        <div
          class="bg-card w-full max-w-4xl h-[90vh] rounded-2xl border border-border shadow-2xl overflow-hidden flex flex-col my-auto animate-in fade-in zoom-in-95 duration-150"
        >
          <!-- Modal Header -->
          <div class="px-5 py-3.5 border-b border-border bg-surface flex items-center justify-between gap-4 shrink-0">
            <div class="flex items-center gap-2.5 min-w-0">
              <div class="h-8 w-8 rounded-lg bg-primary-light text-primary flex items-center justify-center shrink-0">
                <FileText class="h-4 w-4" />
              </div>
              <div class="min-w-0">
                <h3 class="text-xs sm:text-sm font-bold text-text-strong truncate">
                  {{ previewResume?.title || 'Resume Document' }}
                </h3>
                <p class="text-[11px] text-text-muted truncate">
                  {{ previewResume?.file_name }}
                  <span v-if="previewResume?.file_size">• {{ formatFileSize(previewResume.file_size) }}</span>
                </p>
              </div>
            </div>

            <!-- Actions -->
            <div class="flex items-center gap-2 shrink-0">
              <a
                v-if="previewSignedUrl"
                :href="previewSignedUrl"
                target="_blank"
                download
                class="inline-flex items-center gap-1 px-3 py-1.5 text-xs font-semibold rounded-lg bg-primary-light text-primary hover:bg-primary hover:text-white border border-primary/20 transition cursor-pointer"
              >
                <Download class="h-3.5 w-3.5" />
                <span class="hidden sm:inline">Download</span>
              </a>

              <button
                type="button"
                @click="closePdfModal"
                class="p-1.5 rounded-lg text-text-muted hover:text-text-strong hover:bg-surface-raised transition cursor-pointer"
                title="Close Viewer"
              >
                <X class="h-5 w-5" />
              </button>
            </div>
          </div>

          <!-- Modal Body (Embedded Google PDF Viewer) -->
          <div class="flex-1 bg-surface-raised relative overflow-hidden">
            <div v-if="previewLoading" class="absolute inset-0 flex flex-col items-center justify-center gap-2 bg-card/60">
              <Loader2 class="h-8 w-8 animate-spin text-primary" />
              <p class="text-xs text-text-muted">Loading PDF Document in Google Viewer...</p>
            </div>

            <iframe
              v-else-if="googlePdfViewerUrl"
              :src="googlePdfViewerUrl"
              class="w-full h-full border-0"
              title="Resume Preview"
            ></iframe>

            <div v-else class="h-full flex flex-col items-center justify-center p-6 text-center space-y-3">
              <FileText class="h-10 w-10 text-text-subtle" />
              <p class="text-xs sm:text-sm text-text-muted">Could not preview the PDF file directly.</p>
              <button
                v-if="previewSignedUrl"
                type="button"
                @click="handleDownloadResume(previewResume!)"
                class="px-4 py-2 text-xs font-semibold rounded-lg text-white bg-primary hover:bg-primary-hover transition"
              >
                Download PDF to View
              </button>
            </div>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>
