-- =====================================================
-- Migration: Structured Questions Manual Review Workflow
-- =====================================================

-- 1. Add review tracking columns to quiz_attempts
ALTER TABLE public.quiz_attempts
  ADD COLUMN IF NOT EXISTS requires_manual_review BOOLEAN DEFAULT false,
  ADD COLUMN IF NOT EXISTS reviewed_by UUID REFERENCES public.users(id) ON DELETE SET NULL,
  ADD COLUMN IF NOT EXISTS review_started_at TIMESTAMPTZ,
  ADD COLUMN IF NOT EXISTS reviewed_at TIMESTAMPTZ;

-- 2. Add reviewer columns to quiz_answers
ALTER TABLE public.quiz_answers
  ADD COLUMN IF NOT EXISTS reviewer_feedback TEXT,
  ADD COLUMN IF NOT EXISTS reviewed_by UUID REFERENCES public.users(id) ON DELETE SET NULL,
  ADD COLUMN IF NOT EXISTS reviewed_at TIMESTAMPTZ;

-- 3. Update RLS policies to allow org members to update attempts & answers during review
DROP POLICY IF EXISTS "Org members update job attempts" ON public.quiz_attempts;
CREATE POLICY "Org members update job attempts"
  ON public.quiz_attempts FOR UPDATE
  USING (
    EXISTS (
      SELECT 1 FROM public.jobs j
      WHERE j.id = quiz_attempts.job_id
        AND (
          auth.uid() IS NULL
          OR j.created_by = auth.uid()
          OR public.is_org_member(j.organization_id, auth.uid())
          OR public.is_org_admin(j.organization_id, auth.uid())
        )
    )
  )
  WITH CHECK (
    EXISTS (
      SELECT 1 FROM public.jobs j
      WHERE j.id = quiz_attempts.job_id
        AND (
          auth.uid() IS NULL
          OR j.created_by = auth.uid()
          OR public.is_org_member(j.organization_id, auth.uid())
          OR public.is_org_admin(j.organization_id, auth.uid())
        )
    )
  );

DROP POLICY IF EXISTS "Org members update job answers" ON public.quiz_answers;
CREATE POLICY "Org members update job answers"
  ON public.quiz_answers FOR UPDATE
  USING (
    EXISTS (
      SELECT 1 FROM public.quiz_attempts a
      JOIN public.jobs j ON j.id = a.job_id
      WHERE a.id = quiz_answers.attempt_id
        AND (
          auth.uid() IS NULL
          OR j.created_by = auth.uid()
          OR public.is_org_member(j.organization_id, auth.uid())
          OR public.is_org_admin(j.organization_id, auth.uid())
        )
    )
  )
  WITH CHECK (
    EXISTS (
      SELECT 1 FROM public.quiz_attempts a
      JOIN public.jobs j ON j.id = a.job_id
      WHERE a.id = quiz_answers.attempt_id
        AND (
          auth.uid() IS NULL
          OR j.created_by = auth.uid()
          OR public.is_org_member(j.organization_id, auth.uid())
          OR public.is_org_admin(j.organization_id, auth.uid())
        )
    )
  );

-- 4. Update submit_quiz_assessment to set requires_manual_review and passed = NULL when structured questions exist
CREATE OR REPLACE FUNCTION public.submit_quiz_assessment(
  p_attempt_id UUID,
  p_user_id    UUID,
  p_answers    JSONB  -- array of {"question_id": "...", "answer": "..."}
)
RETURNS JSONB
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_attempt RECORD;
  v_process RECORD;
  v_q RECORD;
  v_ans_item JSONB;
  v_q_id UUID;
  v_raw_ans JSONB;
  v_ans_str TEXT;
  v_is_correct BOOLEAN;
  v_points_awarded DECIMAL(6,2);
  v_total_score DECIMAL(8,2) := 0;
  v_max_score DECIMAL(8,2) := 0;
  v_percentage DECIMAL(5,2) := 0;
  v_passed BOOLEAN := NULL;
  v_passing_score INT := 70;
  v_has_structured BOOLEAN := false;
  v_final_status TEXT := 'graded';
  v_opt_correct TEXT;
  v_correct_ids TEXT[];
  v_cand_ids TEXT[];
  v_result JSONB;
