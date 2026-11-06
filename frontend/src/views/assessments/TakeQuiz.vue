<script setup lang="ts">
import { ref, onMounted, computed, onUnmounted } from 'vue'
import { useRoute, useRouter, onBeforeRouteLeave } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { assessmentService } from '@/services/assessment.service'
import { supabase } from '@/lib/supabase'
import type { QuizAttempt, CandidateQuestion } from '@/types/assessment'
import {
  Clock,
  CheckCircle2,
  AlertCircle,
  HelpCircle,
  Send,
  ArrowLeft,
  Check,
  FileText,
  RotateCcw,
  Play,
  Layers,
  Award,
  Sparkles,
  BookOpen,
  Info,
  Shield,
  ShieldAlert,
  ShieldCheck,
  Lock,
  Maximize,
  AlertTriangle
} from 'lucide-vue-next'
import { toast } from 'vue-sonner'
import { useConfirm } from '@/composables/useConfirm'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const { confirm: showConfirm } = useConfirm()

const jobId = route.params.jobId as string
const processId = route.params.processId as string
const applicationId = route.query.appId as string | undefined

const loading = ref(true)
const startingQuiz = ref(false)
const submitting = ref(false)
const errorMsg = ref('')

// Pre-quiz overview information
interface StageInfo {
  title: string
  description: string
  timeLimitMinutes: number | null
  questionBankId: string | null
  jobTitle: string
  questionCount: number
}
const stageInfo = ref<StageInfo | null>(null)
const hasStarted = ref(false)
const isResumingAttempt = ref(false)

const attempt = ref<QuizAttempt | null>(null)
const questions = ref<CandidateQuestion[]>([])

// Candidate answers: map of questionId -> answer value
const answers = ref<Record<string, any>>({})

// Timer
const timeRemainingSeconds = ref<number | null>(null)
let timerInterval: any = null

const formattedTime = computed(() => {
  if (timeRemainingSeconds.value === null) return null
  const mins = Math.floor(timeRemainingSeconds.value / 60)
  const secs = timeRemainingSeconds.value % 60
  return `${mins}:${secs < 10 ? '0' : ''}${secs}`
})

const isTimeExpiringSoon = computed(() => {
  return timeRemainingSeconds.value !== null && timeRemainingSeconds.value < 120
})

// =====================================================================
// SAFE EXAM BROWSER RULES & CONFIRMATION STATE
// =====================================================================
const agreeTimer = ref(false)
const agreeFullscreen = ref(false)
const agreeRestrictions = ref(false)
const agreeInfractions = ref(false)

const allRulesAgreed = computed(() => {
  return agreeTimer.value && agreeFullscreen.value && agreeRestrictions.value && agreeInfractions.value
})

function toggleAgreeAll() {
  const newVal = !allRulesAgreed.value
  agreeTimer.value = newVal
  agreeFullscreen.value = newVal
  agreeRestrictions.value = newVal
  agreeInfractions.value = newVal
}

// =====================================================================
// SAFE EXAM LOCKDOWN & PROCTORING STATE
// =====================================================================
const isFullscreen = ref(false)
const infractionCount = ref(0)
const MAX_INFRACTIONS = 3
const showInfractionModal = ref(false)
const lastInfractionReason = ref('')
const showFullscreenModal = ref(false)

// History trap listener
function onPopState() {
  if (hasStarted.value && attempt.value?.status === 'in_progress') {
    history.pushState(null, '', window.location.href)
    toast.error('Browser navigation is disabled in Safe Exam mode.')
  }
}

// Fullscreen change listener
function onFullscreenChange() {
  const isCurrentlyFullscreen = !!document.fullscreenElement
  isFullscreen.value = isCurrentlyFullscreen

  if (hasStarted.value && attempt.value?.status === 'in_progress' && !isCurrentlyFullscreen) {
    recordInfraction('Exited Fullscreen Mode')
    showFullscreenModal.value = true
  }
}

// Window blur & Visibility change listeners
function onWindowBlur() {
  if (hasStarted.value && attempt.value?.status === 'in_progress') {
    recordInfraction('Window Focus Lost (Tab or App Switch)')
  }
}

function onVisibilityChange() {
  if (document.hidden && hasStarted.value && attempt.value?.status === 'in_progress') {
    recordInfraction('Tab Switch or Minimized Window Detected')
  }
}

// Clipboard & Context menu blockers
function onContextMenu(e: MouseEvent) {
  if (hasStarted.value && attempt.value?.status === 'in_progress') {
    e.preventDefault()
    toast.warning('Right-click context menu is disabled in Safe Exam mode.')
  }
}

function onCopyCutPaste(e: ClipboardEvent) {
  if (hasStarted.value && attempt.value?.status === 'in_progress') {
    e.preventDefault()
    toast.warning('Copy, cut, and paste operations are blocked in Safe Exam mode.')
  }
}

// Keyboard shortcuts blocker
function onKeyDown(e: KeyboardEvent) {
  if (!hasStarted.value || attempt.value?.status !== 'in_progress') return

  // Block Function keys (F12, F5, F11)
  if (e.key === 'F12' || e.key === 'F5' || e.key === 'F11') {
    e.preventDefault()
    toast.error('Function keys are disabled in Safe Exam mode.')
    return
  }

  // Block Ctrl/Command combinations
  if (e.ctrlKey || e.metaKey) {
    const key = e.key.toLowerCase()
    // c, v, x (clipboard), a (select all), p (print), s (save), u (view source), r (refresh), w (close tab)
    if (['c', 'v', 'x', 'a', 'p', 's', 'u', 'r', 'w'].includes(key)) {
      e.preventDefault()
      toast.warning(`Shortcut Ctrl+${key.toUpperCase()} is disabled in Safe Exam mode.`)
      return
    }

    // DevTools: Ctrl+Shift+I, Ctrl+Shift+J, Ctrl+Shift+C
    if (e.shiftKey && ['i', 'j', 'c'].includes(key)) {
      e.preventDefault()
      toast.error('Developer tools shortcuts are disabled.')
      return
    }
  }

  // Block Alt + Arrow navigation
  if (e.altKey && (e.key === 'ArrowLeft' || e.key === 'ArrowRight')) {
    e.preventDefault()
    return
  }
}

