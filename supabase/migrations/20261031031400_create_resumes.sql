-- =====================================================
-- RESUMES
-- =====================================================
CREATE TABLE public.resumes (
  id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id             UUID NOT NULL REFERENCES public.users(id) ON DELETE CASCADE,
  
  -- Basic
  title               TEXT NOT NULL,                     -- e.g. "Software Engineer Resume 2025"
  is_primary          BOOLEAN NOT NULL DEFAULT false,
  
  -- File
  file_path           TEXT,                              -- storage path (PDF/DOCX)
  file_name           TEXT,
  file_size           INTEGER,                           -- bytes
  file_type           TEXT,                              -- application/pdf, etc.
  
  -- Parsed / Structured content (for autofill later)
  summary             TEXT,
  skills              TEXT[],                            -- array of skills
  experience          JSONB DEFAULT '[]'::jsonb,         -- [{company, title, start, end, description}]
  education           JSONB DEFAULT '[]'::jsonb,         -- [{institution, degree, field, year}]
  achievements        TEXT[],
  languages           TEXT[],
  certifications      JSONB DEFAULT '[]'::jsonb,
  
  -- Meta
  is_public           BOOLEAN NOT NULL DEFAULT false,    -- for future public profile
  last_used_at        TIMESTAMPTZ,
  
  created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  updated_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  deleted_at          TIMESTAMPTZ
);

CREATE INDEX idx_resumes_user ON public.resumes(user_id);
CREATE INDEX idx_resumes_primary ON public.resumes(user_id, is_primary) WHERE is_primary = true;

CREATE TRIGGER resumes_updated_at
  BEFORE UPDATE ON public.resumes
  FOR EACH ROW
  EXECUTE FUNCTION update_updated_at_column();

-- Ensure only one primary resume per user
CREATE OR REPLACE FUNCTION ensure_single_primary_resume()
RETURNS TRIGGER AS $$
BEGIN
  IF NEW.is_primary = true THEN
    UPDATE public.resumes
    SET is_primary = false
    WHERE user_id = NEW.user_id
      AND id != NEW.id
      AND is_primary = true;
  END IF;
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trigger_single_primary_resume
  BEFORE INSERT OR UPDATE OF is_primary ON public.resumes
  FOR EACH ROW
  WHEN (NEW.is_primary = true)
  EXECUTE FUNCTION ensure_single_primary_resume();

-- Limit to 5 resumes per user
CREATE OR REPLACE FUNCTION check_max_resumes()
RETURNS TRIGGER AS $$
BEGIN
  IF (SELECT COUNT(*) FROM public.resumes WHERE user_id = NEW.user_id AND deleted_at IS NULL) >= 5 THEN
    RAISE EXCEPTION 'You can have a maximum of 5 resumes';
  END IF;
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER enforce_max_resumes
  BEFORE INSERT ON public.resumes
  FOR EACH ROW
  EXECUTE FUNCTION check_max_resumes();

-- =====================================================
-- Link resume to applications (optional but useful)
-- =====================================================
ALTER TABLE public.job_applications
  ADD COLUMN IF NOT EXISTS resume_id UUID REFERENCES public.resumes(id) ON DELETE SET NULL;

-- =====================================================
-- RLS
-- =====================================================
ALTER TABLE public.resumes ENABLE ROW LEVEL SECURITY;

CREATE POLICY "Users can manage own resumes"
  ON public.resumes
  FOR ALL
  USING (user_id = auth.uid())
  WITH CHECK (user_id = auth.uid());

-- Org members can view resumes attached to applications of their jobs
CREATE POLICY "Org members can view resumes of applicants"
  ON public.resumes
  FOR SELECT
  USING (
    EXISTS (
      SELECT 1 FROM public.job_applications ja
      JOIN public.jobs j ON j.id = ja.job_id
      JOIN public.organization_members om ON om.organization_id = j.organization_id
      WHERE ja.resume_id = resumes.id
        AND om.user_id = auth.uid()
        AND om.status = 'active'
    )
  );

-- Users can upload to their own folder
CREATE POLICY "Users can upload own resumes"
ON storage.objects FOR INSERT TO authenticated
WITH CHECK (
  bucket_id = 'resumes' AND
  (storage.foldername(name))[1] = auth.uid()::text
);

CREATE POLICY "Users can update own resumes"
ON storage.objects FOR UPDATE TO authenticated
USING (
  bucket_id = 'resumes' AND
  (storage.foldername(name))[1] = auth.uid()::text
);

CREATE POLICY "Users can delete own resumes"
ON storage.objects FOR DELETE TO authenticated
USING (
  bucket_id = 'resumes' AND
  (storage.foldername(name))[1] = auth.uid()::text
);

CREATE POLICY "Users can read own resumes"
ON storage.objects FOR SELECT TO authenticated
USING (
  bucket_id = 'resumes' AND
  (storage.foldername(name))[1] = auth.uid()::text
);