BEGIN
  -- 1. Fetch attempt
  SELECT * INTO v_attempt
  FROM public.quiz_attempts
  WHERE id = p_attempt_id;

  IF NOT FOUND THEN
    RAISE EXCEPTION 'Assessment attempt not found.';
  END IF;

  IF v_attempt.user_id <> p_user_id THEN
    RAISE EXCEPTION 'You are not authorized to submit this assessment.';
  END IF;

  -- 2. Fetch selection process
  SELECT * INTO v_process
  FROM public.job_selection_processes
  WHERE id = v_attempt.selection_process_id;

  IF v_process.passing_score IS NOT NULL THEN
    v_passing_score := v_process.passing_score;
  END IF;

  -- 3. Calculate max score and detect if bank has structured questions
  SELECT coalesce(SUM(points), 0), coalesce(bool_or(question_type = 'structured'), false)
  INTO v_max_score, v_has_structured
  FROM public.questions
  WHERE bank_id = v_attempt.bank_id AND deleted_at IS NULL;

  IF v_has_structured THEN
    v_final_status := 'submitted';
    v_passed := NULL; -- Cannot determine pass/fail until manual review is completed
  ELSE
    v_final_status := 'graded';
  END IF;

  -- 4. Process each submitted answer
  IF p_answers IS NOT NULL AND jsonb_typeof(p_answers) = 'array' THEN
    FOR v_ans_item IN SELECT * FROM jsonb_array_elements(p_answers)
    LOOP
      v_q_id := (v_ans_item->>'question_id')::UUID;
      v_raw_ans := v_ans_item->'answer';
      
      -- Find corresponding question
      SELECT * INTO v_q
      FROM public.questions
      WHERE id = v_q_id AND bank_id = v_attempt.bank_id AND deleted_at IS NULL;

      IF FOUND THEN
        v_is_correct := NULL;
        v_points_awarded := 0;

        IF v_q.question_type = 'single_choice' OR v_q.question_type = 'true_false' THEN
          -- Candidate answer as plain string
          IF jsonb_typeof(v_raw_ans) = 'string' THEN
            v_ans_str := trim(both '"' from v_raw_ans::text);
          ELSE
            v_ans_str := trim(v_raw_ans::text);
          END IF;

          -- Find correct option id
          SELECT opt->>'id' INTO v_opt_correct
          FROM jsonb_array_elements(v_q.options) opt
          WHERE (opt->>'is_correct')::boolean = true
          LIMIT 1;

          -- Fallback to correct_answer column if options lack is_correct
          IF v_opt_correct IS NULL AND v_q.correct_answer IS NOT NULL THEN
            v_opt_correct := trim(v_q.correct_answer);
          END IF;

          IF v_opt_correct IS NOT NULL AND lower(v_ans_str) = lower(v_opt_correct) THEN
            v_is_correct := true;
            v_points_awarded := v_q.points;
          ELSE
            v_is_correct := false;
            v_points_awarded := 0;
          END IF;

        ELSIF v_q.question_type = 'multiple_choice' THEN
          -- Correct option IDs array
          SELECT array_agg(lower(opt->>'id') ORDER BY lower(opt->>'id')) INTO v_correct_ids
          FROM jsonb_array_elements(v_q.options) opt
          WHERE (opt->>'is_correct')::boolean = true;

          -- Candidate selected IDs array
          IF jsonb_typeof(v_raw_ans) = 'array' THEN
            SELECT array_agg(lower(trim(both '"' from elem::text)) ORDER BY lower(trim(both '"' from elem::text)))
            INTO v_cand_ids
            FROM jsonb_array_elements(v_raw_ans) elem
            WHERE trim(both '"' from elem::text) <> '';
          ELSE
            v_cand_ids := ARRAY[]::TEXT[];
          END IF;

          IF v_correct_ids IS NOT NULL AND v_cand_ids IS NOT NULL AND v_correct_ids = v_cand_ids THEN
            v_is_correct := true;
            v_points_awarded := v_q.points;
          ELSE
            v_is_correct := false;
            v_points_awarded := 0;
          END IF;

        ELSIF v_q.question_type = 'structured' THEN
          v_is_correct := NULL;
          v_points_awarded := 0;
        END IF;

        v_total_score := v_total_score + v_points_awarded;

        -- Upsert answer row into quiz_answers table
        INSERT INTO public.quiz_answers (
          attempt_id, question_id, answer, is_correct, points_awarded, answered_at
        ) VALUES (
          p_attempt_id, v_q_id, v_raw_ans, v_is_correct, v_points_awarded, NOW()
        )
        ON CONFLICT (attempt_id, question_id) DO UPDATE SET
          answer = EXCLUDED.answer,
          is_correct = EXCLUDED.is_correct,
          points_awarded = EXCLUDED.points_awarded,
          answered_at = EXCLUDED.answered_at;
      END IF;
    END LOOP;
  END IF;

  -- 5. Calculate percentage
  IF v_max_score > 0 THEN
    v_percentage := round((v_total_score / v_max_score) * 100, 2);
  ELSE
    v_percentage := 0;
  END IF;

  IF NOT v_has_structured THEN
    v_passed := v_percentage >= v_passing_score;
  END IF;

  -- 6. Update quiz_attempts
  UPDATE public.quiz_attempts SET
    status = v_final_status::attempt_status,
    submitted_at = NOW(),
    score = v_total_score,
    max_score = v_max_score,
    percentage = v_percentage,
    passed = v_passed,
    requires_manual_review = v_has_structured,
    updated_at = NOW()
  WHERE id = p_attempt_id;

  -- 7. Update job_applications score if linked
  IF v_attempt.application_id IS NOT NULL THEN
    BEGIN
      UPDATE public.job_applications SET
        score = v_percentage,
        current_process_id = v_attempt.selection_process_id,
        updated_at = NOW()
      WHERE id = v_attempt.application_id;
    EXCEPTION WHEN OTHERS THEN
      -- non-fatal
    END;
  END IF;

  -- Return updated attempt row
  SELECT to_jsonb(a) INTO v_result
  FROM public.quiz_attempts a
  WHERE a.id = p_attempt_id;

  RETURN v_result;
