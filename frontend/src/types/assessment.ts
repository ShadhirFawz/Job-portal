export type QuestionType = 'single_choice' | 'multiple_choice' | 'true_false' | 'structured'

export type DifficultyLevel = 'easy' | 'medium' | 'hard'

export interface QuestionOption {
  id: string
  text: string
  is_correct: boolean
}

export interface CandidateQuestionOption {
  id: string
  text: string
}

export interface Question {
  id: string
  bank_id: string
  organization_id: string
  created_by: string
  body: string
  question_type: QuestionType
  options?: QuestionOption[]
  correct_answer?: string | null
  explanation?: string | null
  difficulty: DifficultyLevel
  points: number
  order_index: number
  is_active: boolean
  created_at: string
  updated_at: string
}

export interface CandidateQuestion {
  id: string
  body: string
  question_type: QuestionType
  options?: CandidateQuestionOption[]
  points: number
  order_index: number
}

export interface QuestionBank {
  id: string
  organization_id: string
  created_by: string
  creator_name?: string | null
  name: string
  description?: string | null
  category?: string | null
  questions_count: number
  total_points: number
  questions?: Question[]
  created_at: string
  updated_at: string
}

export interface CreateQuestionBankPayload {
  name: string
  description?: string
  category?: string
  questions?: {
    body: string
    question_type: QuestionType
    options?: QuestionOption[]
    correct_answer?: string
    explanation?: string
    difficulty?: DifficultyLevel
    points?: number
    order_index?: number
  }[]
}

export interface UpdateQuestionBankPayload {
  name?: string
  description?: string
  category?: string
}

export interface CreateQuestionPayload {
  body: string
  question_type: QuestionType
  options?: QuestionOption[]
  correct_answer?: string
  explanation?: string
  difficulty?: DifficultyLevel
  points?: number
  order_index?: number
}

export interface UpdateQuestionPayload {
  body?: string
  question_type?: QuestionType
  options?: QuestionOption[]
  correct_answer?: string
  explanation?: string
  difficulty?: DifficultyLevel
  points?: number
  order_index?: number
}

export interface QuestionOptionResult {
  id: string
  text: string
  is_correct: boolean
}

export interface QuizAnswerDetail {
  question_id: string
  question_body: string
  question_type: string
  options?: QuestionOptionResult[]
  answer?: any
  is_correct?: boolean | null
  points_awarded: number
  max_points: number
  explanation?: string | null
}

export interface QuizAttempt {
  id: string
  job_id: string
  selection_process_id: string
  application_id?: string | null
  bank_id?: string | null
  user_id: string
  status: 'not_started' | 'in_progress' | 'submitted' | 'graded' | 'expired'
  started_at: string
  submitted_at?: string | null
  expires_at?: string | null
  time_limit_minutes?: number | null
  score: number
  max_score: number
  percentage: number
  passing_score?: number | null
  passed?: boolean | null
  stage_title?: string | null
  job_title?: string | null
  candidate_name?: string | null
  candidate_email?: string | null
  questions?: CandidateQuestion[]
  answers?: QuizAnswerDetail[]
}

export interface StartAssessmentPayload {
  job_id: string
  selection_process_id: string
  application_id?: string | null
}

export interface SubmitAnswerItem {
  question_id: string
  answer: any
}

export interface SubmitAssessmentPayload {
  attempt_id: string
  answers: SubmitAnswerItem[]
}

