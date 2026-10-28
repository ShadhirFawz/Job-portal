-- =====================================================
-- Storage Buckets: Profile Images
-- =====================================================
-- Two separate public buckets for clean organization:
--
--   profile-images/
--     users/{userId}/avatar-{timestamp}.{ext}
--     users/{userId}/cover-{timestamp}.{ext}
--
--   organization-images/
--     organizations/{orgId}/avatar-{timestamp}.{ext}
--     organizations/{orgId}/cover-{timestamp}.{ext}
--
-- Why two buckets instead of one?
--   - Independent public/private settings per entity type
--   - Cleaner RLS policies scoped to each bucket
--   - Easier future migration (e.g. making org images private)
--   - Avoids path collisions between user and org assets
-- =====================================================

-- -------------------------------------------------------
-- 1. User Profile Images Bucket
-- -------------------------------------------------------
INSERT INTO storage.buckets (id, name, public, file_size_limit, allowed_mime_types)
VALUES (
  'profile-images',
  'profile-images',
  true,
  5242880,    -- 5 MB
  ARRAY['image/jpeg', 'image/png', 'image/webp', 'image/gif']
)
ON CONFLICT (id) DO UPDATE SET
  public             = EXCLUDED.public,
  file_size_limit    = EXCLUDED.file_size_limit,
  allowed_mime_types = EXCLUDED.allowed_mime_types;

-- -------------------------------------------------------
-- 2. Organization Images Bucket
-- -------------------------------------------------------
INSERT INTO storage.buckets (id, name, public, file_size_limit, allowed_mime_types)
VALUES (
  'organization-images',
  'organization-images',
  true,
  5242880,    -- 5 MB
  ARRAY['image/jpeg', 'image/png', 'image/webp', 'image/gif']
)
ON CONFLICT (id) DO UPDATE SET
  public             = EXCLUDED.public,
  file_size_limit    = EXCLUDED.file_size_limit,
  allowed_mime_types = EXCLUDED.allowed_mime_types;

-- -------------------------------------------------------
-- 3. RLS Policies: profile-images bucket
-- -------------------------------------------------------

-- Public read for everyone (bucket is public, but explicit policy for clarity)
DROP POLICY IF EXISTS "Profile images are publicly readable" ON storage.objects;
CREATE POLICY "Profile images are publicly readable"
  ON storage.objects
  FOR SELECT
  USING (bucket_id = 'profile-images');

-- Authenticated users can upload to their own folder
-- Path pattern:  users/{userId}/avatar-*.{ext}  or  users/{userId}/cover-*.{ext}
DROP POLICY IF EXISTS "Users can upload their own profile images" ON storage.objects;
CREATE POLICY "Users can upload their own profile images"
  ON storage.objects
  FOR INSERT
  TO authenticated
  WITH CHECK (
    bucket_id = 'profile-images'
    AND (storage.foldername(name))[1] = 'users'
    AND (storage.foldername(name))[2] = auth.uid()::text
  );

-- Users can overwrite / delete their own images
DROP POLICY IF EXISTS "Users can update their own profile images" ON storage.objects;
CREATE POLICY "Users can update their own profile images"
  ON storage.objects
  FOR UPDATE
  TO authenticated
  USING (
    bucket_id = 'profile-images'
    AND (storage.foldername(name))[1] = 'users'
    AND (storage.foldername(name))[2] = auth.uid()::text
  );

DROP POLICY IF EXISTS "Users can delete their own profile images" ON storage.objects;
CREATE POLICY "Users can delete their own profile images"
  ON storage.objects
  FOR DELETE
  TO authenticated
  USING (
    bucket_id = 'profile-images'
    AND (storage.foldername(name))[1] = 'users'
    AND (storage.foldername(name))[2] = auth.uid()::text
  );

-- -------------------------------------------------------
-- 4. RLS Policies: organization-images bucket
-- -------------------------------------------------------

-- Public read
DROP POLICY IF EXISTS "Organization images are publicly readable" ON storage.objects;
CREATE POLICY "Organization images are publicly readable"
  ON storage.objects
  FOR SELECT
  USING (bucket_id = 'organization-images');

-- Org admins and creators can upload to their organization's folder
-- Path pattern:  organizations/{orgId}/avatar-*.{ext}  or  organizations/{orgId}/cover-*.{ext}
DROP POLICY IF EXISTS "Org admins can upload organization images" ON storage.objects;
CREATE POLICY "Org admins can upload organization images"
  ON storage.objects
  FOR INSERT
  TO authenticated
  WITH CHECK (
    bucket_id = 'organization-images'
    AND (storage.foldername(name))[1] = 'organizations'
    AND (
      public.is_org_admin(
        ((storage.foldername(name))[2])::uuid,
        auth.uid()
      )
      OR EXISTS (
        SELECT 1 FROM public.organizations
        WHERE id = ((storage.foldername(name))[2])::uuid
          AND created_by = auth.uid()
      )
    )
  );

DROP POLICY IF EXISTS "Org admins can update organization images" ON storage.objects;
CREATE POLICY "Org admins can update organization images"
  ON storage.objects
  FOR UPDATE
  TO authenticated
  USING (
    bucket_id = 'organization-images'
    AND (storage.foldername(name))[1] = 'organizations'
    AND (
      public.is_org_admin(
        ((storage.foldername(name))[2])::uuid,
        auth.uid()
      )
      OR EXISTS (
        SELECT 1 FROM public.organizations
        WHERE id = ((storage.foldername(name))[2])::uuid
          AND created_by = auth.uid()
      )
    )
  );

DROP POLICY IF EXISTS "Org admins can delete organization images" ON storage.objects;
CREATE POLICY "Org admins can delete organization images"
  ON storage.objects
  FOR DELETE
  TO authenticated
  USING (
    bucket_id = 'organization-images'
    AND (storage.foldername(name))[1] = 'organizations'
    AND (
      public.is_org_admin(
        ((storage.foldername(name))[2])::uuid,
        auth.uid()
      )
      OR EXISTS (
        SELECT 1 FROM public.organizations
        WHERE id = ((storage.foldername(name))[2])::uuid
          AND created_by = auth.uid()
      )
    )
  );