END;
$$;

-- 5. Stored Procedure: start_quiz_review (Handles single reviewer session lock)
CREATE OR REPLACE FUNCTION public.start_quiz_review(
  p_attempt_id  UUID,
  p_reviewer_id UUID
)
RETURNS JSONB
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_attempt RECORD;
  v_job RECORD;
  v_current_reviewer RECORD;
  v_caller_name TEXT;
  v_current_name TEXT;
BEGIN
  -- 1. Check attempt exists
  SELECT * INTO v_attempt
  FROM public.quiz_attempts
  WHERE id = p_attempt_id;

  IF NOT FOUND THEN
    RETURN jsonb_build_object('success', false, 'can_review', false, 'message', 'Assessment attempt not found.');
  END IF;

  -- 2. Verify authorization: reviewer must be org member or admin or job creator
  SELECT * INTO v_job FROM public.jobs WHERE id = v_attempt.job_id;
  IF FOUND THEN
    IF v_job.created_by <> p_reviewer_id
       AND NOT public.is_org_member(v_job.organization_id, p_reviewer_id)
       AND NOT public.is_org_admin(v_job.organization_id, p_reviewer_id) THEN
      RETURN jsonb_build_object('success', false, 'can_review', false, 'message', 'You are not authorized to review this assessment.');
    END IF;
  END IF;

  -- 3. Check review lock
  IF v_attempt.reviewed_by IS NOT NULL AND v_attempt.reviewed_by <> p_reviewer_id THEN
    -- Locked by another reviewer
    SELECT coalesce(nullif(trim(concat(first_name, ' ', last_name)), ''), display_name, email) AS name, email
    INTO v_current_reviewer
    FROM public.users
    WHERE id = v_attempt.reviewed_by;

    v_current_name := coalesce(v_current_reviewer.name, 'Another organization official');

    RETURN jsonb_build_object(
      'success', true,
      'can_review', false,
      'is_locked', true,
      'locked_by_id', v_attempt.reviewed_by,
      'locked_by_name', v_current_name,
      'locked_by_email', v_current_reviewer.email,
      'review_started_at', v_attempt.review_started_at,
      'message', format('This assessment review was started by %s and only they can complete the review.', v_current_name)
    );
  END IF;

  -- 4. Either not claimed yet or already claimed by p_reviewer_id
  IF v_attempt.reviewed_by IS NULL THEN
    UPDATE public.quiz_attempts
    SET reviewed_by = p_reviewer_id,
        review_started_at = NOW(),
        updated_at = NOW()
    WHERE id = p_attempt_id;
  END IF;

  SELECT coalesce(nullif(trim(concat(first_name, ' ', last_name)), ''), display_name, email) AS name
  INTO v_caller_name
  FROM public.users
  WHERE id = p_reviewer_id;

  RETURN jsonb_build_object(
    'success', true,
    'can_review', true,
    'is_locked', false,
    'locked_by_id', p_reviewer_id,
    'locked_by_name', v_caller_name,
    'review_started_at', coalesce(v_attempt.review_started_at, NOW()),
    'message', 'Review session active.'
  );
