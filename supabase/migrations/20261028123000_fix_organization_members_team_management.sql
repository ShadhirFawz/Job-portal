-- =====================================================
-- FIX: Organization Members Team Management & Permissions
--
-- Issues addressed:
-- 1. Ensure is_org_admin recognizes organization creators and super_admins automatically.
-- 2. Allow active members, creators, org admins, and super admins to view all team members
--    (including pending invitations) in their organization.
-- 3. Ensure Org Admins can update roles, approve pending invitations, and revoke/remove members.
-- =====================================================

-- -------------------------------------------------------
-- 1. Update is_org_admin helper
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
  )
  OR EXISTS (
    SELECT 1
    FROM public.organizations
    WHERE id = p_organization_id
      AND created_by = p_user_id
  )
  OR (public.get_current_user_role() = 'super_admin');
$$;

-- -------------------------------------------------------
-- 2. Update is_org_member helper
-- -------------------------------------------------------
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

-- -------------------------------------------------------
-- 3. Update SELECT Policy for organization_members
-- -------------------------------------------------------
DROP POLICY IF EXISTS "Org admins can view all members of their org" ON public.organization_members;
DROP POLICY IF EXISTS "Org creators and admins can view all members of their org" ON public.organization_members;
DROP POLICY IF EXISTS "Members can view all members of their org" ON public.organization_members;
DROP POLICY IF EXISTS "Members and admins can view all members of their org" ON public.organization_members;

CREATE POLICY "Members and admins can view all members of their org"
  ON public.organization_members
  FOR SELECT
  TO authenticated
  USING (
    public.is_org_admin(organization_id, auth.uid())
    OR public.is_org_member(organization_id, auth.uid())
    OR user_id = auth.uid()
  );

-- -------------------------------------------------------
-- 4. Update UPDATE Policy for organization_members
-- -------------------------------------------------------
DROP POLICY IF EXISTS "Org creators and admins can update members" ON public.organization_members;

CREATE POLICY "Org creators and admins can update members"
  ON public.organization_members
  FOR UPDATE
  TO authenticated
  USING (
    public.is_org_admin(organization_id, auth.uid())
    OR user_id = auth.uid()
  )
  WITH CHECK (
    public.is_org_admin(organization_id, auth.uid())
    OR user_id = auth.uid()
  );

-- -------------------------------------------------------
-- 5. Update DELETE Policy for organization_members
-- -------------------------------------------------------
DROP POLICY IF EXISTS "Org creators and admins can delete members" ON public.organization_members;

CREATE POLICY "Org creators and admins can delete members"
  ON public.organization_members
  FOR DELETE
  TO authenticated
  USING (
    public.is_org_admin(organization_id, auth.uid())
  );

