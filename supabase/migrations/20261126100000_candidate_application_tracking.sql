-- =====================================================
-- Migration: Candidate Application & Quiz Tracking RPCs
-- =====================================================

-- 1. Ensure candidates can view and withdraw their own applications via RLS
DROP POLICY IF EXISTS "Candidates view own applications" ON public.job_applications;
CREATE POLICY "Candidates view own applications"
  ON public.job_applications FOR SELECT
  USING (user_id = auth.uid() OR auth.uid() IS NULL);

DROP POLICY IF EXISTS "Candidates withdraw own applications" ON public.job_applications;
CREATE POLICY "Candidates withdraw own applications"
  ON public.job_applications FOR UPDATE
  USING (user_id = auth.uid() OR auth.uid() IS NULL)
  WITH CHECK (user_id = auth.uid() OR auth.uid() IS NULL);

-- 2. RPC: get_candidate_applications(p_user_id UUID)
CREATE OR REPLACE FUNCTION public.get_candidate_applications(p_user_id UUID)
RETURNS JSONB
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_results JSONB;
BEGIN
  IF auth.uid() IS NOT NULL AND auth.uid() <> p_user_id THEN
    RAISE EXCEPTION 'Access denied: you can only view your own applications.';
  END IF;

  SELECT coalesce(jsonb_agg(row_data ORDER BY applied_at DESC), '[]'::JSONB)
  INTO v_results
  FROM (
    SELECT
      ja.applied_at,
      jsonb_build_object(
        'id', ja.id,
        'slug', ja.slug,
        'job_id', ja.job_id,
        'user_id', ja.user_id,
        'status', ja.status,
        'cover_letter', ja.cover_letter,
        'resume_id', ja.resume_id,
        'custom_answers', ja.custom_answers,
        'current_process_id', ja.current_process_id,
        'score', ja.score,
        'applied_at', ja.applied_at,
        'updated_at', ja.updated_at,
        'withdrawn_at', ja.withdrawn_at,
        'job', jsonb_build_object(
          'id', j.id,
          'title', j.title,
          'slug', j.slug,
          'job_type', j.job_type,
          'work_mode', j.work_mode,
          'city', j.city,
          'country_code', j.country_code,
          'status', j.status,
          'application_deadline', j.application_deadline,
          'organization', CASE WHEN o.id IS NOT NULL THEN jsonb_build_object(
            'id', o.id,
            'name', o.name,
            'slug', o.slug,
            'avatar_path', o.avatar_path
          ) ELSE NULL END
        ),
        'resume', CASE WHEN r.id IS NOT NULL THEN jsonb_build_object(
          'id', r.id,
          'title', r.title,
          'file_name', r.file_name,
          'file_path', r.file_path,
          'file_size', r.file_size,
          'file_type', r.file_type
        ) ELSE NULL END,
        'selection_stages', coalesce((
          SELECT jsonb_agg(
            jsonb_build_object(
              'id', sp.id,
              'title', sp.title,
              'slug', sp.slug,
              'process_type', sp.process_type,
              'order_index', sp.order_index,
              'is_mandatory', sp.is_mandatory,
              'time_limit_minutes', sp.time_limit_minutes,
              'passing_score', sp.passing_score,
              'question_bank_id', sp.question_bank_id
            ) ORDER BY sp.order_index ASC
          )
          FROM public.job_selection_processes sp
          WHERE sp.job_id = ja.job_id
        ), '[]'::JSONB),
        'quiz_attempts', coalesce((
          SELECT jsonb_agg(
            jsonb_build_object(
              'id', qa.id,
              'selection_process_id', qa.selection_process_id,
              'application_id', qa.application_id,
              'status', qa.status,
              'score', qa.score,
              'max_score', qa.max_score,
              'percentage', qa.percentage,
              'passed', qa.passed,
              'requires_manual_review', qa.requires_manual_review,
              'started_at', qa.started_at,
              'submitted_at', qa.submitted_at,
              'reviewed_at', qa.reviewed_at,
              'passing_score', sp_qa.passing_score,
              'time_limit_minutes', sp_qa.time_limit_minutes,
              'stage_title', coalesce(sp_qa.title, 'Quiz Assessment')
            ) ORDER BY qa.created_at DESC
          )
          FROM public.quiz_attempts qa
          LEFT JOIN public.job_selection_processes sp_qa ON sp_qa.id = qa.selection_process_id
          WHERE qa.application_id = ja.id
             OR (qa.job_id = ja.job_id AND qa.user_id = p_user_id)
        ), '[]'::JSONB)
      ) AS row_data
    FROM public.job_applications ja
    JOIN public.jobs j ON j.id = ja.job_id
    LEFT JOIN public.organizations o ON o.id = j.organization_id
    LEFT JOIN public.resumes r ON r.id = ja.resume_id
    WHERE ja.user_id = p_user_id
  ) sub;

  RETURN coalesce(v_results, '[]'::JSONB);
END;
$$;

GRANT EXECUTE ON FUNCTION public.get_candidate_applications(UUID) TO authenticated, service_role, anon;

-- 3. RPC: withdraw_candidate_application(p_application_id UUID, p_user_id UUID)
CREATE OR REPLACE FUNCTION public.withdraw_candidate_application(
  p_application_id UUID,
  p_user_id UUID
)
RETURNS JSONB
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_app RECORD;
  v_result JSONB;
BEGIN
  IF auth.uid() IS NOT NULL AND auth.uid() <> p_user_id THEN
    RAISE EXCEPTION 'Access denied: you can only withdraw your own application.';
  END IF;

  SELECT * INTO v_app FROM public.job_applications WHERE id = p_application_id;
  IF NOT FOUND THEN
    RAISE EXCEPTION 'Application not found.';
  END IF;

  IF v_app.user_id <> p_user_id THEN
    RAISE EXCEPTION 'You are not authorized to withdraw this application.';
  END IF;

  IF v_app.status IN ('hired', 'withdrawn') THEN
    RAISE EXCEPTION 'Cannot withdraw an application that is already %.', v_app.status;
  END IF;

  UPDATE public.job_applications
  SET status = 'withdrawn'::application_status,
      withdrawn_at = NOW(),
      updated_at = NOW()
  WHERE id = p_application_id;

  SELECT to_jsonb(a) INTO v_result
  FROM public.job_applications a
  WHERE a.id = p_application_id;

  RETURN v_result;
END;
$$;

GRANT EXECUTE ON FUNCTION public.withdraw_candidate_application(UUID, UUID) TO authenticated, service_role, anon;
