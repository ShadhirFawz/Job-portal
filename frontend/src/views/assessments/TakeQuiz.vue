<script setup lang="ts">
import { ref, onMounted, computed, onUnmounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { assessmentService } from '@/services/assessment.service'
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
  RotateCcw
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
const submitting = ref(false)
const errorMsg = ref('')

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

onMounted(async () => {
  if (!auth.user) {
    router.push({ name: 'login', query: { redirect: route.fullPath } })
    return
  }
  await initAssessment()
})

onUnmounted(() => {
  if (timerInterval) clearInterval(timerInterval)
})

async function initAssessment() {
  loading.value = true
  errorMsg.value = ''
  try {
    const data = await assessmentService.startAssessment(auth.user!.id, {
      job_id: jobId,
      selection_process_id: processId,
      application_id: applicationId || null
    })

    attempt.value = data
    questions.value = data.questions || []

    // If already submitted/graded, don't start timer
    if (data.status === 'submitted' || data.status === 'graded') {
      loading.value = false
      return
    }

    // Initialize timer
    if (data.expires_at) {
      const expiresAtMs = new Date(data.expires_at).getTime()
      const nowMs = Date.now()
      const diffSecs = Math.max(0, Math.floor((expiresAtMs - nowMs) / 1000))
      timeRemainingSeconds.value = diffSecs

      if (diffSecs <= 0) {
        handleTimeExpired()
      } else {
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
    }
  } catch (err: any) {
    errorMsg.value = err.message || 'Failed to start assessment. Please ensure a valid question bank is attached.'
  } finally {
    loading.value = false
  }
}

function handleTimeExpired() {
  toast.error('Time limit reached! Submitting answers automatically...')
  submitQuiz(true)
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

async function submitQuiz(auto = false) {
  if (!attempt.value || !auth.user) return

  if (!auto && answeredCount.value < questions.value.length) {
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
    toast.success('Assessment submitted successfully!')
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
      <div v-if="loading" class="py-20 text-center">
        <div class="h-8 w-8 animate-spin rounded-full border-2 border-primary border-t-transparent mx-auto mb-3"></div>
        <p class="text-xs sm:text-sm text-text-muted">Preparing your assessment...</p>
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

      <!-- IN-PROGRESS QUIZ TAKING INTERFACE -->
      <div v-else-if="attempt" class="space-y-6">
        
        <!-- Sticky Top Assessment Bar -->
        <div class="sticky top-0 z-20 bg-card/95 backdrop-blur-md border border-border rounded-2xl p-4 shadow-sm flex items-center justify-between gap-4">
          <div>
            <h1 class="text-sm sm:text-base font-bold text-text-strong">{{ attempt.stage_title }}</h1>
            <p class="text-[11px] text-text-muted">
              {{ answeredCount }} of {{ questions.length }} answered
            </p>
          </div>

          <!-- Timer Clock -->
          <div v-if="formattedTime" class="flex items-center gap-2">
            <div
              :class="[
                'flex items-center gap-1.5 px-3 py-1.5 rounded-lg border font-mono font-bold text-xs sm:text-sm transition',
                isTimeExpiringSoon
                  ? 'bg-danger-bg text-danger border-danger/30 animate-pulse'
                  : 'bg-surface-raised text-text-strong border-border'
              ]"
            >
              <Clock class="h-4 w-4" />
              <span>{{ formattedTime }}</span>
            </div>
          </div>
        </div>

        <!-- Questions Feed -->
        <div class="space-y-4">
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
            <p class="text-xs sm:text-sm font-medium text-text-strong leading-relaxed whitespace-pre-line">
              {{ q.body }}
            </p>

            <!-- SINGLE CHOICE OPTIONS -->
            <div v-if="q.question_type === 'single_choice'" class="space-y-2 pt-1">
              <label
                v-for="opt in q.options"
                :key="opt.id"
                @click="selectSingleChoice(q.id, opt.id)"
                :class="[
                  'flex items-center gap-3 p-3 rounded-xl border text-xs sm:text-sm cursor-pointer transition',
                  answers[q.id] === opt.id
                    ? 'border-primary bg-primary-light text-text-strong font-medium'
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
                <span class="font-bold text-xs uppercase w-4 text-center">{{ opt.id }}</span>
                <span class="flex-1">{{ opt.text }}</span>
              </label>
            </div>

            <!-- MULTIPLE CHOICE OPTIONS -->
            <div v-else-if="q.question_type === 'multiple_choice'" class="space-y-2 pt-1">
              <label
                v-for="opt in q.options"
                :key="opt.id"
                @click="toggleMultipleChoice(q.id, opt.id)"
                :class="[
                  'flex items-center gap-3 p-3 rounded-xl border text-xs sm:text-sm cursor-pointer transition',
                  isMultipleChoiceChecked(q.id, opt.id)
                    ? 'border-primary bg-primary-light text-text-strong font-medium'
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
                <span class="font-bold text-xs uppercase w-4 text-center">{{ opt.id }}</span>
                <span class="flex-1">{{ opt.text }}</span>
              </label>
            </div>

            <!-- TRUE / FALSE OPTIONS -->
            <div v-else-if="q.question_type === 'true_false'" class="grid grid-cols-2 gap-3 pt-1">
              <button
                type="button"
                @click="answers[q.id] = 'true'"
                :class="[
                  'p-3 rounded-xl border text-center text-xs sm:text-sm font-semibold transition cursor-pointer',
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
                  'p-3 rounded-xl border text-center text-xs sm:text-sm font-semibold transition cursor-pointer',
                  answers[q.id] === 'false'
                    ? 'border-primary bg-primary text-white shadow-xs'
                    : 'border-border bg-card text-text-strong hover:bg-surface-raised'
                ]"
              >
                False
              </button>
            </div>

            <!-- STRUCTURED RESPONSE -->
            <div v-else-if="q.question_type === 'structured'" class="space-y-2 pt-1">
              <textarea
                v-model="answers[q.id]"
                rows="4"
                placeholder="Type your structured answer here..."
                class="w-full bg-card rounded-xl border border-border p-3 text-xs sm:text-sm text-text-strong placeholder:text-text-subtle focus:outline-none focus:ring-2 focus:ring-primary/20 transition"
              ></textarea>
              <span class="text-[11px] text-text-muted">Provide a clear and concise response.</span>
            </div>
          </div>
        </div>

        <!-- Submit Bar -->
        <div class="bg-card border border-border rounded-2xl p-4 sm:p-5 flex flex-col sm:flex-row items-center justify-between gap-3 shadow-xs">
          <p class="text-xs text-text-muted">
            Ready to complete your assessment? Once submitted, your answers will be evaluated.
          </p>

          <button
            type="button"
            :disabled="submitting"
            @click="submitQuiz(false)"
            class="w-full sm:w-auto inline-flex items-center justify-center gap-2 px-6 py-2.5 text-xs sm:text-sm font-semibold rounded-lg bg-primary text-white hover:bg-primary-hover disabled:opacity-50 transition shadow-xs"
          >
            <Send class="h-4 w-4" />
            <span>{{ submitting ? 'Submitting...' : 'Submit Assessment' }}</span>
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

