-- ====================================================================
-- Allow Organization Members and Super Admins to Read Applicant Resumes
-- ====================================================================

DROP POLICY IF EXISTS "Org members can read applicant resumes" ON storage.objects;
DROP POLICY IF EXISTS "Super admins can read all resumes" ON storage.objects;

-- 1. Org members (admins and staff) can read applicant resumes attached to their jobs
CREATE POLICY "Org members can read applicant resumes"
ON storage.objects FOR SELECT TO authenticated
USING (
  bucket_id = 'resumes' AND
  EXISTS (
    SELECT 1 FROM public.resumes r
    JOIN public.job_applications ja ON ja.resume_id = r.id
    JOIN public.jobs j ON j.id = ja.job_id
    JOIN public.organization_members om ON om.organization_id = j.organization_id
    WHERE ltrim(r.file_path, '/') = ltrim(storage.objects.name, '/')
      AND om.user_id = auth.uid()
      AND om.status = 'active'
  )
);

-- 2. Super admins can read all resumes
CREATE POLICY "Super admins can read all resumes"
ON storage.objects FOR SELECT TO authenticated
USING (
  bucket_id = 'resumes' AND
  EXISTS (
    SELECT 1 FROM public.users u
    WHERE u.id = auth.uid()
      AND u.role = 'super_admin'
  )
);

