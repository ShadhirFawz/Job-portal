-- =====================================================
-- Migration: Add job enhancements
-- Adds fields for:
--   - 'Nice to have' (nice_to_have)
--   - 'What we offer' (what_we_offer)
--   - 'Tech Stacks you'll work with' (tech_stacks)
--   - Custom hiring process toggle (has_custom_hiring_process)
--   - 'How to apply' instructions (how_to_apply)
--   - Selection process visibility toggle (is_selection_process_public)
-- =====================================================

ALTER TABLE public.jobs
  ADD COLUMN IF NOT EXISTS nice_to_have TEXT,
  ADD COLUMN IF NOT EXISTS what_we_offer TEXT,
  ADD COLUMN IF NOT EXISTS tech_stacks TEXT[] DEFAULT '{}'::TEXT[],
  ADD COLUMN IF NOT EXISTS has_custom_hiring_process BOOLEAN NOT NULL DEFAULT false,
  ADD COLUMN IF NOT EXISTS how_to_apply TEXT,
  ADD COLUMN IF NOT EXISTS is_selection_process_public BOOLEAN NOT NULL DEFAULT true;

-- Update RLS policy on job_selection_processes to respect is_selection_process_public
DROP POLICY IF EXISTS "Selection processes visible with job" ON public.job_selection_processes;

CREATE POLICY "Selection processes visible with job"
  ON public.job_selection_processes FOR SELECT
  USING (
    EXISTS (
      SELECT 1 FROM public.jobs j
      WHERE j.id = job_id
        AND (
          -- If public: visible for published jobs
          (j.is_selection_process_public = true AND j.status = 'published' AND j.deleted_at IS NULL)
          -- OR user is org_admin/org_staff of the organization
          OR EXISTS (
            SELECT 1 FROM public.organization_members om
            WHERE om.organization_id = j.organization_id
              AND om.user_id = auth.uid()
              AND om.status = 'active'
              AND om.role IN ('org_admin', 'org_staff')
          )
        )
    )
  );

