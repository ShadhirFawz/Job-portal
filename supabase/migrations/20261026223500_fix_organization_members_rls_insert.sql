-- =====================================================
-- FIX: Organization Members & Organizations RLS Policies
--
-- Issues addressed:
-- 1. organization_members had NO INSERT/UPDATE/DELETE policies,
--    causing "new row violates row-level security policy for
--    table organization_members" when creating an organization
--    and adding the creator as org_admin.
-- 2. organizations UPDATE policy only checked is_org_admin,
--    which blocked the creator from updating image paths/slug
--    before the org_member record was created.
-- 3. Automatic slug generation trigger on organizations table
--    if slug is omitted.
-- =====================================================

-- -------------------------------------------------------
-- 1. SECURITY DEFINER helpers to avoid recursion
-- -------------------------------------------------------
CREATE OR REPLACE FUNCTION public.is_org_creator(
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
    FROM public.organizations
    WHERE id = p_organization_id
      AND created_by = p_user_id
  );
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
  );
$$;

-- -------------------------------------------------------
-- 2. Organizations Policies
-- -------------------------------------------------------
-- Allow members to view their org even if status is pending_verification
DROP POLICY IF EXISTS "Members can view their own organizations" ON public.organizations;
CREATE POLICY "Members can view their own organizations"
  ON public.organizations
  FOR SELECT
  TO authenticated
  USING (public.is_org_member(id, auth.uid()));

-- Allow creator OR org admin to update organization
DROP POLICY IF EXISTS "Org admins can update organization" ON public.organizations;
DROP POLICY IF EXISTS "Org creators and admins can update organization" ON public.organizations;
CREATE POLICY "Org creators and admins can update organization"
  ON public.organizations
  FOR UPDATE
  TO authenticated
  USING (
    created_by = auth.uid() OR public.is_org_admin(id, auth.uid())
  )
  WITH CHECK (
    created_by = auth.uid() OR public.is_org_admin(id, auth.uid())
  );

-- -------------------------------------------------------
-- 3. Organization Members Policies
-- -------------------------------------------------------

-- SELECT: Creators and admins can view all members in the org
DROP POLICY IF EXISTS "Org admins can view all members of their org" ON public.organization_members;
DROP POLICY IF EXISTS "Org creators and admins can view all members of their org" ON public.organization_members;
CREATE POLICY "Org creators and admins can view all members of their org"
  ON public.organization_members
  FOR SELECT
  TO authenticated
  USING (
    public.is_org_creator(organization_id, auth.uid())
    OR public.is_org_admin(organization_id, auth.uid())
  );

-- INSERT: Creators (when creating org) and Org Admins can add members
DROP POLICY IF EXISTS "Org creators and admins can insert members" ON public.organization_members;
CREATE POLICY "Org creators and admins can insert members"
  ON public.organization_members
  FOR INSERT
  TO authenticated
  WITH CHECK (
    public.is_org_creator(organization_id, auth.uid())
    OR public.is_org_admin(organization_id, auth.uid())
  );

-- UPDATE: Creators, Org Admins, or member updating self
DROP POLICY IF EXISTS "Org creators and admins can update members" ON public.organization_members;
CREATE POLICY "Org creators and admins can update members"
  ON public.organization_members
  FOR UPDATE
  TO authenticated
  USING (
    public.is_org_creator(organization_id, auth.uid())
    OR public.is_org_admin(organization_id, auth.uid())
    OR user_id = auth.uid()
  )
  WITH CHECK (
    public.is_org_creator(organization_id, auth.uid())
    OR public.is_org_admin(organization_id, auth.uid())
    OR user_id = auth.uid()
  );

-- DELETE: Creators and Org Admins can remove members
DROP POLICY IF EXISTS "Org creators and admins can delete members" ON public.organization_members;
CREATE POLICY "Org creators and admins can delete members"
  ON public.organization_members
  FOR DELETE
  TO authenticated
  USING (
    public.is_org_creator(organization_id, auth.uid())
    OR public.is_org_admin(organization_id, auth.uid())
  );

-- -------------------------------------------------------
-- 4. Auto-generate slug on insert if missing
-- -------------------------------------------------------
CREATE OR REPLACE FUNCTION public.set_organization_slug()
RETURNS TRIGGER AS $$
BEGIN
  IF NEW.slug IS NULL OR NEW.slug = '' THEN
    NEW.slug := public.generate_organization_slug(NEW.name);
  END IF;
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trigger_set_organization_slug ON public.organizations;
CREATE TRIGGER trigger_set_organization_slug
  BEFORE INSERT ON public.organizations
  FOR EACH ROW
  EXECUTE FUNCTION public.set_organization_slug();
