import { api } from '@/lib/api'
import { supabase } from '@/lib/supabase'
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
    // 1. Try Supabase query with joined candidate and process details
    try {
      const { data: attemptRow, error: attemptErr } = await supabase
        .from('quiz_attempts')
        .select(`
          *,
          selection_process:job_selection_processes(id, title, process_type, passing_score, time_limit_minutes),
          job:jobs(id, title),
          candidate:users!user_id(id, email, first_name, last_name, display_name)
        `)
        .eq('id', attemptId)
        .single()

      if (!attemptErr && attemptRow) {
        // Fetch questions from question bank
        let questions: any[] = []
        if (attemptRow.bank_id) {
          const { data: qData } = await supabase
            .from('questions')
            .select('*')
            .eq('bank_id', attemptRow.bank_id)
            .is('deleted_at', null)
            .order('order_index', { ascending: true })
          questions = qData || []
        }
        const questionMap = new Map(questions.map((q: any) => [q.id, q]))

        // Fetch answers
        const { data: answersData } = await supabase
          .from('quiz_answers')
          .select('*')
          .eq('attempt_id', attemptId)

        const answerDetails = (answersData || []).map((ans: any) => {
          const q = questionMap.get(ans.question_id)
          return {
            question_id: ans.question_id,
            question_body: q?.body || '',
            question_type: q?.question_type || 'single_choice',
            options: q?.options || [],
            answer: ans.answer,
            is_correct: ans.is_correct,
            points_awarded: Number(ans.points_awarded ?? 0),
            max_points: Number(q?.points ?? 1),
            explanation: q?.explanation
          }
        })

        const candName = attemptRow.candidate?.display_name ||
          `${attemptRow.candidate?.first_name || ''} ${attemptRow.candidate?.last_name || ''}`.trim() ||
          attemptRow.candidate?.email

        return {
          id: attemptRow.id,
          job_id: attemptRow.job_id,
          selection_process_id: attemptRow.selection_process_id,
          application_id: attemptRow.application_id,
          bank_id: attemptRow.bank_id,
          user_id: attemptRow.user_id,
          status: attemptRow.status,
          started_at: attemptRow.started_at,
          submitted_at: attemptRow.submitted_at,
          expires_at: attemptRow.expires_at,
          time_limit_minutes: attemptRow.selection_process?.time_limit_minutes,
          score: Number(attemptRow.score ?? 0),
          max_score: Number(attemptRow.max_score ?? 0),
          percentage: Number(attemptRow.percentage ?? 0),
          passing_score: attemptRow.selection_process?.passing_score,
          passed: attemptRow.passed,
          stage_title: attemptRow.selection_process?.title || 'Quiz Assessment',
          job_title: attemptRow.job?.title,
          candidate_name: candName,
          candidate_email: attemptRow.candidate?.email,
          answers: answerDetails,
          questions: questions
        }
      }
    } catch (sbErr) {
      console.warn('Supabase getAttempt direct fetch failed, falling back to API:', sbErr)
    }

    // 2. Fallback to backend API
    try {
      return await api.get<QuizAttempt>(`assessments/${attemptId}`, { userId })
    } catch (error) {
      console.error('Error fetching assessment attempt from API:', error)
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
  },

  async getJobAttempts(jobId: string, userId: string): Promise<QuizAttempt[]> {
    // 1. Try Supabase RPC get_job_quiz_attempts directly (instant, SECURITY DEFINER)
    try {
      const { data, error } = await supabase.rpc('get_job_quiz_attempts', { p_job_id: jobId })
      if (!error && Array.isArray(data) && data.length > 0) {
        return data as QuizAttempt[]
      }
    } catch (rpcErr) {
      console.warn('Supabase rpc get_job_quiz_attempts failed:', rpcErr)
    }

    // 2. Try direct Supabase table select with joins
    try {
      const { data, error } = await supabase
        .from('quiz_attempts')
        .select(`
          id,
          job_id,
          selection_process_id,
          application_id,
          bank_id,
          user_id,
          status,
          started_at,
          submitted_at,
          expires_at,
          score,
          max_score,
          percentage,
          passed,
          selection_process:job_selection_processes(id, title, process_type, passing_score, time_limit_minutes),
          job:jobs(id, title),
          candidate:users!user_id(id, email, first_name, last_name, display_name)
        `)
        .eq('job_id', jobId)
        .order('created_at', { ascending: false })

      if (!error && Array.isArray(data) && data.length > 0) {
        return data.map((row: any) => ({
          id: row.id,
          job_id: row.job_id,
          selection_process_id: row.selection_process_id,
          application_id: row.application_id,
          bank_id: row.bank_id,
          user_id: row.user_id,
          status: row.status,
          started_at: row.started_at,
          submitted_at: row.submitted_at,
          expires_at: row.expires_at,
          time_limit_minutes: row.selection_process?.time_limit_minutes,
          score: Number(row.score ?? 0),
          max_score: Number(row.max_score ?? 0),
          percentage: Number(row.percentage ?? 0),
          passing_score: row.selection_process?.passing_score,
          passed: row.passed,
          stage_title: row.selection_process?.title || 'Quiz Assessment',
          job_title: row.job?.title,
          candidate_name: row.candidate?.display_name ||
            `${row.candidate?.first_name || ''} ${row.candidate?.last_name || ''}`.trim() ||
            row.candidate?.email,
          candidate_email: row.candidate?.email
        }))
      }
    } catch (sbErr) {
      console.warn('Supabase direct query failed:', sbErr)
    }

    // 3. Fallback to backend API
    try {
      return await api.get<QuizAttempt[]>(`assessments/job/${jobId}`, { userId })
    } catch (error) {
      console.error('Error fetching job assessment attempts from API:', error)
      return []
    }
  },

  async getApplicationAttempt(applicationId: string, userId: string): Promise<QuizAttempt | null> {
    try {
      return await api.get<QuizAttempt>(`assessments/application/${applicationId}`, { userId })
    } catch (error) {
      console.error('Error fetching application assessment attempt:', error)
      return null
    }
  }
}