// Browser reload / leave guard
function onBeforeUnload(e: BeforeUnloadEvent) {
  if (hasStarted.value && attempt.value?.status === 'in_progress') {
    e.preventDefault()
    e.returnValue = 'Assessment in progress! Your answers will be submitted if you leave.'
    return e.returnValue
  }
}

function recordInfraction(reason: string) {
  infractionCount.value++
  lastInfractionReason.value = reason

  if (infractionCount.value >= MAX_INFRACTIONS) {
    toast.error(`Maximum infractions reached (${MAX_INFRACTIONS}/${MAX_INFRACTIONS})! Auto-submitting assessment...`)
    submitQuiz(true, 'Maximum security infractions reached (3/3). Safe Exam automatically submitted your responses.')
  } else {
    showInfractionModal.value = true
  }
}

async function requestFullscreenMode() {
  try {
    if (!document.fullscreenElement) {
      await document.documentElement.requestFullscreen()
      isFullscreen.value = true
    }
  } catch (err) {
    console.warn('Fullscreen request blocked or not supported:', err)
  }
}

function exitFullscreenMode() {
  try {
    if (document.fullscreenElement) {
      document.exitFullscreen()
      isFullscreen.value = false
    }
  } catch (err) {
    console.warn('Error exiting fullscreen:', err)
  }
}

function setupSafeExamListeners() {
  window.addEventListener('blur', onWindowBlur)
  document.addEventListener('visibilitychange', onVisibilityChange)
  document.addEventListener('fullscreenchange', onFullscreenChange)
  window.addEventListener('contextmenu', onContextMenu)
  window.addEventListener('copy', onCopyCutPaste)
  window.addEventListener('cut', onCopyCutPaste)
  window.addEventListener('paste', onCopyCutPaste)
  window.addEventListener('keydown', onKeyDown)
  window.addEventListener('beforeunload', onBeforeUnload)
  window.addEventListener('popstate', onPopState)

  history.pushState(null, '', window.location.href)
}

function cleanupSafeExamListeners() {
  window.removeEventListener('blur', onWindowBlur)
  document.removeEventListener('visibilitychange', onVisibilityChange)
  document.removeEventListener('fullscreenchange', onFullscreenChange)
  window.removeEventListener('contextmenu', onContextMenu)
  window.removeEventListener('copy', onCopyCutPaste)
  window.removeEventListener('cut', onCopyCutPaste)
  window.removeEventListener('paste', onCopyCutPaste)
  window.removeEventListener('keydown', onKeyDown)
  window.removeEventListener('beforeunload', onBeforeUnload)
  window.removeEventListener('popstate', onPopState)
}

// Vue Router Navigation Guard
onBeforeRouteLeave(async (to, from, next) => {
  if (hasStarted.value && attempt.value && attempt.value.status === 'in_progress') {
    const confirmed = await showConfirm({
      title: 'Exit Safe Exam Environment?',
      message: 'Your assessment is currently in progress. If you leave this page, your time will continue running or your attempt may be invalidated. Are you sure you want to exit?',
      confirmLabel: 'Leave Assessment',
      cancelLabel: 'Stay in Exam',
      variant: 'danger'
    })
    if (confirmed) {
      cleanupSafeExamListeners()
      exitFullscreenMode()
      next()
    } else {
      next(false)
    }
  } else {
    cleanupSafeExamListeners()
    exitFullscreenMode()
    next()
  }
})

onMounted(async () => {
  if (!auth.user) {
    router.push({ name: 'login', query: { redirect: route.fullPath } })
    return
  }
  await initAssessment()
})

onUnmounted(() => {
  if (timerInterval) clearInterval(timerInterval)
  cleanupSafeExamListeners()
  exitFullscreenMode()
})

function startTimer() {
  if (timerInterval) clearInterval(timerInterval)
  if (timeRemainingSeconds.value === null || timeRemainingSeconds.value <= 0) return

  timerInterval = setInterval(() => {
    if (timeRemainingSeconds.value !== null) {
      timeRemainingSeconds.value--
      if (timeRemainingSeconds.value <= 0) {
        clearInterval(timerInterval)
        handleTimeExpired()
      }
    }
  }, 1000)
}

