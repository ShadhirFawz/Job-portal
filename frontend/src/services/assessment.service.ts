import { api } from '@/lib/api'
import type {
  QuizAttempt,
  StartAssessmentPayload,
  SubmitAssessmentPayload
} from '@/types/assessment'

export const assessmentService = {
  async startAssessment(userId: string, payload: StartAssessmentPayload): Promise<QuizAttempt> {
    return await api.post<QuizAttempt>('assessments/start', payload, { userId })
  },

  async getAttempt(attemptId: string, userId: string): Promise<QuizAttempt | null> {
    try {
      return await api.get<QuizAttempt>(`assessments/${attemptId}`, { userId })
    } catch (error) {
      console.error('Error fetching assessment attempt:', error)
      return null
    }
  },

  async getStageAttempt(selectionProcessId: string, userId: string): Promise<QuizAttempt | null> {
    try {
      return await api.get<QuizAttempt>(`assessments/stage/${selectionProcessId}`, { userId })
    } catch (error) {
      console.error('Error fetching user stage attempt:', error)
      return null
    }
  },

  async submitAssessment(userId: string, payload: SubmitAssessmentPayload): Promise<QuizAttempt> {
    return await api.post<QuizAttempt>('assessments/submit', payload, { userId })
  }
}

