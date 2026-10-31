-- =====================================================
-- ENUMS
-- =====================================================
CREATE TYPE job_status AS ENUM (
  'draft',
  'published',
  'paused',
  'closed',
  'archived'
);

CREATE TYPE job_type AS ENUM (
  'full_time',
  'part_time',
  'contract',
  'internship',
  'freelance',
  'temporary'
);

CREATE TYPE work_mode AS ENUM (
  'onsite',
  'remote',
  'hybrid'
);

CREATE TYPE experience_level AS ENUM (
  'entry',
  'junior',
  'mid',
  'senior',
  'lead',
  'executive'
);

CREATE TYPE selection_process_type AS ENUM (
  'quiz',
  'screening',
  'coding',
  'screening_quiz',
  'screening_coding',
  'quiz_coding',
  'custom'
);

CREATE TYPE application_status AS ENUM (
  'applied',
  'under_review',
  'shortlisted',
  'interview',
  'offered',
  'hired',
  'rejected',
  'withdrawn'
);

-- =====================================================
-- JOBS TABLE
-- =====================================================
CREATE TABLE public.jobs (
  id                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  organization_id         UUID NOT NULL REFERENCES public.organizations(id) ON DELETE CASCADE,
  
  -- Basic Info
  title                   TEXT NOT NULL,
  slug                    TEXT,
  description             TEXT,                          -- Rich text / markdown
  short_description       TEXT,                          -- For cards
  responsibilities        TEXT,
  requirements            TEXT,
  benefits                TEXT,
  
  -- Classification
  job_type                job_type NOT NULL DEFAULT 'full_time',
  work_mode               work_mode NOT NULL DEFAULT 'onsite',
  experience_level        experience_level,
  department              TEXT,
  category                TEXT,                          -- e.g. Engineering, Design, Sales
  
  -- Location
  country_code            CHAR(2),
  city                    TEXT,
  state_province          TEXT,
  is_remote_friendly      BOOLEAN NOT NULL DEFAULT false,
  
  -- Compensation
  salary_min              INTEGER,
  salary_max              INTEGER,
  salary_currency         CHAR(3) DEFAULT 'INR',
  salary_period           TEXT DEFAULT 'year',           -- year, month, hour
  show_salary             BOOLEAN NOT NULL DEFAULT false,
  
  -- Application Settings
  application_deadline    TIMESTAMPTZ,
  max_applications        INTEGER,                       -- null = unlimited
  accept_applications     BOOLEAN NOT NULL DEFAULT true,
  custom_form_schema      JSONB DEFAULT '[]'::jsonb,     -- Array of custom fields
  
  -- System
  status                  job_status NOT NULL DEFAULT 'draft',
  published_at            TIMESTAMPTZ,
  closed_at               TIMESTAMPTZ,
  views_count             INTEGER NOT NULL DEFAULT 0,
  applications_count      INTEGER NOT NULL DEFAULT 0,
  
  created_by              UUID NOT NULL REFERENCES public.users(id),
  updated_by              UUID REFERENCES public.users(id),
  
  metadata                JSONB DEFAULT '{}'::jsonb,
  created_at              TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  updated_at              TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  deleted_at              TIMESTAMPTZ
);

CREATE INDEX idx_jobs_organization ON public.jobs(organization_id);
CREATE INDEX idx_jobs_status ON public.jobs(status);
CREATE INDEX idx_jobs_published_at ON public.jobs(published_at DESC);
CREATE INDEX idx_jobs_slug ON public.jobs(slug);

CREATE TRIGGER jobs_updated_at
  BEFORE UPDATE ON public.jobs
  FOR EACH ROW
  EXECUTE FUNCTION update_updated_at_column();

-- =====================================================
-- SELECTION PROCESSES (max 3 per job)
-- =====================================================
CREATE TABLE public.job_selection_processes (
  id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  job_id              UUID NOT NULL REFERENCES public.jobs(id) ON DELETE CASCADE,
  
  title               TEXT NOT NULL,                     -- e.g. "Aptitude Test", "Technical Screening"
  process_type        selection_process_type NOT NULL,
  description         TEXT,
  order_index         SMALLINT NOT NULL,                 -- 1, 2 or 3
  
  -- Configuration
  is_mandatory        BOOLEAN NOT NULL DEFAULT true,
  time_limit_minutes  INTEGER,                           -- null = no limit
  passing_score       DECIMAL(5,2),                      -- percentage or absolute
  
  -- For quiz / coding later
  config              JSONB DEFAULT '{}'::jsonb,         -- Flexible config (questions, languages, etc.)
  
  created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  updated_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  
  UNIQUE(job_id, order_index)
);

CREATE INDEX idx_selection_processes_job ON public.job_selection_processes(job_id);

CREATE TRIGGER job_selection_processes_updated_at
  BEFORE UPDATE ON public.job_selection_processes
  FOR EACH ROW
  EXECUTE FUNCTION update_updated_at_column();