async function initAssessment() {
  loading.value = true
  errorMsg.value = ''

  try {
    // 1. Fetch stage details and job title
    const { data: proc, error: procErr } = await supabase
      .from('job_selection_processes')
      .select('*, job:jobs(id, title)')
      .eq('id', processId)
      .single()

    if (procErr || !proc) {
      errorMsg.value = 'Assessment stage could not be found.'
      loading.value = false
      return
    }

    const bankId = proc.question_bank_id || proc.config?.question_bank_id || null

    // Determine question count in the attached bank
    let qCount = 0
    if (bankId) {
      const { count } = await supabase
        .from('questions')
        .select('id', { count: 'exact', head: true })
        .eq('bank_id', bankId)
        .is('deleted_at', null)
      qCount = count || 0
    }

    stageInfo.value = {
      title: proc.title || 'Quiz Assessment',
      description: proc.description || '',
      timeLimitMinutes: proc.time_limit_minutes ?? null,
      questionBankId: bankId,
      jobTitle: proc.job?.title || 'Job Opportunity',
      questionCount: qCount
    }

    // 2. Check if candidate already has an existing attempt
    const existing = await assessmentService.getStageAttempt(processId, auth.user!.id)

    if (existing) {
      attempt.value = existing
      questions.value = existing.questions || []

      // If already submitted or graded, display result card directly
      if (existing.status === 'submitted' || existing.status === 'graded') {
        hasStarted.value = true
        loading.value = false
        return
      }

      // If in progress
      if (existing.status === 'in_progress') {
        if (existing.expires_at) {
          const expiresAtMs = new Date(existing.expires_at).getTime()
          const diffSecs = Math.max(0, Math.floor((expiresAtMs - Date.now()) / 1000))
          if (diffSecs > 0) {
            timeRemainingSeconds.value = diffSecs
            isResumingAttempt.value = true
            // DO NOT automatically start timer or set hasStarted = true!
            // Candidate must acknowledge the Safe Exam confirmation gate first!
            hasStarted.value = false
            loading.value = false
            return
          } else {
            // Already expired
            attempt.value = { ...existing, status: 'expired' }
            hasStarted.value = true
            loading.value = false
            return
          }
        } else {
          // Untimed attempt
          isResumingAttempt.value = true
          hasStarted.value = false
          loading.value = false
          return
        }
      }
    }

    // No active attempt - stay on the pre-assessment briefing screen so timer does NOT start
    hasStarted.value = false
  } catch (err: any) {
    errorMsg.value = err.message || 'Failed to initialize assessment. Please ensure a valid question bank is attached.'
  } finally {
    loading.value = false
  }
}

async function handleEnterSafeExam() {
  if (!auth.user || !allRulesAgreed.value) return

  startingQuiz.value = true
  errorMsg.value = ''

  try {
    // 1. If not resuming, start the attempt via API
    if (!isResumingAttempt.value || !attempt.value) {
      const data = await assessmentService.startAssessment(auth.user.id, {
        job_id: jobId,
        selection_process_id: processId,
        application_id: applicationId || null
      })

      attempt.value = data
      questions.value = data.questions || []

      if (data.expires_at) {
        const expiresAtMs = new Date(data.expires_at).getTime()
        const nowMs = Date.now()
        const diffSecs = Math.max(0, Math.floor((expiresAtMs - nowMs) / 1000))
        timeRemainingSeconds.value = diffSecs
      }
    }

    // 2. Request Fullscreen
    await requestFullscreenMode()

    // 3. Setup lockdown listeners
    setupSafeExamListeners()

    // 4. Activate exam interface & start timer
    hasStarted.value = true

    if (timeRemainingSeconds.value !== null && timeRemainingSeconds.value > 0) {
      startTimer()
    } else if (timeRemainingSeconds.value !== null && timeRemainingSeconds.value <= 0) {
      handleTimeExpired()
    }

    toast.success('Safe Exam Environment Active! Fullscreen lockdown enabled.')
  } catch (err: any) {
    errorMsg.value = err.message || 'Failed to start assessment. Please ensure a valid question bank is attached.'
  } finally {
    startingQuiz.value = false
  }
}

function handleTimeExpired() {
  toast.error('Time limit reached! Submitting answers automatically...')
  submitQuiz(true, 'Time expired. Assessment submitted automatically.')
}

function selectSingleChoice(qId: string, optId: string) {
  answers.value[qId] = optId
}

function toggleMultipleChoice(qId: string, optId: string) {
  const current: string[] = Array.isArray(answers.value[qId]) ? [...answers.value[qId]] : []
  const idx = current.indexOf(optId)
  if (idx === -1) {
    current.push(optId)
  } else {
    current.splice(idx, 1)
  }
  answers.value[qId] = current
}

function isMultipleChoiceChecked(qId: string, optId: string): boolean {
  const current = answers.value[qId]
  return Array.isArray(current) && current.includes(optId)
}

const answeredCount = computed(() => {
  return questions.value.filter(q => {
    const val = answers.value[q.id]
    if (val === undefined || val === null || val === '') return false
    if (Array.isArray(val) && val.length === 0) return false
    return true
  }).length
})

const progressPercentage = computed(() => {
  if (questions.value.length === 0) return 0
  return Math.round((answeredCount.value / questions.value.length) * 100)
})

