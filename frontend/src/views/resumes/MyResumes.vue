<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { resumeService } from '@/services/resume.service'
import { uploadResumeFile } from '@/composables/useResumeUpload'
import type { Resume } from '@/types/resume'
import {
  FileText,
  UploadCloud,
  CheckCircle2,
  Trash2,
  Download,
  Star,
  Plus,
  AlertCircle,
  ArrowLeft,
  Loader2,
  X,
  FileCheck,
} from 'lucide-vue-next'
import { toast } from 'vue-sonner'

const router = useRouter()
const auth = useAuthStore()

const resumes = ref<Resume[]>([])
const loading = ref(true)
const uploading = ref(false)
const actionId = ref<string | null>(null)
const errorMsg = ref('')
const successMsg = ref('')

// Form state
const showUploadForm = ref(false)
const newTitle = ref('')
const selectedFile = ref<File | null>(null)
const fileInputRef = ref<HTMLInputElement | null>(null)

// Max resumes limit
const MAX_RESUMES = 5
const canUploadMore = computed(() => resumes.value.length < MAX_RESUMES)

onMounted(loadResumes)

async function loadResumes() {
  if (!auth.user) return
  loading.value = true
  errorMsg.value = ''

  try {
    resumes.value = await resumeService.fetchUserResumes(auth.user.id)
  } catch (err: any) {
    errorMsg.value = err.message || 'Failed to load your resumes.'
    toast.error('Could not load resumes')
  } finally {
    loading.value = false
  }
}

function onFileChange(e: Event) {
  const target = e.target as HTMLInputElement
  const file = target.files?.[0]
  if (!file) return

  errorMsg.value = ''

  const allowedTypes = [
    'application/pdf',
    'application/msword',
    'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
  ]
  const isValidType =
    allowedTypes.includes(file.type) || /\.(pdf|doc|docx)$/i.test(file.name)

  if (!isValidType) {
    errorMsg.value = 'Only PDF, DOC, or DOCX documents are accepted.'
    target.value = ''
    selectedFile.value = null
    return
  }

  // 5 MB limit
  if (file.size > 5 * 1024 * 1024) {
    errorMsg.value = 'Resume file size cannot exceed 5 MB.'
    target.value = ''
    selectedFile.value = null
    return
  }

  selectedFile.value = file
  if (!newTitle.value.trim()) {
    newTitle.value = file.name.replace(/\.[^/.]+$/, '')
  }
}

function cancelUpload() {
  showUploadForm.value = false
  newTitle.value = ''
  selectedFile.value = null
  errorMsg.value = ''
  if (fileInputRef.value) {
    fileInputRef.value.value = ''
  }
}

async function handleUpload() {
  if (!auth.user) {
    toast.error('You must be signed in to upload a resume.')
    return
  }

  if (!selectedFile.value) {
    errorMsg.value = 'Please select a resume file to upload.'
    return
  }

  if (!newTitle.value.trim()) {
    errorMsg.value = 'Please enter a title for your resume.'
    return
  }

  if (!canUploadMore.value) {
    errorMsg.value = `You have reached the maximum limit of ${MAX_RESUMES} resumes.`
    return
  }

  uploading.value = true
  errorMsg.value = ''
  successMsg.value = ''

  try {
    // 1. Upload file to Supabase storage
    const uploadResult = await uploadResumeFile(auth.user.id, selectedFile.value)
    if (!uploadResult) {
      throw new Error('Failed to upload file to storage. Please try again.')
    }

    // 2. Insert resume metadata into database via service
    const isFirst = resumes.value.length === 0
    await resumeService.createResume({
      user_id: auth.user.id,
      title: newTitle.value.trim(),
      file_path: uploadResult.path,
      file_name: uploadResult.fileName,
      file_size: uploadResult.fileSize,
      file_type: uploadResult.fileType,
      is_primary: isFirst,
    })

    toast.success('Resume uploaded successfully!')
    cancelUpload()
    await loadResumes()
  } catch (err: any) {
    errorMsg.value = err.message || 'Failed to complete resume upload.'
    toast.error(errorMsg.value)
  } finally {
    uploading.value = false
  }
}

async function handleSetPrimary(resume: Resume) {
  if (resume.is_primary) return

  actionId.value = resume.id
  try {
    await resumeService.setPrimaryResume(resume.id)
    toast.success(`"${resume.title}" set as primary resume.`)
    await loadResumes()
  } catch (err: any) {
    toast.error(err.message || 'Failed to set primary resume.')
  } finally {
    actionId.value = null
  }
}

