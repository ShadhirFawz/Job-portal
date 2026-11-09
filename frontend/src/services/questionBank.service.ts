import { api } from '@/lib/api'
import type {
  QuestionBank,
  Question,
  CreateQuestionBankPayload,
  UpdateQuestionBankPayload,
  CreateQuestionPayload,
  UpdateQuestionPayload
} from '@/types/assessment'

export const questionBankService = {
  async getOrganizationBanks(organizationId: string): Promise<QuestionBank[]> {
    try {
      return await api.get<QuestionBank[]>(`questionbanks/organization/${organizationId}`)
    } catch (error) {
      console.error('Error fetching question banks:', error)
      return []
    }
  },

  async getBankById(bankId: string): Promise<QuestionBank | null> {
    try {
      return await api.get<QuestionBank>(`questionbanks/${bankId}`)
    } catch (error) {
      console.error('Error fetching question bank by id:', error)
      return null
    }
  },

  async createBank(
    organizationId: string,
    userId: string,
    payload: CreateQuestionBankPayload
  ): Promise<QuestionBank> {
    return await api.post<QuestionBank>(
      'questionbanks',
      payload,
      { organizationId, userId }
    )
  },

  async updateBank(
    bankId: string,
    userId: string,
    payload: UpdateQuestionBankPayload
  ): Promise<QuestionBank> {
    return await api.put<QuestionBank>(
      `questionbanks/${bankId}`,
      payload,
      { userId }
    )
  },

  async deleteBank(bankId: string, userId: string): Promise<void> {
    await api.delete(`questionbanks/${bankId}`, { userId })
  },

  async addQuestion(
    bankId: string,
    userId: string,
    payload: CreateQuestionPayload
  ): Promise<Question> {
    return await api.post<Question>(
      `questionbanks/${bankId}/questions`,
      payload,
      { userId }
    )
  },

  async updateQuestion(
    questionId: string,
    userId: string,
    payload: UpdateQuestionPayload
  ): Promise<Question> {
    return await api.put<Question>(
      `questionbanks/questions/${questionId}`,
      payload,
      { userId }
    )
  },

  async deleteQuestion(questionId: string, userId: string): Promise<void> {
    await api.delete(`questionbanks/questions/${questionId}`, { userId })
  }
}

