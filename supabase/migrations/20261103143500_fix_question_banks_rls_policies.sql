-- =====================================================
-- FIX: Question Banks and Questions RLS Policies
-- 
-- Fixes:
-- 1. Soft-delete (UPDATE with deleted_at = NOW()):
--    The SELECT policy previously required `deleted_at IS NULL`.
--    When PostgREST executes an UPDATE with `Prefer: return=representation`,
--    PostgreSQL evaluates the SELECT policy on the updated row.
--    Because `deleted_at IS NOT NULL` on the soft-deleted row, it failed
--    RLS with error 42501 "new row violates row-level security policy".
-- 2. Organization Creators:
--    Organization owners/creators registered via `organizations.created_by`
--    must be recognized via `public.is_org_member` and `public.is_org_admin`.
-- 3. Explicit `WITH CHECK` clauses for UPDATE policies.
-- 4. Allows both Question Bank Creator AND Org Admin to manage/delete banks.
-- =====================================================

-- Ensure helper functions exist
CREATE OR REPLACE FUNCTION public.is_org_admin(
  p_organization_id UUID,
  p_user_id         UUID
)
RETURNS BOOLEAN
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = public
AS $$
  SELECT EXISTS (
    SELECT 1
    FROM public.organization_members
    WHERE organization_id = p_organization_id
      AND user_id         = p_user_id
      AND role            = 'org_admin'
      AND status          = 'active'
  )
  OR EXISTS (
    SELECT 1
    FROM public.organizations
    WHERE id = p_organization_id
      AND created_by = p_user_id
  )
  OR (public.get_current_user_role() = 'super_admin');
$$;

CREATE OR REPLACE FUNCTION public.is_org_member(
  p_organization_id UUID,
  p_user_id         UUID
)
RETURNS BOOLEAN
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = public
AS $$
  SELECT EXISTS (
    SELECT 1
    FROM public.organization_members
    WHERE organization_id = p_organization_id
      AND user_id         = p_user_id
      AND status          = 'active'
  )
  OR EXISTS (
    SELECT 1
    FROM public.organizations
    WHERE id = p_organization_id
      AND created_by = p_user_id
  )
  OR (public.get_current_user_role() = 'super_admin');
$$;

-- =====================================================
-- QUESTION BANKS RLS POLICIES
-- =====================================================

-- 1. SELECT: Org members can view question banks of their organization
DROP POLICY IF EXISTS "Org members view question banks" ON public.question_banks;
CREATE POLICY "Org members view question banks"
  ON public.question_banks FOR SELECT
  USING (
    public.is_org_member(organization_id, auth.uid())
  );

-- 2. INSERT: Active org officials can create question banks
DROP POLICY IF EXISTS "Org officials create question banks" ON public.question_banks;
CREATE POLICY "Org officials create question banks"
  ON public.question_banks FOR INSERT
  WITH CHECK (
    created_by = auth.uid() AND public.is_org_member(organization_id, auth.uid())
  );

-- 3. UPDATE: Bank creator or Org Admin can update (including setting deleted_at for soft delete)
DROP POLICY IF EXISTS "Only creator can update question bank" ON public.question_banks;
CREATE POLICY "Only creator can update question bank"
  ON public.question_banks FOR UPDATE
  USING (
    (created_by = auth.uid() AND public.is_org_member(organization_id, auth.uid()))
    OR public.is_org_admin(organization_id, auth.uid())
  )
  WITH CHECK (
    (created_by = auth.uid() AND public.is_org_member(organization_id, auth.uid()))
    OR public.is_org_admin(organization_id, auth.uid())
  );

-- 4. DELETE: Hard delete fallback for bank creator or Org Admin
DROP POLICY IF EXISTS "Only creator can delete question bank" ON public.question_banks;
CREATE POLICY "Only creator can delete question bank"
  ON public.question_banks FOR DELETE
  USING (
    (created_by = auth.uid() AND public.is_org_member(organization_id, auth.uid()))
    OR public.is_org_admin(organization_id, auth.uid())
  );

-- =====================================================
-- QUESTIONS RLS POLICIES
-- =====================================================

-- 1. SELECT: Org members can view questions of their organization's banks
DROP POLICY IF EXISTS "Org members view questions" ON public.questions;
CREATE POLICY "Org members view questions"
  ON public.questions FOR SELECT
  USING (
    public.is_org_member(organization_id, auth.uid())
  );

-- 2. INSERT: Bank creator or Org Admin can insert questions into a bank
DROP POLICY IF EXISTS "Only bank creator can insert questions" ON public.questions;
CREATE POLICY "Only bank creator can insert questions"
  ON public.questions FOR INSERT
  WITH CHECK (
    created_by = auth.uid() AND (
      EXISTS (
        SELECT 1 FROM public.question_banks qb
        WHERE qb.id = bank_id
          AND (
            (qb.created_by = auth.uid() AND public.is_org_member(qb.organization_id, auth.uid()))
            OR public.is_org_admin(qb.organization_id, auth.uid())
          )
      )
    )
  );

-- 3. UPDATE: Bank creator or Org Admin can update questions (including soft delete)
DROP POLICY IF EXISTS "Only bank creator can update questions" ON public.questions;
CREATE POLICY "Only bank creator can update questions"
  ON public.questions FOR UPDATE
  USING (
    EXISTS (
      SELECT 1 FROM public.question_banks qb
      WHERE qb.id = questions.bank_id
        AND (
          (qb.created_by = auth.uid() AND public.is_org_member(qb.organization_id, auth.uid()))
          OR public.is_org_admin(qb.organization_id, auth.uid())
        )
    )
  )
  WITH CHECK (
    EXISTS (
      SELECT 1 FROM public.question_banks qb
      WHERE qb.id = questions.bank_id
        AND (
          (qb.created_by = auth.uid() AND public.is_org_member(qb.organization_id, auth.uid()))
          OR public.is_org_admin(qb.organization_id, auth.uid())
        )
    )
  );

-- 4. DELETE: Bank creator or Org Admin can delete questions
DROP POLICY IF EXISTS "Only bank creator can delete questions" ON public.questions;
CREATE POLICY "Only bank creator can delete questions"
  ON public.questions FOR DELETE
  USING (
    EXISTS (
      SELECT 1 FROM public.question_banks qb
      WHERE qb.id = questions.bank_id
        AND (
          (qb.created_by = auth.uid() AND public.is_org_member(qb.organization_id, auth.uid()))
          OR public.is_org_admin(qb.organization_id, auth.uid())
        )
    )
  );