async function handleDelete(resume: Resume) {
  if (!confirm(`Are you sure you want to delete "${resume.title}"?`)) return

  actionId.value = resume.id
  try {
    await resumeService.deleteResume(resume)
    toast.success('Resume deleted successfully.')
    await loadResumes()
  } catch (err: any) {
    toast.error(err.message || 'Failed to delete resume.')
  } finally {
    actionId.value = null
  }
}

async function handleDownload(resume: Resume) {
  if (!resume.file_path) {
    toast.error('No file path recorded for this resume.')
    return
  }

  actionId.value = resume.id
  try {
    const url = await resumeService.getDownloadUrl(resume.file_path)
    if (url) {
      window.open(url, '_blank')
    } else {
      toast.error('Could not generate download link. Please try again.')
    }
  } catch (err: any) {
    toast.error(err.message || 'Failed to download resume.')
  } finally {
    actionId.value = null
  }
}

function formatFileSize(bytes: number | null | undefined): string {
  if (!bytes) return '0 B'
  if (bytes < 1024) return bytes + ' B'
  if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB'
  return (bytes / (1024 * 1024)).toFixed(1) + ' MB'
}

function formatDate(dateStr: string): string {
  if (!dateStr) return ''
  try {
    return new Date(dateStr).toLocaleDateString(undefined, {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
    })
  } catch {
    return dateStr
  }
}
</script>

