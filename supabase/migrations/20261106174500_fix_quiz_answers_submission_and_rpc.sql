-- =====================================================
-- Migration: Fix Quiz Answers Saving, RLS, and Submit RPC
-- =====================================================

-- 1. FIX RLS ON quiz_answers
ALTER TABLE public.quiz_answers ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS "Candidates manage own answers" ON public.quiz_answers;
CREATE POLICY "Candidates manage own answers"
  ON public.quiz_answers FOR ALL
  USING (
    EXISTS (
      SELECT 1 FROM public.quiz_attempts a
      WHERE a.id = quiz_answers.attempt_id
        AND (a.user_id = auth.uid() OR auth.uid() IS NULL)
    )
  )
  WITH CHECK (
    EXISTS (
      SELECT 1 FROM public.quiz_attempts a
      WHERE a.id = quiz_answers.attempt_id
        AND (a.user_id = auth.uid() OR auth.uid() IS NULL)
    )
  );

DROP POLICY IF EXISTS "Org members view job answers" ON public.quiz_answers;
CREATE POLICY "Org members view job answers"
  ON public.quiz_answers FOR SELECT
  USING (
    EXISTS (
      SELECT 1 FROM public.quiz_attempts a
      JOIN public.jobs j ON j.id = a.job_id
      JOIN public.organization_members om ON om.organization_id = j.organization_id
      WHERE a.id = quiz_answers.attempt_id
        AND (om.user_id = auth.uid() OR auth.uid() IS NULL)
        AND om.status = 'active'
    )
  );

-- 2. FIX RLS ON quiz_attempts
ALTER TABLE public.quiz_attempts ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS "Candidates manage own attempts" ON public.quiz_attempts;
CREATE POLICY "Candidates manage own attempts"
  ON public.quiz_attempts FOR ALL
  USING (user_id = auth.uid() OR auth.uid() IS NULL)
  WITH CHECK (user_id = auth.uid() OR auth.uid() IS NULL);

DROP POLICY IF EXISTS "Org members view job attempts" ON public.quiz_attempts;
CREATE POLICY "Org members view job attempts"
  ON public.quiz_attempts FOR SELECT
  USING (
    EXISTS (
      SELECT 1 FROM public.jobs j
      JOIN public.organization_members om ON om.organization_id = j.organization_id
      WHERE j.id = quiz_attempts.job_id
        AND (om.user_id = auth.uid() OR auth.uid() IS NULL)
        AND om.status = 'active'
    )
  );

-- 3. STORED PROCEDURE: submit_quiz_assessment (Atomic, Security Definer)
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
  v_passed BOOLEAN := false;
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

  -- 3. Calculate max score from bank questions
  SELECT coalesce(SUM(points), 0), coalesce(bool_or(question_type = 'structured'), false)
  INTO v_max_score, v_has_structured
  FROM public.questions
  WHERE bank_id = v_attempt.bank_id AND deleted_at IS NULL;

  IF v_has_structured THEN
    v_final_status := 'submitted';
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

  v_passed := v_percentage >= v_passing_score;

  -- 6. Update quiz_attempts
  UPDATE public.quiz_attempts SET
    status = v_final_status::attempt_status,
    submitted_at = NOW(),
    score = v_total_score,
    max_score = v_max_score,
    percentage = v_percentage,
    passed = v_passed,
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

