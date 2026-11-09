-- =====================================================
-- Migration: Fix Quiz Attempts and Answers Visibility for Org Members
-- =====================================================

-- 1. Update SELECT policy on quiz_attempts
DROP POLICY IF EXISTS "Org members view job attempts" ON public.quiz_attempts;
CREATE POLICY "Org members view job attempts"
  ON public.quiz_attempts FOR SELECT
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
  );

-- 2. Update SELECT policy on quiz_answers
DROP POLICY IF EXISTS "Org members view job answers" ON public.quiz_answers;
CREATE POLICY "Org members view job answers"
  ON public.quiz_answers FOR SELECT
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
  );

-- 3. Stored procedure: get_job_quiz_attempts (Security Definer)
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
      coalesce(sp.title, 'Quiz Assessment') AS stage_title,
      v_job.title AS job_title,
      coalesce(nullif(trim(concat(u.first_name, ' ', u.last_name)), ''), u.display_name, u.email) AS candidate_name,
      u.email AS candidate_email
    FROM public.quiz_attempts qa
    LEFT JOIN public.job_selection_processes sp ON sp.id = qa.selection_process_id
    LEFT JOIN public.users u ON u.id = qa.user_id
    WHERE qa.job_id = p_job_id
    ORDER BY qa.created_at DESC
  ) t;

  RETURN v_results;
END;
$$;

GRANT EXECUTE ON FUNCTION public.get_job_quiz_attempts(UUID) TO authenticated, service_role, anon;

