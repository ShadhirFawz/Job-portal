-- =====================================================
-- FIX: Users RLS & Helpers for Member Invitations
--
-- Issues addressed:
-- 1. public.users had a restrictive SELECT policy ("Users can view own profile" USING (auth.uid() = id)),
--    which prevented organization admins/creators from finding existing registered users by email to invite them.
--    It also prevented organization members from viewing user details (name, email, avatar) of other team members
--    in the organization_members list.
-- 2. Added a safe SELECT policy allowing authenticated users to view profiles of non-deleted users.
-- 3. Added a SECURITY DEFINER helper function `find_user_by_email` to allow secure, case-insensitive
--    user lookup for invitations without relying solely on RLS table queries.
-- =====================================================

-- -------------------------------------------------------
-- 1. Update SELECT policy on public.users
-- -------------------------------------------------------
DROP POLICY IF EXISTS "Users can view own profile" ON public.users;
DROP POLICY IF EXISTS "Authenticated users can view profiles" ON public.users;
DROP POLICY IF EXISTS "Users can view profiles" ON public.users;

-- Allow authenticated users to view profiles (their own profile, or any non-deleted user)
CREATE POLICY "Authenticated users can view profiles"
  ON public.users
  FOR SELECT
  TO authenticated
  USING (
    auth.uid() = id
    OR deleted_at IS NULL
  );

-- -------------------------------------------------------
-- 2. Helper function: find_user_by_email
-- -------------------------------------------------------
CREATE OR REPLACE FUNCTION public.find_user_by_email(search_email TEXT)
RETURNS TABLE (
  id UUID,
  email TEXT,
  first_name TEXT,
  last_name TEXT,
  display_name TEXT,
  avatar_path TEXT
)
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = public
AS $$
  SELECT 
    u.id, 
    u.email, 
    u.first_name, 
    u.last_name, 
    u.display_name, 
    u.avatar_path
  FROM public.users u
  WHERE LOWER(u.email) = LOWER(TRIM(search_email))
    AND u.deleted_at IS NULL
  LIMIT 1;
$$;

GRANT EXECUTE ON FUNCTION public.find_user_by_email(TEXT) TO authenticated;

