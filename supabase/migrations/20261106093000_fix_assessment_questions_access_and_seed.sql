-- =====================================================
-- Migration: Fix Assessment Questions RLS & Candidate Access
-- =====================================================
-- 1. Updates SELECT policy on public.questions to allow candidates
--    with an active attempt or on published job stages to read questions.
-- 2. Updates SELECT policy on public.question_banks similarly.
-- 3. Seeds starter questions for the Business Analysis Aptitude Quiz
--    (bank_id: '6990389e-86d9-4cd5-90a5-e472c9933d51') if empty.
-- =====================================================

-- 1. QUESTIONS SELECT POLICY
DROP POLICY IF EXISTS "Org members view questions" ON public.questions;
CREATE POLICY "Org members view questions"
  ON public.questions FOR SELECT
  USING (
    public.is_org_member(organization_id, auth.uid())
    OR EXISTS (
      SELECT 1 FROM public.quiz_attempts qa
      WHERE qa.bank_id = questions.bank_id
        AND qa.user_id = auth.uid()
    )
    OR EXISTS (
      SELECT 1 FROM public.job_selection_processes sp
      JOIN public.jobs j ON j.id = sp.job_id
      WHERE sp.question_bank_id = questions.bank_id
        AND j.status = 'published'
    )
  );

-- 2. QUESTION BANKS SELECT POLICY
DROP POLICY IF EXISTS "Org members view question banks" ON public.question_banks;
CREATE POLICY "Org members view question banks"
  ON public.question_banks FOR SELECT
  USING (
    public.is_org_member(organization_id, auth.uid())
    OR EXISTS (
      SELECT 1 FROM public.quiz_attempts qa
      WHERE qa.bank_id = question_banks.id
        AND qa.user_id = auth.uid()
    )
    OR EXISTS (
      SELECT 1 FROM public.job_selection_processes sp
      JOIN public.jobs j ON j.id = sp.job_id
      WHERE sp.question_bank_id = question_banks.id
        AND j.status = 'published'
    )
  );

-- 3. SEED QUESTIONS FOR BUSINESS ANALYSIS APTITUDE QUIZ IF EMPTY
DO $$
DECLARE
  v_bank_id UUID := '6990389e-86d9-4cd5-90a5-e472c9933d51';
  v_org_id UUID;
  v_user_id UUID;
  v_q_count INT;
BEGIN
  -- Verify bank exists
  SELECT organization_id, created_by INTO v_org_id, v_user_id
  FROM public.question_banks
  WHERE id = v_bank_id;

  IF v_org_id IS NOT NULL THEN
    SELECT count(*) INTO v_q_count
    FROM public.questions
    WHERE bank_id = v_bank_id AND deleted_at IS NULL;

    IF v_q_count = 0 THEN
      -- Question 1: Single choice
      INSERT INTO public.questions (
        bank_id, organization_id, created_by, body, question_type,
        options, correct_answer, explanation, difficulty, points, order_index, is_active
      ) VALUES (
        v_bank_id, v_org_id, v_user_id,
        'Which of the following best defines a Functional Requirement in business analysis?',
        'single_choice',
        '[
          {"id": "a", "text": "Specific behaviors, features, and capabilities the system must perform to satisfy business needs", "is_correct": true},
          {"id": "b", "text": "Underlying server architecture and network hardware bandwidth requirements", "is_correct": false},
          {"id": "c", "text": "Budget constraints and project staffing allocation guidelines", "is_correct": false},
          {"id": "d", "text": "Legal liabilities and copyright licensing agreements", "is_correct": false}
        ]'::jsonb,
        'a',
        'Functional requirements describe what the system should do, capturing specific behaviors, features, and interactions.',
        'medium', 2, 0, true
      );

      -- Question 2: Single choice
      INSERT INTO public.questions (
        bank_id, organization_id, created_by, body, question_type,
        options, correct_answer, explanation, difficulty, points, order_index, is_active
      ) VALUES (
        v_bank_id, v_org_id, v_user_id,
        'What is the primary purpose of a SWOT analysis during project discovery?',
        'single_choice',
        '[
          {"id": "a", "text": "To identify Strengths, Weaknesses, Opportunities, and Threats for strategic decision-making", "is_correct": true},
          {"id": "b", "text": "To compile and execute software unit testing suites", "is_correct": false},
          {"id": "c", "text": "To estimate database storage schema sizing", "is_correct": false},
          {"id": "d", "text": "To assign individual code commits to sprint reviewers", "is_correct": false}
        ]'::jsonb,
        'a',
        'SWOT analysis assesses internal strengths and weaknesses along with external opportunities and threats.',
        'easy', 2, 1, true
      );

      -- Question 3: Multiple choice
      INSERT INTO public.questions (
        bank_id, organization_id, created_by, body, question_type,
        options, correct_answer, explanation, difficulty, points, order_index, is_active
      ) VALUES (
        v_bank_id, v_org_id, v_user_id,
        'Which of the following are widely recognized requirements elicitation techniques? (Select all that apply)',
        'multiple_choice',
        '[
          {"id": "a", "text": "Stakeholder interviews and one-on-one sessions", "is_correct": true},
          {"id": "b", "text": "Collaborative workshops and focus groups", "is_correct": true},
          {"id": "c", "text": "Document analysis and reviewing existing systems", "is_correct": true},
          {"id": "d", "text": "Compiling source code binaries into machine executables", "is_correct": false}
        ]'::jsonb,
        '["a","b","c"]',
        'Interviews, workshops, and document analysis are standard elicitation techniques described in the BABOK guide.',
        'medium', 3, 2, true
      );

      -- Question 4: True / False
      INSERT INTO public.questions (
        bank_id, organization_id, created_by, body, question_type,
        options, correct_answer, explanation, difficulty, points, order_index, is_active
      ) VALUES (
        v_bank_id, v_org_id, v_user_id,
        'In Agile methodologies, user stories typically follow the template: "As a [role], I want [capability], so that [benefit]".',
        'true_false',
        '[
          {"id": "true", "text": "True", "is_correct": true},
          {"id": "false", "text": "False", "is_correct": false}
        ]'::jsonb,
        'true',
        'This template helps keep the focus on user value and business objectives.',
        'easy', 1, 3, true
      );

      -- Question 5: Structured
      INSERT INTO public.questions (
        bank_id, organization_id, created_by, body, question_type,
        options, correct_answer, explanation, difficulty, points, order_index, is_active
      ) VALUES (
        v_bank_id, v_org_id, v_user_id,
        'Explain the fundamental difference between a Business Requirement and a Solution/Technical Requirement in 2-3 sentences.',
        'structured',
        '[]'::jsonb,
        'Business requirements define the high-level goals and problems the organization aims to solve. Solution/Technical requirements specify the detailed capabilities, system features, and architectural design that must be implemented to fulfill those business goals.',
        'Business requirements state the "what and why" from the business view, while solution requirements state the "how" of the product.',
        'medium', 2, 4, true
      );
    END IF;
  END IF;
END $$;

