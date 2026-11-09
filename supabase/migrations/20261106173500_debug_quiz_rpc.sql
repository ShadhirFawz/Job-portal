-- Debug quiz rpc
CREATE OR REPLACE FUNCTION public.get_debug_quiz_info()
RETURNS jsonb
LANGUAGE plpgsql
SECURITY DEFINER
AS $$
DECLARE
  result jsonb;
BEGIN
  SELECT jsonb_build_object(
    'attempts', (SELECT coalesce(jsonb_agg(to_jsonb(a)), '[]'::jsonb) FROM (SELECT * FROM public.quiz_attempts ORDER BY created_at DESC LIMIT 5) a),
    'answers', (SELECT coalesce(jsonb_agg(to_jsonb(ans)), '[]'::jsonb) FROM (SELECT * FROM public.quiz_answers ORDER BY answered_at DESC LIMIT 5) ans),
    'processes', (SELECT coalesce(jsonb_agg(to_jsonb(p)), '[]'::jsonb) FROM (SELECT id, job_id, title, question_bank_id, config FROM public.job_selection_processes ORDER BY created_at DESC LIMIT 5) p)
  ) INTO result;
  RETURN result;
END;
$$;