-- Constraint: maximum 3 processes per job (enforced by application + optional trigger)
CREATE OR REPLACE FUNCTION check_max_selection_processes()
RETURNS TRIGGER AS $$
BEGIN
  IF (SELECT COUNT(*) FROM public.job_selection_processes WHERE job_id = NEW.job_id) >= 3 THEN
    RAISE EXCEPTION 'A job can have a maximum of 3 selection processes';
  END IF;
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER enforce_max_selection_processes
  BEFORE INSERT ON public.job_selection_processes
  FOR EACH ROW
  EXECUTE FUNCTION check_max_selection_processes();

-- =====================================================
-- JOB APPLICATIONS (basic for now)
-- =====================================================
CREATE TABLE public.job_applications (
  id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  job_id              UUID NOT NULL REFERENCES public.jobs(id) ON DELETE CASCADE,
  user_id             UUID NOT NULL REFERENCES public.users(id) ON DELETE CASCADE,
  
  status              application_status NOT NULL DEFAULT 'applied',
  cover_letter        TEXT,
  resume_id           UUID,                              -- will link to resumes table later
  custom_answers      JSONB DEFAULT '{}'::jsonb,         -- answers to custom form fields
  
  current_process_id  UUID REFERENCES public.job_selection_processes(id),
  score               DECIMAL(8,2),
  
  applied_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  updated_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  withdrawn_at        TIMESTAMPTZ,
  
  UNIQUE(job_id, user_id)
);

CREATE INDEX idx_applications_job ON public.job_applications(job_id);
CREATE INDEX idx_applications_user ON public.job_applications(user_id);
CREATE INDEX idx_applications_status ON public.job_applications(status);

CREATE TRIGGER job_applications_updated_at
  BEFORE UPDATE ON public.job_applications
  FOR EACH ROW
  EXECUTE FUNCTION update_updated_at_column();

-- =====================================================
-- ROW LEVEL SECURITY
-- =====================================================
ALTER TABLE public.jobs ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.job_selection_processes ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.job_applications ENABLE ROW LEVEL SECURITY;

-- Jobs: Public can view published jobs
CREATE POLICY "Anyone can view published jobs"
  ON public.jobs FOR SELECT
  USING (status = 'published' AND deleted_at IS NULL);

-- Jobs: Org members can view all jobs of their org
CREATE POLICY "Org members can view their organization jobs"
  ON public.jobs FOR SELECT
  USING (
    EXISTS (
      SELECT 1 FROM public.organization_members om
      WHERE om.organization_id = jobs.organization_id
        AND om.user_id = auth.uid()
        AND om.status = 'active'
    )
  );

-- Jobs: Org admins/staff can create
CREATE POLICY "Org members can create jobs"
  ON public.jobs FOR INSERT
  TO authenticated
  WITH CHECK (
    EXISTS (
      SELECT 1 FROM public.organization_members om
      WHERE om.organization_id = organization_id
        AND om.user_id = auth.uid()
        AND om.status = 'active'
        AND om.role IN ('org_admin', 'org_staff')
    )
  );

-- Jobs: Org admins/staff can update
CREATE POLICY "Org members can update jobs"
  ON public.jobs FOR UPDATE
  USING (
    EXISTS (
      SELECT 1 FROM public.organization_members om
      WHERE om.organization_id = jobs.organization_id
        AND om.user_id = auth.uid()
        AND om.status = 'active'
        AND om.role IN ('org_admin', 'org_staff')
    )
  );

-- Selection processes follow job permissions (simplified)
CREATE POLICY "Selection processes visible with job"
  ON public.job_selection_processes FOR SELECT
  USING (
    EXISTS (
      SELECT 1 FROM public.jobs j
      WHERE j.id = job_id
        AND (
          j.status = 'published'
          OR EXISTS (
            SELECT 1 FROM public.organization_members om
            WHERE om.organization_id = j.organization_id
              AND om.user_id = auth.uid()
              AND om.status = 'active'
          )
        )
    )
  );

CREATE POLICY "Org members can manage selection processes"
  ON public.job_selection_processes FOR ALL
  USING (
    EXISTS (
      SELECT 1 FROM public.jobs j
      JOIN public.organization_members om ON om.organization_id = j.organization_id
      WHERE j.id = job_id
        AND om.user_id = auth.uid()
        AND om.status = 'active'
        AND om.role IN ('org_admin', 'org_staff')
    )
  );

-- Applications
CREATE POLICY "Users can view own applications"
  ON public.job_applications FOR SELECT
  USING (user_id = auth.uid());

CREATE POLICY "Org members can view applications for their jobs"
  ON public.job_applications FOR SELECT
  USING (
    EXISTS (
      SELECT 1 FROM public.jobs j
      JOIN public.organization_members om ON om.organization_id = j.organization_id
      WHERE j.id = job_id
        AND om.user_id = auth.uid()
        AND om.status = 'active'
    )
  );

CREATE POLICY "Authenticated users can apply"
  ON public.job_applications FOR INSERT
  TO authenticated
  WITH CHECK (user_id = auth.uid());