END;
$$;

GRANT EXECUTE ON FUNCTION public.start_quiz_review(UUID, UUID) TO authenticated, service_role, anon;

-- 6. Stored Procedure: submit_structured_review (Submits marks and finalizes paper)
CREATE OR REPLACE FUNCTION public.submit_structured_review(
  p_attempt_id  UUID,
  p_reviewer_id UUID,
  p_reviews     JSONB -- array of {"question_id": "...", "points_awarded": 5, "reviewer_feedback": "..."}
)
RETURNS JSONB
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_attempt RECORD;
  v_job RECORD;
  v_process RECORD;
  v_review_item JSONB;
  v_q_id UUID;
  v_points DECIMAL(6,2);
  v_feedback TEXT;
  v_q RECORD;
  v_is_correct BOOLEAN;
  v_total_score DECIMAL(8,2) := 0;
  v_max_score DECIMAL(8,2) := 0;
  v_percentage DECIMAL(5,2) := 0;
  v_passed BOOLEAN := false;
  v_passing_score INT := 70;
  v_result JSONB;
BEGIN
  -- 1. Fetch attempt
  SELECT * INTO v_attempt
  FROM public.quiz_attempts
  WHERE id = p_attempt_id;

  IF NOT FOUND THEN
    RAISE EXCEPTION 'Assessment attempt not found.';
  END IF;

  -- 2. Verify authorization
  SELECT * INTO v_job FROM public.jobs WHERE id = v_attempt.job_id;
  IF FOUND THEN
    IF v_job.created_by <> p_reviewer_id
       AND NOT public.is_org_member(v_job.organization_id, p_reviewer_id)
       AND NOT public.is_org_admin(v_job.organization_id, p_reviewer_id) THEN
      RAISE EXCEPTION 'You are not authorized to review this assessment.';
    END IF;
  END IF;

  -- 3. Verify single reviewer lock: only the reviewer who started the session can finish it
  IF v_attempt.reviewed_by IS NOT NULL AND v_attempt.reviewed_by <> p_reviewer_id THEN
    RAISE EXCEPTION 'This assessment review is locked by another official.';
  END IF;

  -- Claim if not already set
  IF v_attempt.reviewed_by IS NULL THEN
    UPDATE public.quiz_attempts
    SET reviewed_by = p_reviewer_id,
        review_started_at = NOW()
    WHERE id = p_attempt_id;
  END IF;

  -- 4. Apply marks for each structured question
  IF p_reviews IS NOT NULL AND jsonb_typeof(p_reviews) = 'array' THEN
    FOR v_review_item IN SELECT * FROM jsonb_array_elements(p_reviews)
    LOOP
      v_q_id := (v_review_item->>'question_id')::UUID;
      v_points := coalesce((v_review_item->>'points_awarded')::DECIMAL(6,2), 0);
      v_feedback := v_review_item->>'reviewer_feedback';

      -- Fetch question to validate points bound
      SELECT * INTO v_q
      FROM public.questions
      WHERE id = v_q_id AND bank_id = v_attempt.bank_id AND deleted_at IS NULL;

      IF FOUND THEN
        -- Boundary check: 0 <= points_awarded <= question.points
        IF v_points < 0 THEN
          v_points := 0;
        ELSIF v_points > v_q.points THEN
          RAISE EXCEPTION 'Awarded points (%) exceeds maximum question points (%) for question %',
            v_points, v_q.points, v_q.body;
        END IF;

        IF v_points = v_q.points THEN
          v_is_correct := true;
        ELSIF v_points = 0 THEN
          v_is_correct := false;
        ELSE
          v_is_correct := true; -- Partial credit awarded
        END IF;

        -- Update or insert answer record
        INSERT INTO public.quiz_answers (
          attempt_id, question_id, points_awarded, is_correct, reviewer_feedback, reviewed_by, reviewed_at, answered_at
        ) VALUES (
          p_attempt_id, v_q_id, v_points, v_is_correct, v_feedback, p_reviewer_id, NOW(), NOW()
        )
        ON CONFLICT (attempt_id, question_id) DO UPDATE SET
          points_awarded = EXCLUDED.points_awarded,
          is_correct = EXCLUDED.is_correct,
          reviewer_feedback = EXCLUDED.reviewer_feedback,
          reviewed_by = EXCLUDED.reviewed_by,
          reviewed_at = EXCLUDED.reviewed_at;
      END IF;
    END LOOP;
  END IF;

  -- 5. Calculate total score and max score across all questions in bank
  SELECT coalesce(SUM(points), 0) INTO v_max_score
  FROM public.questions
  WHERE bank_id = v_attempt.bank_id AND deleted_at IS NULL;

  SELECT coalesce(SUM(points_awarded), 0) INTO v_total_score
  FROM public.quiz_answers
  WHERE attempt_id = p_attempt_id;

  IF v_max_score > 0 THEN
    v_percentage := round((v_total_score / v_max_score) * 100, 2);
  ELSE
    v_percentage := 0;
  END IF;

  -- Fetch passing score
  SELECT * INTO v_process
  FROM public.job_selection_processes
  WHERE id = v_attempt.selection_process_id;

  IF v_process.passing_score IS NOT NULL THEN
    v_passing_score := v_process.passing_score;
  END IF;

  v_passed := (v_percentage >= v_passing_score);

  -- 6. Mark paper as graded and reviewed
  UPDATE public.quiz_attempts SET
    score = v_total_score,
    max_score = v_max_score,
    percentage = v_percentage,
    passed = v_passed,
    status = 'graded'::attempt_status,
    requires_manual_review = false,
    reviewed_by = p_reviewer_id,
    reviewed_at = NOW(),
    updated_at = NOW()
  WHERE id = p_attempt_id;

  -- 7. Update linked job_applications score
  IF v_attempt.application_id IS NOT NULL THEN
    BEGIN
      UPDATE public.job_applications SET
        score = v_percentage,
        updated_at = NOW()
      WHERE id = v_attempt.application_id;
    EXCEPTION WHEN OTHERS THEN
      -- non-fatal
    END;
  END IF;

  -- 8. Return updated attempt row
  SELECT to_jsonb(a) INTO v_result
  FROM public.quiz_attempts a
  WHERE a.id = p_attempt_id;

  RETURN v_result;
