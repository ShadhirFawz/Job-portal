<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { questionBankService } from '@/services/questionBank.service'
import type {
  QuestionBank,
  Question,
  QuestionType,
  DifficultyLevel,
  QuestionOption
} from '@/types/assessment'
import {
  ArrowLeft,
  Plus,
  Edit2,
  Trash2,
  Lock,
  Layers,
  HelpCircle,
  CheckCircle,
  FileText,
  AlertCircle,
  Check,
  ChevronRight,
  Sparkles
} from 'lucide-vue-next'
import { toast } from 'vue-sonner'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()

const organizationId = route.params.orgId as string

// Query flags
const isCreateQuery = computed(() => route.query.create === 'true')
const returnToJob = computed(() => route.query.returnToJob === 'true')

// Banks state
const banks = ref<QuestionBank[]>([])
const selectedBank = ref<QuestionBank | null>(null)
const loading = ref(true)

// Bank Modal State
const showBankModal = ref(false)
const editingBankId = ref<string | null>(null)
const bankName = ref('')
const bankDescription = ref('')
const bankCategory = ref('')
const savingBank = ref(false)

// Question Modal State
const showQuestionModal = ref(false)
const editingQuestionId = ref<string | null>(null)
const qBody = ref('')
const qType = ref<QuestionType>('single_choice')
const qOptions = ref<QuestionOption[]>([
  { id: 'a', text: '', is_correct: false },
  { id: 'b', text: '', is_correct: false }
])
const qCorrectAnswer = ref('')
const qExplanation = ref('')
const qDifficulty = ref<DifficultyLevel>('medium')
const qPoints = ref(1)
const savingQuestion = ref(false)
const questionError = ref('')

// Check ownership
function isOwner(bank: QuestionBank | null): boolean {
  if (!bank || !auth.user) return false
  return bank.created_by === auth.user.id
}

onMounted(async () => {
  await fetchBanks()
  if (isCreateQuery.value) {
    openCreateBank()
  }
})

async function fetchBanks() {
  loading.value = true
  try {
    banks.value = await questionBankService.getOrganizationBanks(organizationId)
    if (selectedBank.value) {
      // Refresh selected bank details
      const refreshed = banks.value.find(b => b.id === selectedBank.value!.id)
      selectedBank.value = refreshed ?? (banks.value[0] ?? null)
    } else if (banks.value.length > 0) {
      selectedBank.value = banks.value[0] ?? null
    }
  } catch (error) {
    toast.error('Failed to load question banks')
  } finally {
    loading.value = false
  }
}

// ----------------------------------------------------
// BANK CRUD
// ----------------------------------------------------
function openCreateBank() {
  editingBankId.value = null
  bankName.value = ''
  bankDescription.value = ''
  bankCategory.value = ''
  showBankModal.value = true
}

function openEditBank(bank: QuestionBank) {
  if (!isOwner(bank)) {
    toast.error('Only the creator of this question bank can modify it.')
    return
  }
  editingBankId.value = bank.id
  bankName.value = bank.name
  bankDescription.value = bank.description || ''
  bankCategory.value = bank.category || ''
  showBankModal.value = true
}

async function saveBank() {
  if (!bankName.value.trim()) {
    toast.error('Bank title is required')
    return
  }
  if (!auth.user) return

  savingBank.value = true
  try {
    if (editingBankId.value) {
      const updated = await questionBankService.updateBank(editingBankId.value, auth.user.id, {
        name: bankName.value.trim(),
        description: bankDescription.value.trim() || undefined,
        category: bankCategory.value.trim() || undefined
      })
      toast.success('Question bank updated successfully')
      showBankModal.value = false
      await fetchBanks()
      selectedBank.value = updated
    } else {
      const created = await questionBankService.createBank(organizationId, auth.user.id, {
        name: bankName.value.trim(),
        description: bankDescription.value.trim() || undefined,
        category: bankCategory.value.trim() || undefined
      })
      toast.success('Question bank created successfully')
      showBankModal.value = false
      await fetchBanks()
      selectedBank.value = created

      // If return to job flag was set, redirect back to create job page with newly created bank ID
      if (returnToJob.value) {
        router.push({
          path: `/organizations/${organizationId}/jobs/create`,
          query: { newBankId: created.id }
        })
        return
      }
    }
  } catch (err: any) {
    toast.error(err.message || 'Failed to save question bank')
  } finally {
    savingBank.value = false
  }
}

