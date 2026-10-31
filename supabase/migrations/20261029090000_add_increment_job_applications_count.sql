-- =====================================================
-- Increment Job Applications Count RPC
-- =====================================================
CREATE OR REPLACE FUNCTION increment_job_applications_count(job_id UUID)
RETURNS void
LANGUAGE plpgsql
SECURITY DEFINER
AS $$
BEGIN
  UPDATE public.jobs
  SET applications_count = applications_count + 1
  WHERE id = job_id;
END;
$$;