async function submitQuiz(auto = false, autoReason = '') {
  if (!attempt.value || !auth.user) return

  if (!auto && questions.value.length > 0 && answeredCount.value < questions.value.length) {
    const unanswered = questions.value.length - answeredCount.value
    const confirmed = await showConfirm({
      title: 'Submit Assessment?',
      message: `You still have ${unanswered} unanswered question(s).\n\nAre you sure you want to finish and submit your quiz now?`,
      confirmLabel: 'Yes, Submit',
      cancelLabel: 'Keep Reviewing',
      variant: 'warning',
    })
    if (!confirmed) {
      return
    }
  }

  submitting.value = true
  if (timerInterval) clearInterval(timerInterval)

  try {
    const answerPayload = Object.entries(answers.value).map(([qId, ans]) => ({
      question_id: qId,
      answer: ans
    }))

    const result = await assessmentService.submitAssessment(auth.user.id, {
      attempt_id: attempt.value.id,
      answers: answerPayload
    })

    attempt.value = result
    cleanupSafeExamListeners()
    exitFullscreenMode()

    if (autoReason) {
      toast.info(autoReason)
    } else {
      toast.success('Assessment submitted successfully!')
    }
  } catch (err: any) {
    toast.error(err.message || 'Failed to submit assessment')
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <div class="min-h-screen bg-page py-6 sm:py-10 px-4 sm:px-6 lg:px-8">
    <div class="max-w-3xl mx-auto space-y-6">

      <!-- Loading State -->
      <div v-if="loading" class="py-24 text-center">
        <div class="h-9 w-9 animate-spin rounded-full border-2 border-primary border-t-transparent mx-auto mb-3"></div>
        <p class="text-xs sm:text-sm text-text-muted">Initializing assessment...</p>
      </div>

      <!-- Error State -->
      <div v-else-if="errorMsg" class="bg-card border border-danger/20 rounded-2xl p-8 text-center space-y-4 shadow-xs">
        <AlertCircle class="h-10 w-10 text-danger mx-auto" />
        <h2 class="text-base sm:text-lg font-bold text-text-strong">Cannot Access Assessment</h2>
        <p class="text-xs sm:text-sm text-text-muted max-w-md mx-auto">{{ errorMsg }}</p>
        <button
          type="button"
          @click="router.push(`/jobs/${jobId}`)"
          class="inline-flex items-center gap-1.5 px-4 py-2 text-xs font-semibold rounded-lg bg-primary text-white hover:bg-primary-hover shadow-xs transition"
        >
          <ArrowLeft class="h-4 w-4" />
          <span>Return to Job Details</span>
        </button>
      </div>

      <!-- COMPLETED / GRADED RESULT VIEW -->
      <div v-else-if="attempt && (attempt.status === 'submitted' || attempt.status === 'graded')" class="space-y-6">
        <div class="bg-card border border-border rounded-2xl p-6 sm:p-8 text-center shadow-xs space-y-4">
          <div
            :class="[
              'h-16 w-16 rounded-full mx-auto flex items-center justify-center',
              attempt.passed
                ? 'bg-success-bg text-success-text border border-success-border'
                : 'bg-primary-light text-primary border border-primary/20'
            ]"
          >
            <CheckCircle2 class="h-8 w-8" />
          </div>

          <div>
            <h2 class="text-xl sm:text-2xl font-bold text-text-strong">Assessment Completed!</h2>
            <p class="text-xs sm:text-sm text-text-muted mt-1">
              Your responses for <strong>{{ attempt.stage_title }}</strong> have been recorded.
            </p>
          </div>

          <!-- Score Card -->
          <div class="grid grid-cols-2 sm:grid-cols-4 gap-3 max-w-lg mx-auto py-4">
            <div class="p-3 bg-surface-raised rounded-xl border border-border text-center">
              <span class="block text-[11px] text-text-muted uppercase tracking-wider font-semibold">Your Score</span>
              <span class="text-lg font-bold text-text-strong">{{ attempt.score }}</span>
            </div>
            <div class="p-3 bg-surface-raised rounded-xl border border-border text-center">
              <span class="block text-[11px] text-text-muted uppercase tracking-wider font-semibold">Total Points</span>
              <span class="text-lg font-bold text-text-strong">{{ attempt.max_score }}</span>
            </div>
            <div class="p-3 bg-surface-raised rounded-xl border border-border text-center">
              <span class="block text-[11px] text-text-muted uppercase tracking-wider font-semibold">Percentage</span>
              <span class="text-lg font-bold text-primary">{{ attempt.percentage }}%</span>
            </div>
            <div class="p-3 bg-surface-raised rounded-xl border border-border text-center">
              <span class="block text-[11px] text-text-muted uppercase tracking-wider font-semibold">Status</span>
              <span
                :class="[
                  'text-xs font-bold uppercase tracking-wider',
                  attempt.passed ? 'text-success-text' : 'text-primary'
                ]"
              >
                {{ attempt.status === 'submitted' ? 'Under Review' : (attempt.passed ? 'Passed' : 'Completed') }}
              </span>
            </div>
          </div>

          <div class="pt-4 flex justify-center gap-3">
            <button
              type="button"
              @click="router.push(`/jobs/${jobId}`)"
              class="px-5 py-2.5 text-xs sm:text-sm font-semibold rounded-lg bg-primary text-white hover:bg-primary-hover shadow-xs transition"
            >
              Return to Job Details
            </button>
          </div>
        </div>
      </div>

      <!-- EXPIRED VIEW -->
      <div v-else-if="attempt && attempt.status === 'expired'" class="space-y-6">
        <div class="bg-card border border-danger/30 rounded-2xl p-6 sm:p-8 text-center shadow-xs space-y-4">
          <div class="h-14 w-14 rounded-full mx-auto flex items-center justify-center bg-danger/10 text-danger border border-danger/20">
            <Clock class="h-7 w-7" />
          </div>
          <div>
            <h2 class="text-lg sm:text-xl font-bold text-text-strong">Assessment Time Expired</h2>
            <p class="text-xs sm:text-sm text-text-muted mt-1 max-w-md mx-auto">
              The time limit for this assessment stage has passed. Please contact the hiring team if you require an extension.
            </p>
          </div>
          <div class="pt-3">
            <button
              type="button"
              @click="router.push(`/jobs/${jobId}`)"
              class="px-4 py-2 text-xs font-semibold rounded-lg bg-primary text-white hover:bg-primary-hover shadow-xs transition"
            >
              Return to Job Details
            </button>
          </div>
        </div>
      </div>

      <!-- =================================================================== -->
      <!-- PRE-ASSESSMENT BRIEFING & SAFE EXAM BROWSER AGREEMENT GATE          -->
      <!-- (Timer NEVER starts here! Requires explicit confirmation)            -->
      <!-- =================================================================== -->
      <div v-else-if="!hasStarted && stageInfo" class="space-y-6">
        <!-- Navigation breadcrumb -->
        <button
          type="button"
          @click="router.push(`/jobs/${jobId}`)"
          class="inline-flex items-center gap-1.5 text-xs font-medium text-text-muted hover:text-text-strong transition"
        >
          <ArrowLeft class="h-3.5 w-3.5" />
          <span>Back to Job Details</span>
        </button>

        <div class="bg-card border border-border rounded-2xl p-6 sm:p-8 shadow-sm space-y-6">
          
          <!-- Header Banner -->
          <div class="border-b border-border pb-5">
            <div class="flex flex-wrap items-center justify-between gap-2 mb-2">
              <div class="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-[11px] font-bold bg-primary text-white uppercase tracking-wider shadow-xs">
                <Shield class="h-3.5 w-3.5" /> Safe Exam Browser Required
              </div>
              <span v-if="isResumingAttempt" class="inline-flex items-center gap-1 text-xs font-semibold text-amber-500 bg-amber-500/10 px-2.5 py-1 rounded-full border border-amber-500/20">
                <RotateCcw class="h-3.5 w-3.5" /> Ongoing Session In Progress
              </span>
            </div>

            <h1 class="text-xl sm:text-2xl font-bold text-text-strong">{{ stageInfo.title }}</h1>
            <p class="text-xs sm:text-sm text-text-muted mt-1">
              Position: <span class="font-semibold text-text-strong">{{ stageInfo.jobTitle }}</span>
            </p>

            <p v-if="stageInfo.description" class="text-xs sm:text-sm text-text-secondary mt-3 leading-relaxed bg-surface-raised p-3.5 rounded-xl border border-border">
              {{ stageInfo.description }}
            </p>
          </div>

          <!-- Key Assessment Metrics -->
          <div class="grid grid-cols-1 sm:grid-cols-3 gap-3">
            <!-- Time Limit -->
            <div class="p-4 rounded-xl border border-border bg-surface-raised flex items-center gap-3">
              <div class="h-10 w-10 rounded-lg bg-primary/10 text-primary flex items-center justify-center shrink-0">
                <Clock class="h-5 w-5" />
              </div>
              <div>
                <span class="block text-[11px] font-semibold text-text-muted uppercase tracking-wider">Time Limit</span>
                <span class="text-sm font-bold text-text-strong">
                  {{ formattedTime ? formattedTime + ' Remaining' : (stageInfo.timeLimitMinutes ? `${stageInfo.timeLimitMinutes} Minutes` : 'Unlimited') }}
                </span>
              </div>
            </div>

            <!-- Question Count -->
            <div class="p-4 rounded-xl border border-border bg-surface-raised flex items-center gap-3">
              <div class="h-10 w-10 rounded-lg bg-primary/10 text-primary flex items-center justify-center shrink-0">
                <FileText class="h-5 w-5" />
              </div>
              <div>
                <span class="block text-[11px] font-semibold text-text-muted uppercase tracking-wider">Total Questions</span>
                <span class="text-sm font-bold text-text-strong">
                  {{ stageInfo.questionCount > 0 ? `${stageInfo.questionCount} Questions` : 'Questions Ready' }}
                </span>
              </div>
            </div>

            <!-- Security Mode -->
            <div class="p-4 rounded-xl border border-border bg-surface-raised flex items-center gap-3">
              <div class="h-10 w-10 rounded-lg bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 flex items-center justify-center shrink-0">
                <ShieldCheck class="h-5 w-5" />
              </div>
              <div>
                <span class="block text-[11px] font-semibold text-text-muted uppercase tracking-wider">Security</span>
                <span class="text-sm font-bold text-text-strong">Lockdown Proctoring</span>
              </div>
            </div>
          </div>

          <!-- Safe Exam Environment Rules Card -->
          <div class="p-5 rounded-xl border border-primary/30 bg-primary-light/40 space-y-4">
            <div class="flex items-center justify-between">
              <h2 class="text-xs sm:text-sm font-bold text-text-strong flex items-center gap-2">
                <Lock class="h-4 w-4 text-primary shrink-0" /> Safe Exam Browser Lockdown Rules
              </h2>
              <button
                type="button"
                @click="toggleAgreeAll"
                class="text-xs font-semibold text-primary hover:underline cursor-pointer select-none"
              >
                {{ allRulesAgreed ? 'Deselect All' : 'Acknowledge All Rules' }}
              </button>
            </div>

            <p class="text-xs text-text-secondary leading-relaxed">
              This assessment runs inside an isolated <strong>Safe Exam Browser</strong> lockdown. To maintain exam integrity, the following security policies are enforced:
            </p>

            <!-- Confirmation Checkbox List -->
            <div class="space-y-3 pt-1">
              <!-- Rule 1: Continuous Timer -->
              <label class="flex items-start gap-3 p-3 rounded-lg border border-border bg-card cursor-pointer hover:bg-surface-raised transition">
                <input
                  v-model="agreeTimer"
                  type="checkbox"
                  class="mt-0.5 h-4 w-4 rounded border-border text-primary focus:ring-2 focus:ring-primary/20 cursor-pointer"
                />
                <div class="text-xs leading-relaxed">
                  <span class="font-bold text-text-strong block">Continuous Timer — Cannot Be Paused:</span>
                  <span class="text-text-muted">Once started, the countdown timer will run continuously. It cannot be paused, stopped, or reset under any circumstance.</span>
                </div>
              </label>

              <!-- Rule 2: Fullscreen Required -->
              <label class="flex items-start gap-3 p-3 rounded-lg border border-border bg-card cursor-pointer hover:bg-surface-raised transition">
                <input
                  v-model="agreeFullscreen"
                  type="checkbox"
                  class="mt-0.5 h-4 w-4 rounded border-border text-primary focus:ring-2 focus:ring-primary/20 cursor-pointer"
                />
                <div class="text-xs leading-relaxed">
                  <span class="font-bold text-text-strong block">Fullscreen Lockdown:</span>
                  <span class="text-text-muted">The exam will open in Fullscreen mode. Exiting fullscreen or minimizing will be logged as a security violation.</span>
                </div>
              </label>

              <!-- Rule 3: Disabled Shortcuts, Copy & Paste -->
              <label class="flex items-start gap-3 p-3 rounded-lg border border-border bg-card cursor-pointer hover:bg-surface-raised transition">
                <input
                  v-model="agreeRestrictions"
                  type="checkbox"
                  class="mt-0.5 h-4 w-4 rounded border-border text-primary focus:ring-2 focus:ring-primary/20 cursor-pointer"
                />
                <div class="text-xs leading-relaxed">
                  <span class="font-bold text-text-strong block">Disabled Shortcuts & Copy/Paste:</span>
                  <span class="text-text-muted">Copying, pasting, right-clicking (context menu), text selection, print screen, developer tools, and navigation shortcuts are strictly disabled.</span>
                </div>
              </label>

              <!-- Rule 4: Window / Tab Switch Infractions -->
              <label class="flex items-start gap-3 p-3 rounded-lg border border-border bg-card cursor-pointer hover:bg-surface-raised transition">
                <input
                  v-model="agreeInfractions"
                  type="checkbox"
                  class="mt-0.5 h-4 w-4 rounded border-border text-primary focus:ring-2 focus:ring-primary/20 cursor-pointer"
                />
                <div class="text-xs leading-relaxed">
                  <span class="font-bold text-text-strong block">3-Infraction Auto-Submission Policy:</span>
                  <span class="text-text-muted">Switching tabs, changing windows, or exiting fullscreen is tracked. Accumulating 3 security infractions will immediately auto-submit your assessment.</span>
                </div>
              </label>
            </div>
          </div>

          <!-- Action Buttons -->
          <div class="pt-2 flex flex-col sm:flex-row items-center justify-between gap-3">
            <button
              type="button"
              @click="router.push(`/jobs/${jobId}`)"
              class="w-full sm:w-auto px-4 py-2.5 text-xs font-semibold rounded-lg border border-border bg-card text-text-secondary hover:bg-surface-raised transition text-center"
            >
              Return to Job Details
            </button>

            <button
              type="button"
              :disabled="!allRulesAgreed || startingQuiz"
              @click="handleEnterSafeExam"
              :class="[
                'w-full sm:w-auto inline-flex items-center justify-center gap-2 px-7 py-3 text-xs sm:text-sm font-bold rounded-xl text-white transition shadow-sm',
                allRulesAgreed
                  ? 'bg-primary hover:bg-primary-hover active:scale-[0.99] ring-2 ring-primary/30'
                  : 'bg-primary/50 opacity-60 cursor-not-allowed'
              ]"
            >
              <Play class="h-4 w-4 fill-current" />
              <span>
                {{ startingQuiz ? 'Entering Safe Exam...' : (isResumingAttempt ? 'Acknowledge & Resume Exam' : 'Enter Safe Exam & Begin Quiz') }}
              </span>
            </button>
          </div>

          <p v-if="!allRulesAgreed" class="text-[11px] text-amber-600 dark:text-amber-400 text-center font-medium">
            Please acknowledge all 4 exam rules above to unlock the Begin Exam button.
          </p>
        </div>
      </div>
    </div>

    <!-- =================================================================== -->
    <!-- ISOLATED SAFE EXAM BROWSER LOCKDOWN INTERFACE                       -->
    <!-- (Fixed full-screen viewport, isolated from the rest of the web app)  -->
    <!-- =================================================================== -->
    <div
      v-if="hasStarted && attempt && attempt.status === 'in_progress'"
      class="fixed inset-0 z-50 bg-page select-none flex flex-col h-screen w-screen overflow-hidden"
    >
      <!-- Safe Exam Browser HUD (Top Bar) -->
      <header class="bg-card/95 backdrop-blur-md border-b border-border px-4 sm:px-6 py-3 shrink-0 shadow-sm z-30">
        <div class="max-w-7xl mx-auto flex flex-col sm:flex-row items-center justify-between gap-3">
          
          <!-- Left: Lockdown Status & Infractions -->
          <div class="flex items-center gap-3">
            <!-- Pulsing Active Shield -->
            <div class="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-[11px] font-bold bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 border border-emerald-500/20">
              <span class="h-2 w-2 rounded-full bg-emerald-500 animate-pulse"></span>
              <ShieldCheck class="h-3.5 w-3.5" />
              <span>Safe Exam Lockdown</span>
            </div>

            <!-- Infractions Counter -->
            <div
              :class="[
                'inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-[11px] font-bold border transition',
                infractionCount === 0
                  ? 'bg-surface-raised text-text-muted border-border'
                  : infractionCount === 1
                    ? 'bg-amber-500/10 text-amber-600 dark:text-amber-400 border-amber-500/30'
                    : 'bg-danger/10 text-danger border-danger/30 animate-pulse'
              ]"
            >
              <AlertTriangle class="h-3.5 w-3.5" />
              <span>Infractions: {{ infractionCount }} / {{ MAX_INFRACTIONS }}</span>
            </div>

            <!-- Fullscreen trigger (if out of fullscreen) -->
            <button
              v-if="!isFullscreen"
              type="button"
              @click="requestFullscreenMode"
              class="inline-flex items-center gap-1 px-2.5 py-1 text-[11px] font-semibold rounded-lg bg-primary text-white hover:bg-primary-hover shadow-xs transition"
            >
              <Maximize class="h-3 w-3" />
              <span>Re-enter Fullscreen</span>
            </button>
          </div>

          <!-- Center: Title & Progress -->
          <div class="text-center min-w-0">
            <h2 class="text-xs sm:text-sm font-bold text-text-strong truncate max-w-xs sm:max-w-md">
              {{ attempt.stage_title }}
            </h2>
            <p class="text-[11px] text-text-muted">
              {{ answeredCount }} of {{ questions.length }} answered ({{ progressPercentage }}%)
            </p>
          </div>

          <!-- Right: Countdown Timer & Quick Submit -->
          <div class="flex items-center gap-3">
            <!-- Timer Clock -->
            <div v-if="formattedTime" class="flex items-center gap-2">
              <div
                :class="[
                  'flex items-center gap-1.5 px-3 py-1.5 rounded-lg border font-mono font-bold text-xs sm:text-sm transition shadow-xs',
                  isTimeExpiringSoon
                    ? 'bg-danger text-white border-danger animate-pulse'
                    : 'bg-surface-raised text-text-strong border-border'
                ]"
              >
                <Clock class="h-4 w-4" />
                <span>{{ formattedTime }}</span>
              </div>
            </div>

            <!-- Finish Button -->
            <button
              type="button"
              :disabled="submitting"
              @click="submitQuiz(false)"
              class="inline-flex items-center gap-1.5 px-4 py-1.5 text-xs font-semibold rounded-lg bg-primary text-white hover:bg-primary-hover disabled:opacity-50 transition shadow-xs"
            >
              <Send class="h-3.5 w-3.5" />
              <span>Finish</span>
            </button>
          </div>
        </div>

        <!-- Visual Progress Bar -->
        <div class="w-full bg-surface-raised h-1 rounded-full overflow-hidden mt-2.5">
          <div
            class="bg-primary h-full transition-all duration-300"
            :style="{ width: `${progressPercentage}%` }"
          ></div>
        </div>
      </header>

      <!-- Main Isolated Question Workspace -->
      <main class="flex-1 overflow-y-auto px-4 sm:px-6 lg:px-8 py-6">
        <div class="max-w-3xl mx-auto space-y-6 pb-20">
          
          <!-- EMPTY QUESTIONS FALLBACK -->
          <div v-if="questions.length === 0" class="bg-card border border-border rounded-2xl p-8 text-center space-y-4 shadow-xs">
            <AlertCircle class="h-10 w-10 text-amber-500 mx-auto" />
            <h2 class="text-base sm:text-lg font-bold text-text-strong">No Questions Configured</h2>
            <p class="text-xs sm:text-sm text-text-muted max-w-md mx-auto">
              This assessment stage does not have any questions assigned to it yet. Please contact the hiring team or administrator.
            </p>
            <div class="pt-2">
              <button
                type="button"
                @click="router.push(`/jobs/${jobId}`)"
                class="inline-flex items-center gap-1.5 px-4 py-2 text-xs font-semibold rounded-lg bg-primary text-white hover:bg-primary-hover shadow-xs transition"
              >
                <ArrowLeft class="h-4 w-4" />
                <span>Return to Job Details</span>
              </button>
            </div>
          </div>

          <!-- Questions Feed -->
          <div v-else class="space-y-4">
            <div
              v-for="(q, idx) in questions"
              :key="q.id"
              class="bg-card border border-border rounded-2xl p-5 sm:p-7 shadow-xs space-y-4"
            >
              <!-- Question Header -->
              <div class="flex items-center justify-between border-b border-border pb-3">
                <div class="flex items-center gap-2">
                  <span class="text-xs font-bold text-primary">Question {{ idx + 1 }}</span>
                  <span class="text-[10px] uppercase font-semibold tracking-wider text-text-muted px-2 py-0.5 rounded bg-surface-raised border border-border">
                    {{ q.question_type.replace('_', ' ') }}
                  </span>
                </div>
                <span class="text-xs font-bold text-text-strong">{{ q.points }} pt{{ q.points !== 1 ? 's' : '' }}</span>
              </div>

              <!-- Question Prompt Body -->
              <p class="text-xs sm:text-sm font-medium text-text-strong leading-relaxed whitespace-pre-line select-none">
                {{ q.body }}
              </p>

              <!-- SINGLE CHOICE OPTIONS -->
              <div v-if="q.question_type === 'single_choice' && q.options" class="space-y-2 pt-1">
                <label
                  v-for="opt in q.options"
                  :key="opt.id"
                  @click="selectSingleChoice(q.id, opt.id)"
                  :class="[
                    'flex items-center gap-3 p-3 rounded-xl border text-xs sm:text-sm cursor-pointer transition select-none',
                    answers[q.id] === opt.id
                      ? 'border-primary bg-primary-light text-text-strong font-medium ring-1 ring-primary/40'
                      : 'border-border bg-card text-text-secondary hover:bg-surface-raised hover:border-border-strong'
                  ]"
                >
                  <div
                    :class="[
                      'h-4 w-4 rounded-full border flex items-center justify-center shrink-0 transition',
                      answers[q.id] === opt.id
                        ? 'border-primary bg-primary'
                        : 'border-border'
                    ]"
                  >
                    <div v-if="answers[q.id] === opt.id" class="h-1.5 w-1.5 rounded-full bg-white"></div>
                  </div>
                  <span class="font-bold text-xs uppercase w-4 text-center shrink-0">{{ opt.id }}</span>
                  <span class="flex-1">{{ opt.text }}</span>
                </label>
              </div>

              <!-- MULTIPLE CHOICE OPTIONS -->
              <div v-else-if="q.question_type === 'multiple_choice' && q.options" class="space-y-2 pt-1">
                <label
                  v-for="opt in q.options"
                  :key="opt.id"
                  @click="toggleMultipleChoice(q.id, opt.id)"
                  :class="[
                    'flex items-center gap-3 p-3 rounded-xl border text-xs sm:text-sm cursor-pointer transition select-none',
                    isMultipleChoiceChecked(q.id, opt.id)
                      ? 'border-primary bg-primary-light text-text-strong font-medium ring-1 ring-primary/40'
                      : 'border-border bg-card text-text-secondary hover:bg-surface-raised hover:border-border-strong'
                  ]"
                >
                  <div
                    :class="[
                      'h-4 w-4 rounded-md border flex items-center justify-center shrink-0 transition',
                      isMultipleChoiceChecked(q.id, opt.id)
                        ? 'border-primary bg-primary text-white'
                        : 'border-border'
                    ]"
                  >
                    <Check v-if="isMultipleChoiceChecked(q.id, opt.id)" class="h-3 w-3 stroke-[3]" />
                  </div>
                  <span class="font-bold text-xs uppercase w-4 text-center shrink-0">{{ opt.id }}</span>
                  <span class="flex-1">{{ opt.text }}</span>
                </label>
              </div>

              <!-- TRUE / FALSE OPTIONS -->
              <div v-else-if="q.question_type === 'true_false'" class="grid grid-cols-2 gap-3 pt-1">
                <button
                  type="button"
                  @click="answers[q.id] = 'true'"
                  :class="[
                    'p-3 rounded-xl border text-center text-xs sm:text-sm font-semibold transition cursor-pointer select-none',
                    answers[q.id] === 'true'
                      ? 'border-primary bg-primary text-white shadow-xs'
                      : 'border-border bg-card text-text-strong hover:bg-surface-raised'
                  ]"
                >
                  True
                </button>
                <button
                  type="button"
                  @click="answers[q.id] = 'false'"
                  :class="[
                    'p-3 rounded-xl border text-center text-xs sm:text-sm font-semibold transition cursor-pointer select-none',
                    answers[q.id] === 'false'
                      ? 'border-primary bg-primary text-white shadow-xs'
                      : 'border-border bg-card text-text-strong hover:bg-surface-raised'
                  ]"
                >
                  False
                </button>
              </div>

              <!-- STRUCTURED RESPONSE (Typing allowed, copy/paste blocked) -->
              <div v-else-if="q.question_type === 'structured'" class="space-y-2 pt-1">
                <textarea
                  v-model="answers[q.id]"
                  rows="4"
                  placeholder="Type your structured answer here..."
                  class="w-full bg-card rounded-xl border border-border p-3 text-xs sm:text-sm text-text-strong placeholder:text-text-subtle focus:outline-none focus:ring-2 focus:ring-primary/20 transition select-text"
                ></textarea>
                <span class="text-[11px] text-text-muted">Provide a clear and concise response. Copy/Paste is disabled.</span>
              </div>
            </div>
          </div>

          <!-- Bottom Finish Bar -->
          <div v-if="questions.length > 0" class="bg-card border border-border rounded-2xl p-4 sm:p-5 flex flex-col sm:flex-row items-center justify-between gap-3 shadow-xs">
            <p class="text-xs text-text-muted">
              <span class="font-semibold text-text-strong">{{ answeredCount }} of {{ questions.length }}</span> questions completed.
              {{ answeredCount < questions.length ? 'You can still review before submitting.' : 'All questions answered!' }}
            </p>

            <button
              type="button"
              :disabled="submitting"
              @click="submitQuiz(false)"
              class="w-full sm:w-auto inline-flex items-center justify-center gap-2 px-7 py-2.5 text-xs sm:text-sm font-bold rounded-lg bg-primary text-white hover:bg-primary-hover disabled:opacity-50 transition shadow-xs"
            >
              <Send class="h-4 w-4" />
              <span>{{ submitting ? 'Submitting...' : 'Submit Assessment' }}</span>
            </button>
          </div>
        </div>
      </main>

      <!-- =================================================================== -->
      <!-- SECURITY WARNING MODAL (Tab switch / blur / window inactive)        -->
      <!-- =================================================================== -->
      <div
        v-if="showInfractionModal"
        class="fixed inset-0 z-50 bg-black/75 backdrop-blur-sm flex items-center justify-center p-4"
      >
        <div class="bg-card border border-danger/40 rounded-2xl max-w-md w-full p-6 text-center space-y-4 shadow-xl animate-in fade-in zoom-in-95">
          <div class="h-14 w-14 rounded-full mx-auto flex items-center justify-center bg-danger/10 text-danger border border-danger/30">
            <ShieldAlert class="h-7 w-7" />
          </div>

          <div>
            <h3 class="text-base sm:text-lg font-bold text-text-strong">Security Infraction Recorded</h3>
            <p class="text-xs text-text-muted mt-1 leading-relaxed">
              {{ lastInfractionReason || 'Window focus was lost or a tab switch was detected.' }}
            </p>
          </div>

          <div class="p-3 bg-danger/10 rounded-xl border border-danger/30 text-center">
            <span class="text-xs font-bold uppercase tracking-wider text-danger block">
              Strike {{ infractionCount }} of {{ MAX_INFRACTIONS }}
            </span>
            <span class="text-[11px] text-text-secondary mt-0.5 block">
              {{ MAX_INFRACTIONS - infractionCount }} more infraction(s) will trigger immediate automatic submission.
            </span>
          </div>

          <button
            type="button"
            @click="showInfractionModal = false; requestFullscreenMode()"
            class="w-full py-2.5 text-xs sm:text-sm font-bold rounded-xl bg-primary text-white hover:bg-primary-hover transition shadow-sm"
          >
            I Understand & Resume Exam
          </button>
        </div>
      </div>

      <!-- =================================================================== -->
      <!-- FULLSCREEN REQUIRED MODAL                                           -->
      <!-- =================================================================== -->
      <div
        v-if="showFullscreenModal && !isFullscreen"
        class="fixed inset-0 z-50 bg-black/75 backdrop-blur-sm flex items-center justify-center p-4"
      >
        <div class="bg-card border border-primary/40 rounded-2xl max-w-md w-full p-6 text-center space-y-4 shadow-xl">
          <div class="h-14 w-14 rounded-full mx-auto flex items-center justify-center bg-primary/10 text-primary border border-primary/30">
            <Maximize class="h-7 w-7" />
          </div>

          <div>
            <h3 class="text-base sm:text-lg font-bold text-text-strong">Fullscreen Mode Required</h3>
            <p class="text-xs text-text-muted mt-1 leading-relaxed">
              Safe Exam Browser requires fullscreen mode to prevent unauthorized browsing and multitasking.
            </p>
          </div>

          <button
            type="button"
            @click="showFullscreenModal = false; requestFullscreenMode()"
            class="w-full py-2.5 text-xs sm:text-sm font-bold rounded-xl bg-primary text-white hover:bg-primary-hover transition shadow-sm"
          >
            Re-enter Fullscreen Mode
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