async function deleteBank(bank: QuestionBank) {
  if (!isOwner(bank)) {
    toast.error('Only the creator of this question bank can delete it.')
    return
  }
  if (!confirm(`Are you sure you want to delete "${bank.name}" and all its questions?`)) return

  try {
    await questionBankService.deleteBank(bank.id, auth.user!.id)
    toast.success('Question bank deleted')
    selectedBank.value = null
    await fetchBanks()
  } catch (err: any) {
    toast.error(err.message || 'Failed to delete question bank')
  }
}

// ----------------------------------------------------
// QUESTION CRUD
// ----------------------------------------------------
function openAddQuestion() {
  if (!selectedBank.value) return
  if (!isOwner(selectedBank.value)) {
    toast.error('Only the creator can add questions to this question bank.')
    return
  }
  editingQuestionId.value = null
  qBody.value = ''
  qType.value = 'single_choice'
  qOptions.value = [
    { id: 'a', text: '', is_correct: false },
    { id: 'b', text: '', is_correct: false }
  ]
  qCorrectAnswer.value = ''
  qExplanation.value = ''
  qDifficulty.value = 'medium'
  qPoints.value = 1
  questionError.value = ''
  showQuestionModal.value = true
}

function openEditQuestion(q: Question) {
  if (!selectedBank.value || !isOwner(selectedBank.value)) {
    toast.error('Only the creator can modify questions in this bank.')
    return
  }
  editingQuestionId.value = q.id
  qBody.value = q.body
  qType.value = q.question_type
  qOptions.value = q.options && q.options.length > 0
    ? JSON.parse(JSON.stringify(q.options))
    : [
        { id: 'a', text: '', is_correct: false },
        { id: 'b', text: '', is_correct: false }
      ]
  qCorrectAnswer.value = q.correct_answer || ''
  qExplanation.value = q.explanation || ''
  qDifficulty.value = q.difficulty || 'medium'
  qPoints.value = q.points || 1
  questionError.value = ''
  showQuestionModal.value = true
}

function addOption() {
  const nextId = String.fromCharCode(97 + qOptions.value.length)
  qOptions.value.push({ id: nextId, text: '', is_correct: false })
}

function removeOption(idx: number) {
  if (qOptions.value.length <= 2) return
  qOptions.value.splice(idx, 1)
}

function handleOptionCheck(idx: number) {
  if (qType.value === 'single_choice' || qType.value === 'true_false') {
    qOptions.value.forEach((opt, i) => {
      opt.is_correct = i === idx
    })
  } else if (qType.value === 'multiple_choice' && qOptions.value[idx]) {
    qOptions.value[idx].is_correct = !qOptions.value[idx].is_correct
  }
}

async function saveQuestion() {
  if (!qBody.value.trim()) {
    questionError.value = 'Question prompt text is required'
    return
  }

  // Validation per type
  if (qType.value === 'single_choice' || qType.value === 'multiple_choice') {
    const filled = qOptions.value.filter(o => o.text.trim())
    if (filled.length < 2) {
      questionError.value = 'At least two options are required'
      return
    }
    if (!qOptions.value.some(o => o.is_correct)) {
      questionError.value = 'Please select at least one correct answer'
      return
    }
  } else if (qType.value === 'true_false') {
    if (!qOptions.value.some(o => o.is_correct)) {
      questionError.value = 'Please select whether True or False is the correct answer'
      return
    }
  }

  if (!auth.user || !selectedBank.value) return

  savingQuestion.value = true
  questionError.value = ''

  const finalOptions = qType.value === 'structured'
    ? []
    : qType.value === 'true_false'
      ? [
          { id: 'true', text: 'True', is_correct: qOptions.value[0]?.is_correct || false },
          { id: 'false', text: 'False', is_correct: qOptions.value[1]?.is_correct || false }
        ]
      : qOptions.value.filter(o => o.text.trim())

  try {
    if (editingQuestionId.value) {
      await questionBankService.updateQuestion(editingQuestionId.value, auth.user.id, {
        body: qBody.value.trim(),
        question_type: qType.value,
        options: finalOptions,
        correct_answer: qCorrectAnswer.value.trim() || undefined,
        explanation: qExplanation.value.trim() || undefined,
        difficulty: qDifficulty.value,
        points: qPoints.value
      })
      toast.success('Question updated')
    } else {
      await questionBankService.addQuestion(selectedBank.value.id, auth.user.id, {
        body: qBody.value.trim(),
        question_type: qType.value,
        options: finalOptions,
        correct_answer: qCorrectAnswer.value.trim() || undefined,
        explanation: qExplanation.value.trim() || undefined,
        difficulty: qDifficulty.value,
        points: qPoints.value,
        order_index: selectedBank.value.questions?.length || 0
      })
      toast.success('Question added to bank')
    }

    showQuestionModal.value = false
    await fetchBanks()
  } catch (err: any) {
    questionError.value = err.message || 'Failed to save question'
  } finally {
    savingQuestion.value = false
  }
}

