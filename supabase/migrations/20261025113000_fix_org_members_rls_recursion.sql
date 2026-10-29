-- =====================================================
-- FIX: Infinite recursion in organization_members RLS
--
-- The policy "Org admins can view all members of their org"
-- (and "Org admins can update organization") used an inline
-- EXISTS subquery that reads organization_members from inside
-- a policy ON organization_members -- causing infinite recursion.
--
-- Solution: a SECURITY DEFINER function that runs as the
-- function owner (postgres), bypassing RLS entirely, so we
-- never re-enter the policy engine while evaluating a policy.
-- =====================================================

-- -------------------------------------------------------
-- 1. Restore the SECURITY DEFINER helper
-- -------------------------------------------------------
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
  );
$$;

-- -------------------------------------------------------
-- 2. Fix the organizations UPDATE policy
--    (inline EXISTS is fine here since organizations table
--     is not the table being protected by this policy,
--     but use the helper for consistency & clarity)
-- -------------------------------------------------------
DROP POLICY IF EXISTS "Org admins can update organization" ON public.organizations;

CREATE POLICY "Org admins can update organization"
  ON public.organizations
  FOR UPDATE
  USING (public.is_org_admin(id, auth.uid()));

-- -------------------------------------------------------
-- 3. Fix the organization_members SELECT policy that
--    caused the actual recursion.
--    (The policy queried organization_members from INSIDE
--     a policy ON organization_members.)
-- -------------------------------------------------------
DROP POLICY IF EXISTS "Org admins can view all members of their org" ON public.organization_members;

CREATE POLICY "Org admins can view all members of their org"
  ON public.organization_members
  FOR SELECT
  USING (public.is_org_admin(organization_id, auth.uid()));
