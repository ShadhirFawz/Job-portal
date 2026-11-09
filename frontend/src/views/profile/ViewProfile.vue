<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { profileService } from '@/services/profile.service'
import { getProfileImageUrl } from '@/composables/useProfileImage'
import type { AppUser } from '@/types/user'
import type { Resume } from '@/types/resume'
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
  Edit3,
  Share2,
  FileText,
  Eye,
  Download,
  ExternalLink,
  CheckCircle2,
  Shield,
  Calendar,
  X,
  Loader2,
  Clock,
  Sparkles,
  FileCheck
} from 'lucide-vue-next'
import { toast } from 'vue-sonner'

const router = useRouter()
const auth = useAuthStore()

const loading = ref(true)
const profile = ref<AppUser | null>(null)
const primaryResume = ref<Resume | null>(null)
const loadingResume = ref(false)

// Google PDF Viewer Modal State
const showPreviewModal = ref(false)
const previewLoading = ref(false)
const previewSignedUrl = ref<string | null>(null)
const previewResume = ref<Resume | null>(null)

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
    profile.value.email
  )
})

const userInitial = computed(() => {
  return (displayName.value || 'U').charAt(0).toUpperCase()
})

const roleBadgeLabel = computed(() => {
  const role = profile.value?.role || auth.role || 'candidate'
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

const publicProfileUrl = computed(() => {
  if (!profile.value?.username) return null
  return `${window.location.origin}/u/${profile.value.username}`
})

const googlePdfViewerUrl = computed(() => {
  if (!previewSignedUrl.value) return ''
  return `https://docs.google.com/viewer?url=${encodeURIComponent(previewSignedUrl.value)}&embedded=true`
})

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

    // 1. Fetch latest profile
    const data = await profileService.getUserProfile(auth.user.id)
    profile.value = data || auth.user

    // 2. Fetch primary resume
    loadingResume.value = true
    try {
      primaryResume.value = await profileService.getPrimaryResume(auth.user.id)
    } catch (rErr) {
      console.warn('Failed to fetch primary resume:', rErr)
    } finally {
      loadingResume.value = false
    }
  } catch (err: any) {
    console.error('Error loading profile view:', err)
    toast.error('Failed to load profile.')
  } finally {
    loading.value = false
  }
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
      toast.error('Failed to generate download link.')
    }
  } catch (err) {
    toast.error('Error downloading resume.')
  }
}

async function copyPublicProfileLink() {
  if (!publicProfileUrl.value) {
    toast.info('Please set a username in profile settings to share your public profile.')
    return
  }
  try {
    await navigator.clipboard.writeText(publicProfileUrl.value)
    toast.success('Public profile link copied to clipboard!')
  } catch {
    toast.error('Failed to copy link.')
  }
}
</script>