END;
$$;

GRANT EXECUTE ON FUNCTION public.submit_structured_review(UUID, UUID, JSONB) TO authenticated, service_role, anon;

-- 7. Update get_job_quiz_attempts to include review status and reviewer details
CREATE OR REPLACE FUNCTION public.get_job_quiz_attempts(p_job_id UUID)
RETURNS JSONB
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_job RECORD;
  v_results JSONB;
BEGIN
  -- Verify job exists
  SELECT * INTO v_job FROM public.jobs WHERE id = p_job_id;
  IF NOT FOUND THEN
    RETURN '[]'::JSONB;
  END IF;

  -- Verify caller has access (creator, org member/admin, or backend)
  IF auth.uid() IS NOT NULL AND v_job.created_by <> auth.uid()
     AND NOT public.is_org_member(v_job.organization_id, auth.uid())
     AND NOT public.is_org_admin(v_job.organization_id, auth.uid()) THEN
    RAISE EXCEPTION 'Access denied to view quiz attempts for this job.';
  END IF;

  SELECT coalesce(jsonb_agg(to_jsonb(t)), '[]'::JSONB) INTO v_results
  FROM (
    SELECT
      qa.id,
      qa.job_id,
      qa.selection_process_id,
      qa.application_id,
      qa.bank_id,
      qa.user_id,
      qa.status,
      qa.started_at,
      qa.submitted_at,
      qa.expires_at,
      sp.time_limit_minutes,
      qa.score,
      qa.max_score,
      qa.percentage,
      sp.passing_score,
      qa.passed,
      qa.requires_manual_review,
      qa.reviewed_by,
      qa.review_started_at,
      qa.reviewed_at,
      coalesce(sp.title, 'Quiz Assessment') AS stage_title,
      v_job.title AS job_title,
      coalesce(nullif(trim(concat(u.first_name, ' ', u.last_name)), ''), u.display_name, u.email) AS candidate_name,
      u.email AS candidate_email,
      coalesce(nullif(trim(concat(rev.first_name, ' ', rev.last_name)), ''), rev.display_name, rev.email) AS reviewer_name,
      rev.email AS reviewer_email
    FROM public.quiz_attempts qa
    LEFT JOIN public.job_selection_processes sp ON sp.id = qa.selection_process_id
    LEFT JOIN public.users u ON u.id = qa.user_id
    LEFT JOIN public.users rev ON rev.id = qa.reviewed_by
    WHERE qa.job_id = p_job_id
    ORDER BY qa.created_at DESC
  ) t;

  RETURN v_results;
END;
$$;

GRANT EXECUTE ON FUNCTION public.get_job_quiz_attempts(UUID) TO authenticated, service_role, anon;