async function deleteQuestion(qId: string) {
  if (!selectedBank.value || !isOwner(selectedBank.value)) {
    toast.error('Only the creator can delete questions from this bank.')
    return
  }
  if (!confirm('Are you sure you want to delete this question?')) return

  try {
    await questionBankService.deleteQuestion(qId, auth.user!.id)
    toast.success('Question deleted')
    await fetchBanks()
  } catch (err: any) {
    toast.error(err.message || 'Failed to delete question')
  }
}

function formatType(t: string): string {
  const map: Record<string, string> = {
    single_choice: 'Single Choice',
    multiple_choice: 'Multiple Choice',
    true_false: 'True / False',
    structured: 'Structured'
  }
  return map[t] || t.replace('_', ' ')
}
</script>

<template>
  <div class="min-h-screen bg-page py-6 sm:py-8 px-4 sm:px-6 lg:px-8">
    <div class="max-w-7xl mx-auto space-y-6">
      
      <!-- Top Navigation Header -->
      <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4 border-b border-border pb-5">
        <div>
          <button
            type="button"
            @click="returnToJob ? router.push(`/organizations/${organizationId}/jobs/create`) : router.push(`/organizations/${organizationId}?tab=jobs`)"
            class="inline-flex items-center gap-1.5 text-xs sm:text-sm font-medium text-text-muted hover:text-text-strong transition mb-2"
          >
            <ArrowLeft class="h-4 w-4" />
            <span>{{ returnToJob ? 'Return to Job Creation' : 'Back to Organization' }}</span>
          </button>
          <div class="flex items-center gap-3">
            <h1 class="text-2xl sm:text-3xl font-bold tracking-tight text-text-strong">
              Question Banks
            </h1>
            <span class="px-2.5 py-0.5 rounded-full text-xs font-semibold bg-primary-light text-primary border border-primary/20">
              {{ banks.length }} {{ banks.length === 1 ? 'Bank' : 'Banks' }}
            </span>
          </div>
          <p class="text-xs sm:text-sm text-text-muted mt-1">
            Organize questions into reusable banks. Questions can be single choice, multiple choice, true/false, or structured.
          </p>
        </div>

        <div class="flex items-center gap-3 self-start sm:self-auto">
          <button
            type="button"
            @click="openCreateBank"
            class="inline-flex items-center gap-2 px-4 py-2 text-xs sm:text-sm font-semibold rounded-lg bg-primary text-white hover:bg-primary-hover shadow-xs transition"
          >
            <Plus class="h-4 w-4" />
            <span>Create Question Bank</span>
          </button>
        </div>
      </div>

      <!-- Main Dual Panel Layout -->
      <div v-if="loading" class="py-20 text-center">
        <div class="h-8 w-8 animate-spin rounded-full border-2 border-primary border-t-transparent mx-auto mb-3"></div>
        <p class="text-xs sm:text-sm text-text-muted">Loading question banks...</p>
      </div>

      <div v-else-if="banks.length === 0" class="bg-card border border-border rounded-2xl p-12 text-center shadow-xs">
        <div class="h-12 w-12 rounded-2xl bg-surface-raised border border-border mx-auto flex items-center justify-center text-text-muted mb-4">
          <Layers class="h-6 w-6" />
        </div>
        <h3 class="text-base sm:text-lg font-semibold text-text-strong">No Question Banks Yet</h3>
        <p class="text-xs sm:text-sm text-text-muted max-w-md mx-auto mt-1 mb-6">
          Create topic-based question banks (e.g. "React Questions", "JavaScript Core", "System Design") to attach to candidate assessment stages.
        </p>
        <button
          type="button"
          @click="openCreateBank"
          class="inline-flex items-center gap-2 px-4 py-2 text-xs sm:text-sm font-semibold rounded-lg bg-primary text-white hover:bg-primary-hover shadow-xs transition"
        >
          <Plus class="h-4 w-4" />
          <span>Create Your First Bank</span>
        </button>
      </div>

      <div v-else class="grid grid-cols-1 lg:grid-cols-12 gap-6 items-start">
        
        <!-- Left Column: Bank Selection List (4 cols) -->
        <div class="lg:col-span-4 space-y-3">
          <div class="text-xs font-semibold uppercase tracking-wider text-text-muted px-1">
            Question Banks ({{ banks.length }})
          </div>

          <div class="space-y-2">
            <div
              v-for="b in banks"
              :key="b.id"
              @click="selectedBank = b"
              :class="[
                'p-4 rounded-xl border transition-all cursor-pointer relative shadow-xs',
                selectedBank?.id === b.id
                  ? 'bg-card border-primary ring-1 ring-primary/40'
                  : 'bg-card border-border hover:border-border-strong hover:bg-surface-raised'
              ]"
            >
              <div class="flex items-start justify-between gap-2">
                <div class="min-w-0 flex-1">
                  <div class="flex items-center gap-1.5 flex-wrap">
                    <span class="text-sm font-bold text-text-strong truncate">{{ b.name }}</span>
                    <span v-if="b.category" class="px-1.5 py-0.5 text-[10px] rounded bg-surface-raised border border-border text-text-muted">
                      {{ b.category }}
                    </span>
                  </div>
                  <p v-if="b.description" class="text-xs text-text-secondary mt-1 line-clamp-2">
                    {{ b.description }}
                  </p>
                </div>
                <ChevronRight class="h-4 w-4 text-text-subtle shrink-0 mt-0.5" />
              </div>

              <!-- Bank Meta footer -->
              <div class="mt-3 pt-2.5 border-t border-border flex items-center justify-between text-[11px] text-text-muted">
                <span>{{ b.questions_count }} {{ b.questions_count === 1 ? 'question' : 'questions' }}</span>
                <span v-if="isOwner(b)" class="font-medium text-primary flex items-center gap-1">
                  <Sparkles class="h-3 w-3" /> Created by you
                </span>
                <span v-else class="text-text-subtle flex items-center gap-1">
                  <Lock class="h-3 w-3" /> {{ b.creator_name ? `By ${b.creator_name}` : 'Team Bank' }}
                </span>
              </div>
            </div>
          </div>
        </div>

        <!-- Right Column: Selected Bank Questions Management (8 cols) -->
        <div v-if="selectedBank" class="lg:col-span-8 bg-card border border-border rounded-2xl p-5 sm:p-7 shadow-xs space-y-6">
          
          <!-- Bank Header and Actions -->
          <div class="flex flex-col sm:flex-row sm:items-start justify-between gap-4 border-b border-border pb-5">
            <div class="min-w-0 flex-1">
              <div class="flex items-center gap-2 flex-wrap">
                <h2 class="text-lg sm:text-xl font-bold text-text-strong">{{ selectedBank.name }}</h2>
                <span v-if="selectedBank.category" class="px-2 py-0.5 text-xs font-semibold rounded-md bg-surface-raised text-text-muted border border-border">
                  {{ selectedBank.category }}
                </span>
              </div>
              <p v-if="selectedBank.description" class="text-xs sm:text-sm text-text-secondary mt-1.5">
                {{ selectedBank.description }}
              </p>

              <!-- Ownership note -->
              <div class="mt-2.5 flex items-center gap-2 text-xs">
                <span v-if="isOwner(selectedBank)" class="inline-flex items-center gap-1 font-semibold text-primary">
                  <CheckCircle class="h-3.5 w-3.5" /> You own this question bank (full edit permissions)
                </span>
                <span v-else class="inline-flex items-center gap-1 text-text-muted">
                  <Lock class="h-3.5 w-3.5 text-amber-500" />
                  Owned by <strong>{{ selectedBank.creator_name || 'another official' }}</strong> (Read-only. Only the creator can modify or delete it.)
                </span>
              </div>
            </div>

            <!-- Owner Action Buttons -->
            <div class="flex items-center gap-2 shrink-0">
              <template v-if="isOwner(selectedBank)">
                <button
                  type="button"
                  @click="openEditBank(selectedBank)"
                  class="inline-flex items-center gap-1 px-3 py-1.5 text-xs font-semibold rounded-lg border border-border bg-card hover:bg-surface-raised text-text-strong transition"
                >
                  <Edit2 class="h-3.5 w-3.5" />
                  <span>Edit Bank</span>
                </button>
                <button
                  type="button"
                  @click="deleteBank(selectedBank)"
                  class="inline-flex items-center gap-1 px-3 py-1.5 text-xs font-semibold rounded-lg border border-danger/20 text-danger hover:bg-danger-bg transition"
                >
                  <Trash2 class="h-3.5 w-3.5" />
                  <span>Delete</span>
                </button>
              </template>
              <div v-else class="text-[11px] text-text-subtle px-2 py-1 bg-surface-raised rounded border border-border flex items-center gap-1">
                <Lock class="h-3 w-3" /> Modifying locked
              </div>
            </div>
          </div>

          <!-- Questions Header -->
          <div class="flex items-center justify-between">
            <div>
              <h3 class="text-sm sm:text-base font-semibold text-text-strong">
                Questions ({{ selectedBank.questions?.length || 0 }})
              </h3>
              <p class="text-xs text-text-muted">
                Total Assessment Points: <strong>{{ selectedBank.total_points }} pts</strong>
              </p>
            </div>

            <button
              v-if="isOwner(selectedBank)"
              type="button"
              @click="openAddQuestion"
              class="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs sm:text-sm font-semibold rounded-lg bg-primary text-white hover:bg-primary-hover shadow-xs transition"
            >
              <Plus class="h-4 w-4" />
              <span>Add Question</span>
            </button>
          </div>

          <!-- Question Cards Feed -->
          <div v-if="!selectedBank.questions || selectedBank.questions.length === 0" class="py-12 text-center border-2 border-dashed border-border rounded-xl px-4">
            <HelpCircle class="h-8 w-8 text-text-subtle mx-auto mb-2" />
            <p class="text-sm font-medium text-text-strong">No questions in this bank yet</p>
            <p class="text-xs text-text-muted mt-1 max-w-sm mx-auto">
              Add single choice, multiple choice, true/false, or structured open-ended questions.
            </p>
            <button
              v-if="isOwner(selectedBank)"
              type="button"
              @click="openAddQuestion"
              class="mt-4 inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-semibold rounded-lg bg-primary text-white hover:bg-primary-hover shadow-xs transition"
            >
              <Plus class="h-4 w-4" />
              <span>Add First Question</span>
            </button>
          </div>

          <div v-else class="space-y-3">
            <div
              v-for="(q, qIndex) in selectedBank.questions"
              :key="q.id"
              class="border border-border rounded-xl p-4 bg-surface-raised space-y-3 transition"
            >
              <div class="flex items-start justify-between gap-3">
                <div class="min-w-0 flex-1">
                  <div class="flex items-center gap-2 flex-wrap mb-1">
                    <span class="text-xs font-bold text-primary">Q{{ qIndex + 1 }}</span>
                    <span class="px-2 py-0.5 text-[10px] font-semibold uppercase tracking-wider rounded bg-card border border-border text-text-strong">
                      {{ formatType(q.question_type) }}
                    </span>
                    <span class="px-2 py-0.5 text-[10px] font-medium rounded bg-card border border-border text-text-muted capitalize">
                      {{ q.difficulty }}
                    </span>
                    <span class="text-xs font-bold text-text-strong">
                      {{ q.points }} {{ q.points === 1 ? 'pt' : 'pts' }}
                    </span>
                  </div>
                  <p class="text-xs sm:text-sm text-text-strong font-medium whitespace-pre-line leading-relaxed">
                    {{ q.body }}
                  </p>
                </div>

                <!-- Question item edit/delete actions -->
                <div v-if="isOwner(selectedBank)" class="flex items-center gap-1 shrink-0">
                  <button
                    type="button"
                    @click="openEditQuestion(q)"
                    class="p-1.5 text-text-muted hover:text-primary rounded-md transition"
                    title="Edit question"
                  >
                    <Edit2 class="h-3.5 w-3.5" />
                  </button>
                  <button
                    type="button"
                    @click="deleteQuestion(q.id)"
                    class="p-1.5 text-text-muted hover:text-danger rounded-md transition"
                    title="Delete question"
                  >
                    <Trash2 class="h-3.5 w-3.5" />
                  </button>
                </div>
              </div>

              <!-- Options preview for MCQ / True False -->
              <div v-if="q.question_type !== 'structured' && q.options && q.options.length > 0" class="space-y-1.5 pt-1">
                <div
                  v-for="opt in q.options"
                  :key="opt.id"
                  :class="[
                    'flex items-center gap-2 text-xs px-2.5 py-1.5 rounded-lg border',
                    opt.is_correct
                      ? 'bg-success-bg/40 border-success-border text-success-text font-medium'
                      : 'bg-card border-border text-text-secondary'
                  ]"
                >
                  <span class="h-4 w-4 rounded-full flex items-center justify-center text-[10px] font-bold"
                    :class="opt.is_correct ? 'bg-success-text text-white' : 'bg-surface-raised border border-border text-text-muted'">
                    {{ opt.id.toUpperCase() }}
                  </span>
                  <span class="flex-1 truncate">{{ opt.text }}</span>
                  <span v-if="opt.is_correct" class="text-[10px] font-bold uppercase tracking-wider text-success-text">
                    Correct Answer
                  </span>
                </div>
              </div>

              <!-- Structured note -->
              <div v-else-if="q.question_type === 'structured'" class="p-2.5 rounded-lg bg-card border border-border text-xs text-text-muted">
                <div class="flex items-center gap-1.5 text-text-strong font-medium mb-0.5">
                  <FileText class="h-3.5 w-3.5 text-primary" />
                  <span>Structured Written Response</span>
                </div>
                <span>Candidate provides open-ended formatted text. No fixed options.</span>
                <p v-if="q.correct_answer" class="mt-1.5 text-[11px] text-text-secondary bg-surface-raised p-2 rounded border border-border">
                  <strong>Model answer / rubrics:</strong> {{ q.correct_answer }}
                </p>
              </div>

              <div v-if="q.explanation" class="text-[11px] text-text-muted italic pt-1 border-t border-border">
                Note: {{ q.explanation }}
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- ==================================================== -->
    <!-- CREATE / EDIT BANK MODAL -->
    <!-- ==================================================== -->
    <div
      v-if="showBankModal"
      class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-xs"
      @click.self="showBankModal = false"
    >
      <div class="bg-card border border-border rounded-2xl w-full max-w-md p-6 shadow-xl space-y-4">
        <h3 class="text-lg font-bold text-text-strong">
          {{ editingBankId ? 'Edit Question Bank' : 'Create Question Bank' }}
        </h3>

        <div class="space-y-3">
          <div>
            <label class="block text-xs font-semibold text-text-strong mb-1">Bank Title *</label>
            <input
              v-model="bankName"
              type="text"
              placeholder="e.g. React & TypeScript Fundamentals"
              class="w-full bg-card rounded-lg border border-border px-3 py-2 text-xs sm:text-sm text-text-strong focus:outline-none focus:ring-2 focus:ring-primary/20 transition"
            />
          </div>

          <div>
            <label class="block text-xs font-semibold text-text-strong mb-1">Category (Optional)</label>
            <input
              v-model="bankCategory"
              type="text"
              placeholder="e.g. Frontend Engineering, Product, Aptitude"
              class="w-full bg-card rounded-lg border border-border px-3 py-2 text-xs sm:text-sm text-text-strong focus:outline-none focus:ring-2 focus:ring-primary/20 transition"
            />
          </div>

          <div>
            <label class="block text-xs font-semibold text-text-strong mb-1">Description (Optional)</label>
            <textarea
              v-model="bankDescription"
              rows="3"
              placeholder="Brief summary of skills or topics evaluated in this question bank..."
              class="w-full bg-card rounded-lg border border-border px-3 py-2 text-xs sm:text-sm text-text-strong focus:outline-none focus:ring-2 focus:ring-primary/20 transition"
            ></textarea>
          </div>
        </div>

        <div class="flex items-center justify-end gap-2 pt-3 border-t border-border">
          <button
            type="button"
            @click="showBankModal = false"
            class="px-4 py-2 text-xs sm:text-sm font-semibold rounded-lg border border-border hover:bg-surface-raised text-text-strong transition"
          >
            Cancel
          </button>
          <button
            type="button"
            :disabled="savingBank"
            @click="saveBank"
            class="px-4 py-2 text-xs sm:text-sm font-semibold rounded-lg bg-primary text-white hover:bg-primary-hover disabled:opacity-50 transition shadow-xs"
          >
            {{ savingBank ? 'Saving...' : (editingBankId ? 'Update Bank' : 'Create Bank') }}
          </button>
        </div>
      </div>
    </div>

    <!-- ==================================================== -->
    <!-- CREATE / EDIT QUESTION MODAL -->
    <!-- ==================================================== -->
    <div
      v-if="showQuestionModal"
      class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-xs overflow-y-auto"
      @click.self="showQuestionModal = false"
    >
      <div class="bg-card border border-border rounded-2xl w-full max-w-xl p-6 shadow-xl space-y-4 my-8">
        <div class="flex items-center justify-between border-b border-border pb-3">
          <h3 class="text-base sm:text-lg font-bold text-text-strong">
            {{ editingQuestionId ? 'Edit Question' : 'Add Question' }}
          </h3>
          <span class="text-xs text-text-muted">In {{ selectedBank?.name }}</span>
        </div>

        <div v-if="questionError" class="p-3 rounded-lg bg-danger-bg text-danger border border-danger/20 text-xs flex items-center gap-2">
          <AlertCircle class="h-4 w-4 shrink-0" />
          <span>{{ questionError }}</span>
        </div>

        <div class="space-y-4 max-h-[70vh] overflow-y-auto pr-1">
          <!-- Question prompt -->
          <div>
            <label class="block text-xs font-semibold text-text-strong mb-1">Question Prompt *</label>
            <textarea
              v-model="qBody"
              rows="3"
              placeholder="Enter the question text or problem statement..."
              class="w-full bg-card rounded-lg border border-border px-3 py-2 text-xs sm:text-sm text-text-strong focus:outline-none focus:ring-2 focus:ring-primary/20 transition"
            ></textarea>
          </div>

          <!-- Type, Points & Difficulty Grid -->
          <div class="grid grid-cols-1 sm:grid-cols-3 gap-3">
            <div>
              <label class="block text-xs font-semibold text-text-strong mb-1">Question Type</label>
              <select
                v-model="qType"
                class="w-full bg-card rounded-lg border border-border px-3 py-2 text-xs sm:text-sm text-text-strong focus:outline-none focus:ring-2 focus:ring-primary/20 transition"
              >
                <option value="single_choice">Single Choice (1 Correct)</option>
                <option value="multiple_choice">Multiple Choice (Multiple Correct)</option>
                <option value="true_false">True / False</option>
                <option value="structured">Structured (Written Answer)</option>
              </select>
            </div>

            <div>
              <label class="block text-xs font-semibold text-text-strong mb-1">Points</label>
              <input
                v-model.number="qPoints"
                type="number"
                min="0.5"
                step="0.5"
                class="w-full bg-card rounded-lg border border-border px-3 py-2 text-xs sm:text-sm text-text-strong focus:outline-none focus:ring-2 focus:ring-primary/20 transition"
              />
            </div>

            <div>
              <label class="block text-xs font-semibold text-text-strong mb-1">Difficulty</label>
              <select
                v-model="qDifficulty"
                class="w-full bg-card rounded-lg border border-border px-3 py-2 text-xs sm:text-sm text-text-strong focus:outline-none focus:ring-2 focus:ring-primary/20 transition"
              >
                <option value="easy">Easy</option>
                <option value="medium">Medium</option>
                <option value="hard">Hard</option>
              </select>
            </div>
          </div>

          <!-- Dynamic Options Section based on Type -->
          <div v-if="qType === 'single_choice' || qType === 'multiple_choice'" class="space-y-3 pt-2">
            <div class="flex items-center justify-between">
              <label class="text-xs font-semibold text-text-strong">
                Options & Correct Answer(s) *
              </label>
              <button
                type="button"
                @click="addOption"
                class="text-xs font-semibold text-primary hover:underline"
              >
                + Add Option
              </button>
            </div>

            <div class="space-y-2">
              <div
                v-for="(opt, idx) in qOptions"
                :key="idx"
                class="flex items-center gap-2"
              >
                <button
                  type="button"
                  @click="handleOptionCheck(idx)"
                  :class="[
                    'h-6 w-6 rounded-md border flex items-center justify-center shrink-0 transition cursor-pointer',
                    opt.is_correct
                      ? 'bg-primary border-primary text-white'
                      : 'border-border bg-card text-transparent hover:border-text-muted'
                  ]"
                  :title="qType === 'single_choice' ? 'Mark as correct' : 'Toggle correct'"
                >
                  <Check class="h-3.5 w-3.5 stroke-[3]" />
                </button>

                <span class="text-xs font-bold text-text-muted uppercase w-4 text-center">
                  {{ opt.id }}
                </span>

                <input
                  v-model="opt.text"
                  type="text"
                  :placeholder="`Option ${opt.id.toUpperCase()} text...`"
                  class="flex-1 bg-card rounded-lg border border-border px-3 py-1.5 text-xs sm:text-sm text-text-strong focus:outline-none focus:ring-2 focus:ring-primary/20 transition"
                />

                <button
                  v-if="qOptions.length > 2"
                  type="button"
                  @click="removeOption(idx)"
                  class="text-text-subtle hover:text-danger p-1 text-sm font-bold"
                  title="Remove option"
                >
                  ×
                </button>
              </div>
            </div>
            <p class="text-[11px] text-text-muted">
              {{ qType === 'single_choice' ? 'Check the single radio/box corresponding to the correct answer.' : 'Check all options that are valid correct answers.' }}
            </p>
          </div>

          <!-- True / False Options -->
          <div v-else-if="qType === 'true_false'" class="space-y-2 pt-2">
            <label class="block text-xs font-semibold text-text-strong">Correct Answer *</label>
            <div class="flex gap-4">
              <label class="flex items-center gap-2 text-xs sm:text-sm text-text-strong cursor-pointer">
                <input
                  type="radio"
                  name="tfOption"
                  :checked="qOptions[0]?.is_correct"
                  @change="handleOptionCheck(0)"
                  class="text-primary focus:ring-primary"
                />
                True is correct
              </label>
              <label class="flex items-center gap-2 text-xs sm:text-sm text-text-strong cursor-pointer">
                <input
                  type="radio"
                  name="tfOption"
                  :checked="qOptions[1]?.is_correct"
                  @change="handleOptionCheck(1)"
                  class="text-primary focus:ring-primary"
                />
                False is correct
              </label>
            </div>
          </div>

          <!-- Structured Info -->
          <div v-else-if="qType === 'structured'" class="space-y-3 pt-2 p-3 bg-surface-raised rounded-xl border border-border">
            <div class="flex items-center gap-1.5 text-xs font-semibold text-text-strong">
              <FileText class="h-4 w-4 text-primary" />
              <span>Structured Question (Open-Ended)</span>
            </div>
            <p class="text-[11px] text-text-muted leading-relaxed">
              No multiple choice options are required. The candidate will be provided a rich response area during the assessment.
            </p>
            <div>
              <label class="block text-xs font-semibold text-text-strong mb-1">Model Answer / Evaluation Rubrics (Optional)</label>
              <textarea
                v-model="qCorrectAnswer"
                rows="2"
                placeholder="Key points or sample answer for manual scoring reference..."
                class="w-full bg-card rounded-lg border border-border px-3 py-1.5 text-xs text-text-strong focus:outline-none focus:ring-2 focus:ring-primary/20 transition"
              ></textarea>
            </div>
          </div>

          <!-- Explanation / Note -->
          <div>
            <label class="block text-xs font-semibold text-text-strong mb-1">Explanation / Candidate Feedback (Optional)</label>
            <textarea
              v-model="qExplanation"
              rows="2"
              placeholder="Explanation shown after the assessment is submitted..."
              class="w-full bg-card rounded-lg border border-border px-3 py-1.5 text-xs text-text-strong focus:outline-none focus:ring-2 focus:ring-primary/20 transition"
            ></textarea>
          </div>
        </div>

        <div class="flex items-center justify-end gap-2 pt-3 border-t border-border">
          <button
            type="button"
            @click="showQuestionModal = false"
            class="px-4 py-2 text-xs sm:text-sm font-semibold rounded-lg border border-border hover:bg-surface-raised text-text-strong transition"
          >
            Cancel
          </button>
          <button
            type="button"
            :disabled="savingQuestion"
            @click="saveQuestion"
            class="px-4 py-2 text-xs sm:text-sm font-semibold rounded-lg bg-primary text-white hover:bg-primary-hover disabled:opacity-50 transition shadow-xs"
          >
            {{ savingQuestion ? 'Saving...' : (editingQuestionId ? 'Update Question' : 'Save Question') }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