<template>
  <div class="min-h-screen bg-page py-6 sm:py-10 px-4 sm:px-6 lg:px-8">
    <div class="max-w-4xl mx-auto space-y-6">

      <!-- Loading State -->
      <div v-if="loading" class="bg-card rounded-2xl border border-border p-12 text-center shadow-sm">
        <Loader2 class="h-8 w-8 animate-spin text-primary mx-auto mb-3" />
        <p class="text-xs sm:text-sm text-text-muted">Loading your profile...</p>
      </div>

      <div v-else-if="profile" class="space-y-6">

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
                <router-link
                  to="/profile/edit"
                  class="inline-flex items-center gap-1.5 px-4 py-2 text-xs font-semibold rounded-xl text-white bg-primary hover:bg-primary-hover transition shadow-sm"
                >
                  <Edit3 class="h-3.5 w-3.5" />
                  <span>Edit Profile</span>
                </router-link>

                <button
                  v-if="profile.username"
                  type="button"
                  @click="copyPublicProfileLink"
                  class="inline-flex items-center gap-1.5 px-3.5 py-2 text-xs font-semibold rounded-xl border border-border bg-card text-text-strong hover:bg-surface-raised transition shadow-xs"
                  title="Copy Public Link"
                >
                  <Share2 class="h-3.5 w-3.5" />
                  <span>Share Profile</span>
                </button>

                <router-link
                  v-if="profile.username"
                  :to="`/u/${profile.username}`"
                  target="_blank"
                  class="inline-flex items-center gap-1.5 px-3.5 py-2 text-xs font-semibold rounded-xl border border-border bg-card text-text-muted hover:text-text-strong hover:bg-surface-raised transition shadow-xs"
                  title="View Public Profile"
                >
                  <ExternalLink class="h-3.5 w-3.5" />
                  <span class="hidden sm:inline">Public View</span>
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

                <!-- Profile Complete Status -->
                <span
                  v-if="profile.is_profile_complete"
                  class="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-[10px] font-semibold bg-success-bg text-success border border-success-border"
                >
                  <CheckCircle2 class="h-3 w-3" />
                  <span>Verified Profile</span>
                </span>
              </div>

              <!-- Username & Headline -->
              <div class="flex flex-wrap items-center gap-2 text-xs text-text-muted">
                <span v-if="profile.username" class="font-semibold text-primary">
                  @{{ profile.username }}
                </span>
                <span v-if="profile.username && (profile.current_job_title || profile.current_company)">•</span>
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

                <span class="inline-flex items-center gap-1">
                  <Mail class="h-3.5 w-3.5 text-text-subtle" />
                  <span>{{ profile.email }}</span>
                </span>

                <span v-if="profile.phone" class="inline-flex items-center gap-1">
                  <Phone class="h-3.5 w-3.5 text-text-subtle" />
                  <span>{{ profile.phone }}</span>
                </span>

                <span v-if="profile.created_at" class="inline-flex items-center gap-1">
                  <Calendar class="h-3.5 w-3.5 text-text-subtle" />
                  <span>Joined {{ formatDate(profile.created_at) }}</span>
                </span>
              </div>
            </div>
          </div>
        </div>

        <!-- Primary Resume Card -->
        <div class="bg-card rounded-2xl border border-border p-5 sm:p-6 shadow-sm space-y-4">
          <div class="flex items-center justify-between border-b border-border pb-3">
            <div class="flex items-center gap-2">
              <FileCheck class="h-4 w-4 text-primary" />
              <h2 class="text-xs sm:text-sm font-bold uppercase tracking-wider text-text-muted">
                Primary Resume
              </h2>
            </div>
            <router-link
              to="/resumes"
              class="text-xs font-semibold text-primary hover:text-primary-hover hover:underline inline-flex items-center gap-1 transition"
            >
              <span>Manage All Resumes</span>
              <ExternalLink class="h-3 w-3" />
            </router-link>
          </div>

          <!-- Loading state -->
          <div v-if="loadingResume" class="py-6 text-center text-xs text-text-muted">
            <Loader2 class="h-5 w-5 animate-spin mx-auto text-primary mb-1.5" />
            Loading resume...
          </div>

          <!-- Has Primary Resume -->
          <div
            v-else-if="primaryResume"
            class="flex flex-col sm:flex-row sm:items-center justify-between gap-4 p-4 rounded-xl border border-border bg-surface-raised/40 transition hover:border-border-strong"
          >
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
                class="inline-flex items-center justify-center gap-1.5 px-3 py-1.5 text-xs font-semibold rounded-lg border border-border bg-card text-text-secondary hover:text-text-strong hover:bg-surface-raised transition shadow-xs cursor-pointer"
                title="Download file"
              >
                <Download class="h-3.5 w-3.5" />
                <span class="hidden sm:inline">Download</span>
              </button>
            </div>
          </div>

          <!-- No Resume State -->
          <div
            v-else
            class="p-5 text-center rounded-xl border border-dashed border-border bg-surface-raised/20 space-y-2"
          >
            <FileText class="h-8 w-8 text-text-subtle mx-auto" />
            <p class="text-xs font-semibold text-text-strong">No Primary Resume Uploaded</p>
            <p class="text-[11px] text-text-muted max-w-sm mx-auto">
              Uploading a primary resume enables fast 1-click job applications and displays your credentials on your public profile.
            </p>
            <router-link
              to="/resumes"
              class="inline-flex items-center gap-1.5 px-3.5 py-1.5 text-xs font-semibold rounded-lg text-white bg-primary hover:bg-primary-hover transition shadow-sm mt-1"
            >
              <span>Upload Resume Now</span>
            </router-link>
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

        <!-- Professional & Online Presence Links -->
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

