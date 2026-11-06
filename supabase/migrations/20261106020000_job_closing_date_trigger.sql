-- ============================================================
-- Migration: Auto-close jobs when application_deadline passes
-- ============================================================
-- This migration creates:
--   1. A trigger function that fires on INSERT/UPDATE of the jobs
--      table and automatically sets status = 'closed',
--      closed_at = NOW(), accept_applications = false when
--      application_deadline is not null and has passed.
--   2. A trigger attached to the jobs table.
--   3. A helper function close_expired_jobs() to manually
--      bulk-close any already-expired jobs (run once after deploy).
-- ============================================================

-- -------------------------------------------------------
-- 1. Trigger function: check and auto-close on each row write
-- -------------------------------------------------------
CREATE OR REPLACE FUNCTION public.check_job_application_deadline()
RETURNS TRIGGER
LANGUAGE plpgsql
SECURITY DEFINER
AS $$
BEGIN
  -- Only close if the deadline is set and has passed, AND the job is
  -- currently in an "open" state (not already closed/archived).
  IF NEW.application_deadline IS NOT NULL
     AND NEW.application_deadline <= NOW()
     AND NEW.status IN ('published', 'paused')
  THEN
    NEW.status            := 'closed';
    NEW.closed_at         := NOW();
    NEW.accept_applications := FALSE;
  END IF;

  RETURN NEW;
END;
$$;

-- -------------------------------------------------------
-- 2. Attach trigger to public.jobs (BEFORE INSERT OR UPDATE)
-- -------------------------------------------------------
DROP TRIGGER IF EXISTS trg_check_job_application_deadline ON public.jobs;

CREATE TRIGGER trg_check_job_application_deadline
  BEFORE INSERT OR UPDATE ON public.jobs
  FOR EACH ROW
  EXECUTE FUNCTION public.check_job_application_deadline();

-- -------------------------------------------------------
-- 3. Helper: bulk-close all currently expired published jobs
--    Run this once after migration to catch existing rows.
-- -------------------------------------------------------
CREATE OR REPLACE FUNCTION public.close_expired_jobs()
RETURNS INTEGER
LANGUAGE plpgsql
SECURITY DEFINER
AS $$
DECLARE
  v_count INTEGER;
BEGIN
  UPDATE public.jobs
  SET
    status              = 'closed',
    closed_at           = NOW(),
    accept_applications = FALSE,
    updated_at          = NOW()
  WHERE
    application_deadline IS NOT NULL
    AND application_deadline <= NOW()
    AND status IN ('published', 'paused')
    AND deleted_at IS NULL;

  GET DIAGNOSTICS v_count = ROW_COUNT;
  RETURN v_count;
END;
$$;

-- Close any currently expired jobs immediately after migration
SELECT public.close_expired_jobs();