<template>
  <div class="min-h-screen bg-page py-4 px-3 sm:py-6 sm:px-4 md:py-8 md:px-6 lg:py-10 lg:px-8">
    <div class="max-w-4xl mx-auto space-y-5 sm:space-y-6">
      <!-- Back Button -->
      <button
        type="button"
        @click="router.back()"
        class="inline-flex items-center gap-1.5 text-xs sm:text-sm font-medium text-text-muted hover:text-text-strong transition"
      >
        <ArrowLeft class="h-3.5 w-3.5 sm:h-4 sm:w-4" />
        <span>Back</span>
      </button>

      <!-- Page Header -->
      <div
        class="bg-card rounded-xl sm:rounded-2xl border border-border p-5 sm:p-6 md:p-8 shadow-sm flex flex-col sm:flex-row sm:items-center justify-between gap-4"
      >
        <div class="space-y-1">
          <div class="flex items-center gap-2">
            <div
              class="h-9 w-9 sm:h-10 sm:w-10 rounded-xl bg-primary-light flex items-center justify-center text-primary"
            >
              <FileText class="h-5 w-5 sm:h-5 sm:w-5" />
            </div>
            <h1 class="text-xl sm:text-2xl font-bold text-text-strong tracking-tight">
              My Resumes
            </h1>
          </div>
          <p class="text-xs sm:text-sm text-text-muted pl-11">
            Store and manage up to {{ MAX_RESUMES }} tailored resumes for your job applications.
          </p>
        </div>

        <div class="flex items-center gap-3 self-start sm:self-center pl-11 sm:pl-0">
          <span
            class="text-xs font-semibold px-2.5 py-1 rounded-full border border-border bg-surface-raised text-text-secondary"
          >
            {{ resumes.length }} / {{ MAX_RESUMES }} used
          </span>

          <button
            v-if="canUploadMore && !showUploadForm"
            type="button"
            @click="showUploadForm = true"
            class="inline-flex items-center gap-1.5 px-3.5 py-2 text-xs sm:text-sm font-semibold rounded-lg text-white bg-primary hover:bg-primary-hover transition shadow-sm"
          >
            <Plus class="h-4 w-4" />
            <span>Upload Resume</span>
          </button>
        </div>
      </div>

      <!-- Upload Form Modal / Panel -->
      <div
        v-if="showUploadForm"
        class="bg-card rounded-xl sm:rounded-2xl border border-border p-5 sm:p-6 md:p-8 shadow-sm space-y-5 animate-in fade-in duration-200"
      >
        <div class="flex items-center justify-between border-b border-border pb-4">
          <div class="flex items-center gap-2">
            <UploadCloud class="h-5 w-5 text-primary" />
            <h2 class="text-sm sm:text-base font-bold text-text-strong">
              Upload a New Resume
            </h2>
          </div>
          <button
            type="button"
            @click="cancelUpload"
            class="text-text-muted hover:text-text-strong p-1 rounded-lg hover:bg-surface-raised transition"
          >
            <X class="h-4 w-4" />
          </button>
        </div>

        <!-- Feedback Alert -->
        <div
          v-if="errorMsg"
          class="alert alert-error p-3 sm:p-3.5 text-xs font-medium rounded-lg flex items-center gap-2"
        >
          <AlertCircle class="h-4 w-4 shrink-0" />
          <span>{{ errorMsg }}</span>
        </div>

        <div class="space-y-4">
          <!-- Resume Title -->
          <div class="space-y-1.5">
            <label
              for="resume-title"
              class="block text-[11px] sm:text-xs font-bold uppercase tracking-wider text-text-secondary"
            >
              Resume Title <span class="text-danger">*</span>
            </label>
            <input
              id="resume-title"
              v-model="newTitle"
              type="text"
              placeholder="e.g. Senior Frontend Engineer (Vue & React)"
              class="block w-full rounded-lg sm:rounded-xl border border-border bg-page px-3.5 py-2.5 text-xs sm:text-sm text-text-strong placeholder:text-text-subtle shadow-sm focus:ring-1 focus:ring-primary focus:border-primary"
            />
          </div>

          <!-- File Input Drop Zone -->
          <div class="space-y-1.5">
            <label
              class="block text-[11px] sm:text-xs font-bold uppercase tracking-wider text-text-secondary"
            >
              Document File <span class="text-danger">*</span>
            </label>

            <div
              class="border-2 border-dashed border-border rounded-xl p-5 text-center hover:border-primary/50 transition cursor-pointer bg-surface-raised"
              @click="fileInputRef?.click()"
            >
              <input
                ref="fileInputRef"
                type="file"
                accept=".pdf,.doc,.docx,application/pdf,application/msword,application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                class="hidden"
                @change="onFileChange"
              />

              <div class="space-y-2">
                <div
                  class="h-10 w-10 mx-auto rounded-full bg-primary-light text-primary flex items-center justify-center"
                >
                  <FileCheck v-if="selectedFile" class="h-5 w-5" />
                  <UploadCloud v-else class="h-5 w-5" />
                </div>

                <div v-if="selectedFile" class="space-y-1">
                  <p class="text-xs sm:text-sm font-semibold text-text-strong">
                    {{ selectedFile.name }}
                  </p>
                  <p class="text-[11px] text-text-muted">
                    {{ formatFileSize(selectedFile.size) }} • Click to change file
                  </p>
                </div>

                <div v-else class="space-y-1">
                  <p class="text-xs sm:text-sm font-semibold text-text-strong">
                    Click to browse or drop your resume here
                  </p>
                  <p class="text-[11px] text-text-muted">
                    Supported formats: PDF, DOC, DOCX (Maximum file size: 5 MB)
                  </p>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Form Actions -->
        <div class="flex items-center justify-end gap-3 pt-3 border-t border-border">
          <button
            type="button"
            :disabled="uploading"
            @click="cancelUpload"
            class="px-4 py-2 text-xs sm:text-sm font-semibold rounded-lg border border-border text-text-secondary hover:text-text-strong hover:bg-surface-raised transition"
          >
            Cancel
          </button>

          <button
            type="button"
            :disabled="uploading || !selectedFile || !newTitle.trim()"
            @click="handleUpload"
            class="inline-flex items-center justify-center gap-2 px-5 py-2 text-xs sm:text-sm font-semibold rounded-lg text-white bg-primary hover:bg-primary-hover disabled:opacity-50 transition shadow-sm"
          >
            <Loader2 v-if="uploading" class="h-4 w-4 animate-spin" />
            <span>{{ uploading ? 'Uploading...' : 'Save & Upload' }}</span>
          </button>
        </div>
      </div>

      <!-- Resumes List -->
      <div v-if="loading" class="bg-card rounded-xl sm:rounded-2xl border border-border p-12 text-center">
        <div
          class="h-8 w-8 animate-spin rounded-full border-2 border-primary border-t-transparent mx-auto mb-3"
        ></div>
        <p class="text-xs sm:text-sm text-text-muted">Loading your resumes...</p>
      </div>

      <!-- Empty State -->
      <div
        v-else-if="resumes.length === 0 && !showUploadForm"
        class="bg-card rounded-xl sm:rounded-2xl border border-border p-10 sm:p-14 text-center space-y-4 shadow-sm"
      >
        <div
          class="h-14 w-14 rounded-2xl bg-surface-raised text-text-subtle flex items-center justify-center mx-auto"
        >
          <FileText class="h-7 w-7" />
        </div>
        <div class="space-y-1">
          <h3 class="text-base sm:text-lg font-bold text-text-strong">
            No Resumes Uploaded Yet
          </h3>
          <p class="text-xs sm:text-sm text-text-muted max-w-md mx-auto">
            Upload your resume now so you can apply to jobs with a single click. You can add up to
            {{ MAX_RESUMES }} different versions.
          </p>
        </div>
        <button
          type="button"
          @click="showUploadForm = true"
          class="inline-flex items-center gap-1.5 px-4 py-2 text-xs sm:text-sm font-semibold rounded-lg text-white bg-primary hover:bg-primary-hover transition shadow-sm"
        >
          <Plus class="h-4 w-4" />
          <span>Upload Your First Resume</span>
        </button>
      </div>

      <!-- List of Resumes -->
      <div v-else class="space-y-3.5 sm:space-y-4">
        <div
          v-for="resume in resumes"
          :key="resume.id"
          class="bg-card rounded-xl sm:rounded-2xl border border-border p-4 sm:p-5 md:p-6 shadow-sm hover:border-border-strong transition flex flex-col md:flex-row md:items-center justify-between gap-4"
        >
          <!-- Left: Info -->
          <div class="flex items-start gap-3.5 min-w-0">
            <div
              class="h-10 w-10 sm:h-11 sm:w-11 rounded-xl flex items-center justify-center shrink-0"
              :class="
                resume.is_primary
                  ? 'bg-primary text-white shadow-sm'
                  : 'bg-surface-raised text-text-secondary border border-border'
              "
            >
              <FileText class="h-5 w-5" />
            </div>

            <div class="min-w-0 flex-1 space-y-1">
              <div class="flex flex-wrap items-center gap-2">
                <h3 class="text-sm sm:text-base font-bold text-text-strong truncate">
                  {{ resume.title }}
                </h3>
                <span
                  v-if="resume.is_primary"
                  class="inline-flex items-center gap-1 px-2 py-0.5 text-[10px] sm:text-xs font-semibold rounded-full bg-primary-light text-primary border border-primary/20"
                >
                  <Star class="h-3 w-3 fill-current" />
                  <span>Primary</span>
                </span>
              </div>

              <div
                class="flex flex-wrap items-center gap-x-3 gap-y-1 text-xs text-text-muted"
              >
                <span class="truncate">{{ resume.file_name || 'Document' }}</span>
                <span v-if="resume.file_size" class="text-text-subtle">•</span>
                <span v-if="resume.file_size">{{ formatFileSize(resume.file_size) }}</span>
                <span class="text-text-subtle">•</span>
                <span>Uploaded {{ formatDate(resume.created_at) }}</span>
              </div>
            </div>
          </div>

          <!-- Right: Actions -->
          <div
            class="flex items-center gap-2 self-end md:self-center shrink-0 pt-2 md:pt-0 border-t md:border-t-0 border-border w-full md:w-auto justify-end"
          >
            <!-- Download Button -->
            <button
              type="button"
              :disabled="actionId === resume.id"
              @click="handleDownload(resume)"
              class="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold rounded-lg border border-border bg-card text-text-secondary hover:text-text-strong hover:bg-surface-raised transition"
              title="Download or preview resume"
            >
              <Loader2 v-if="actionId === resume.id" class="h-3.5 w-3.5 animate-spin" />
              <Download v-else class="h-3.5 w-3.5" />
              <span>Download</span>
            </button>

            <!-- Set as Primary -->
            <button
              v-if="!resume.is_primary"
              type="button"
              :disabled="actionId === resume.id"
              @click="handleSetPrimary(resume)"
              class="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold rounded-lg bg-primary-light text-primary border border-primary/30 hover:bg-primary/20 transition"
              title="Set as your default primary resume for quick applying"
            >
              <Star class="h-3.5 w-3.5" />
              <span>Make Primary</span>
            </button>

            <!-- Delete Button -->
            <button
              type="button"
              :disabled="actionId === resume.id"
              @click="handleDelete(resume)"
              class="inline-flex items-center gap-1.5 px-2.5 py-1.5 text-xs font-semibold rounded-lg alert-error hover:bg-danger-bg transition"
              title="Delete this resume"
            >
              <Trash2 class="h-3.5 w-3.5" />
              <span class="sr-only sm:not-sr-only">Delete</span>
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
