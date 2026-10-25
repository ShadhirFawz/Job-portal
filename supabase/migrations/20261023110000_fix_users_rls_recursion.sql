-- =====================================================
-- Fix: infinite recursion in "Super admins full access" policy
-- The original policy queried public.users inside a USING clause,
-- causing PostgreSQL to re-evaluate RLS on the same table recursively.
-- =====================================================

-- Step 1: Drop the broken recursive policy
DROP POLICY IF EXISTS "Super admins full access" ON public.users;

-- Step 2: Create a SECURITY DEFINER helper function that reads the role
-- directly from public.users bypassing RLS (runs with owner permissions)
CREATE OR REPLACE FUNCTION public.get_current_user_role()
RETURNS user_role
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = public
AS $$
  SELECT role FROM public.users WHERE id = auth.uid();
$$;

-- Step 3: Re-create the super admin policy using the helper function
-- The function runs with SECURITY DEFINER so it bypasses RLS,
-- breaking the recursion entirely.
CREATE POLICY "Super admins full access"
  ON public.users
  FOR ALL
  USING (
    public.get_current_user_role() = 'super_admin'
  );

-- Step 4: Also ensure candidates can INSERT their own row
-- (needed for upsert from the frontend)
DROP POLICY IF EXISTS "Users can insert own profile" ON public.users;
CREATE POLICY "Users can insert own profile"
  ON public.users
  FOR INSERT
  WITH CHECK (auth.uid() = id);