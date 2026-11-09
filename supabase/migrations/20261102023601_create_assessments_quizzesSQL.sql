-- =====================================================
-- ENUMS
-- =====================================================
DO $$ BEGIN
  CREATE TYPE question_type AS ENUM (
    'single_choice',
    'multiple_choice',
    'true_false',
    'structured'
  );
EXCEPTION
  WHEN duplicate_object THEN null;
END $$;

DO $$ BEGIN
  CREATE TYPE attempt_status AS ENUM (
    'not_started',
    'in_progress',
    'submitted',
    'graded',
    'expired'
  );
EXCEPTION
  WHEN duplicate_object THEN null;
END $$;

-- =====================================================
-- QUESTION BANKS
-- =====================================================
CREATE TABLE IF NOT EXISTS public.question_banks (
  id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  organization_id     UUID NOT NULL REFERENCES public.organizations(id) ON DELETE CASCADE,
  created_by          UUID NOT NULL REFERENCES public.users(id),
  updated_by          UUID REFERENCES public.users(id),
  
  name                TEXT NOT NULL,                     -- e.g. "React Questions", "JavaScript Core"
  description         TEXT,
  category            TEXT,                              -- e.g. "Frontend", "Backend", "HR"
  
  is_active           BOOLEAN NOT NULL DEFAULT true,
  created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  updated_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  deleted_at          TIMESTAMPTZ
);

CREATE INDEX IF NOT EXISTS idx_question_banks_org ON public.question_banks(organization_id);
CREATE INDEX IF NOT EXISTS idx_question_banks_creator ON public.question_banks(created_by);

CREATE TRIGGER question_banks_updated_at
  BEFORE UPDATE ON public.question_banks
  FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- =====================================================
-- QUESTIONS (Within a Question Bank)
-- =====================================================
CREATE TABLE IF NOT EXISTS public.questions (
  id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  bank_id             UUID NOT NULL REFERENCES public.question_banks(id) ON DELETE CASCADE,
  organization_id     UUID NOT NULL REFERENCES public.organizations(id) ON DELETE CASCADE,
  created_by          UUID NOT NULL REFERENCES public.users(id),
  
  body                TEXT NOT NULL,                     -- question statement / prompt
  question_type       question_type NOT NULL DEFAULT 'single_choice',
  
  -- Options for single_choice / multiple_choice / true_false (empty array for structured)
  -- Example: [{"id": "a", "text": "Option A", "is_correct": true}, ...]
  options             JSONB DEFAULT '[]'::jsonb,
  
  -- Model answer, rubrics, or explanation
  correct_answer      TEXT,
  explanation         TEXT,
  difficulty          TEXT CHECK (difficulty IN ('easy', 'medium', 'hard')) DEFAULT 'medium',
  points              DECIMAL(6,2) NOT NULL DEFAULT 1,
  order_index         INTEGER NOT NULL DEFAULT 0,
  
  is_active           BOOLEAN NOT NULL DEFAULT true,
  created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  updated_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  deleted_at          TIMESTAMPTZ
);

CREATE INDEX IF NOT EXISTS idx_questions_bank ON public.questions(bank_id);
CREATE INDEX IF NOT EXISTS idx_questions_org ON public.questions(organization_id);
CREATE INDEX IF NOT EXISTS idx_questions_type ON public.questions(question_type);

CREATE TRIGGER questions_updated_at
  BEFORE UPDATE ON public.questions
  FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- =====================================================
-- LINK QUESTION BANK TO JOB SELECTION PROCESSES
-- =====================================================
DO $$ BEGIN
  ALTER TABLE public.job_selection_processes 
    ADD COLUMN IF NOT EXISTS question_bank_id UUID REFERENCES public.question_banks(id) ON DELETE SET NULL;
EXCEPTION
  WHEN duplicate_column THEN null;
END $$;

CREATE INDEX IF NOT EXISTS idx_selection_processes_bank ON public.job_selection_processes(question_bank_id);

-- =====================================================
-- QUIZ / ASSESSMENT ATTEMPTS (By Candidates)
-- =====================================================
CREATE TABLE IF NOT EXISTS public.quiz_attempts (
  id                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  job_id                  UUID NOT NULL REFERENCES public.jobs(id) ON DELETE CASCADE,
  selection_process_id    UUID NOT NULL REFERENCES public.job_selection_processes(id) ON DELETE CASCADE,
  application_id          UUID REFERENCES public.job_applications(id) ON DELETE SET NULL,
  bank_id                 UUID REFERENCES public.question_banks(id) ON DELETE SET NULL,
  user_id                 UUID NOT NULL REFERENCES public.users(id) ON DELETE CASCADE,
  
  status                  attempt_status NOT NULL DEFAULT 'in_progress',
  
  started_at              TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  submitted_at            TIMESTAMPTZ,
  expires_at              TIMESTAMPTZ,                       -- calculated from selection_process.time_limit_minutes
  
  score                   DECIMAL(8,2) DEFAULT 0,
  max_score               DECIMAL(8,2) DEFAULT 0,
  percentage              DECIMAL(5,2) DEFAULT 0,
  passed                  BOOLEAN,
  
  created_at              TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  updated_at              TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_quiz_attempts_job ON public.quiz_attempts(job_id);
CREATE INDEX IF NOT EXISTS idx_quiz_attempts_process ON public.quiz_attempts(selection_process_id);
CREATE INDEX IF NOT EXISTS idx_quiz_attempts_user ON public.quiz_attempts(user_id);
CREATE INDEX IF NOT EXISTS idx_quiz_attempts_app ON public.quiz_attempts(application_id);

CREATE TRIGGER quiz_attempts_updated_at
  BEFORE UPDATE ON public.quiz_attempts
  FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- =====================================================
-- QUIZ / ASSESSMENT ANSWERS
-- =====================================================
CREATE TABLE IF NOT EXISTS public.quiz_answers (
  id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  attempt_id          UUID NOT NULL REFERENCES public.quiz_attempts(id) ON DELETE CASCADE,
  question_id         UUID NOT NULL REFERENCES public.questions(id) ON DELETE CASCADE,
  
  -- Answer value: string (for single/true_false/structured) or array (for multiple_choice)
  answer              JSONB,
  is_correct          BOOLEAN,
  points_awarded      DECIMAL(6,2) DEFAULT 0,
  
  answered_at         TIMESTAMPTZ DEFAULT NOW(),
  
  UNIQUE(attempt_id, question_id)
);

CREATE INDEX IF NOT EXISTS idx_quiz_answers_attempt ON public.quiz_answers(attempt_id);
CREATE INDEX IF NOT EXISTS idx_quiz_answers_question ON public.quiz_answers(question_id);

-- =====================================================
-- ROW LEVEL SECURITY (RLS)
-- =====================================================
ALTER TABLE public.question_banks ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.questions ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.quiz_attempts ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.quiz_answers ENABLE ROW LEVEL SECURITY;

-- -----------------------------------------------------
-- Question Banks RLS:
-- 1. Any active OrgAdmin or OrgStaff can VIEW question banks of their org.
-- 2. Any active OrgAdmin or OrgStaff can CREATE question banks.
-- 3. ONLY THE CREATOR (created_by) can UPDATE or DELETE their question bank!
-- -----------------------------------------------------
DROP POLICY IF EXISTS "Org members view question banks" ON public.question_banks;
CREATE POLICY "Org members view question banks"
  ON public.question_banks FOR SELECT
  USING (
    deleted_at IS NULL AND EXISTS (
      SELECT 1 FROM public.organization_members om
      WHERE om.organization_id = question_banks.organization_id
        AND om.user_id = auth.uid()
        AND om.status = 'active'
    )
  );

DROP POLICY IF EXISTS "Org officials create question banks" ON public.question_banks;
CREATE POLICY "Org officials create question banks"
  ON public.question_banks FOR INSERT
  WITH CHECK (
    created_by = auth.uid() AND EXISTS (
      SELECT 1 FROM public.organization_members om
      WHERE om.organization_id = organization_id
        AND om.user_id = auth.uid()
        AND om.status = 'active'
        AND om.role IN ('org_admin', 'org_staff')
    )
  );

DROP POLICY IF EXISTS "Only creator can update question bank" ON public.question_banks;
CREATE POLICY "Only creator can update question bank"
  ON public.question_banks FOR UPDATE
  USING (
    created_by = auth.uid() AND EXISTS (
      SELECT 1 FROM public.organization_members om
      WHERE om.organization_id = question_banks.organization_id
        AND om.user_id = auth.uid()
        AND om.status = 'active'
        AND om.role IN ('org_admin', 'org_staff')
    )
  );

DROP POLICY IF EXISTS "Only creator can delete question bank" ON public.question_banks;
CREATE POLICY "Only creator can delete question bank"
  ON public.question_banks FOR DELETE
  USING (
    created_by = auth.uid() AND EXISTS (
      SELECT 1 FROM public.organization_members om
      WHERE om.organization_id = question_banks.organization_id
        AND om.user_id = auth.uid()
        AND om.status = 'active'
        AND om.role IN ('org_admin', 'org_staff')
    )
  );

-- -----------------------------------------------------
-- Questions RLS:
-- 1. Org members can view questions of banks in their org.
-- 2. ONLY the creator of the question bank can add, edit, or delete questions in it!
-- -----------------------------------------------------
DROP POLICY IF EXISTS "Org members view questions" ON public.questions;
CREATE POLICY "Org members view questions"
  ON public.questions FOR SELECT
  USING (
    deleted_at IS NULL AND EXISTS (
      SELECT 1 FROM public.question_banks qb
      JOIN public.organization_members om ON om.organization_id = qb.organization_id
      WHERE qb.id = questions.bank_id
        AND om.user_id = auth.uid()
        AND om.status = 'active'
    )
  );

DROP POLICY IF EXISTS "Only bank creator can insert questions" ON public.questions;
CREATE POLICY "Only bank creator can insert questions"
  ON public.questions FOR INSERT
  WITH CHECK (
    created_by = auth.uid() AND EXISTS (
      SELECT 1 FROM public.question_banks qb
      WHERE qb.id = bank_id
        AND qb.created_by = auth.uid()
    )
  );

DROP POLICY IF EXISTS "Only bank creator can update questions" ON public.questions;
CREATE POLICY "Only bank creator can update questions"
  ON public.questions FOR UPDATE
  USING (
    EXISTS (
      SELECT 1 FROM public.question_banks qb
      WHERE qb.id = questions.bank_id
        AND qb.created_by = auth.uid()
    )
  );

DROP POLICY IF EXISTS "Only bank creator can delete questions" ON public.questions;
CREATE POLICY "Only bank creator can delete questions"
  ON public.questions FOR DELETE
  USING (
    EXISTS (
      SELECT 1 FROM public.question_banks qb
      WHERE qb.id = questions.bank_id
        AND qb.created_by = auth.uid()
    )
  );

-- -----------------------------------------------------
-- Attempts RLS:
-- 1. Candidates manage their own attempts.
-- 2. Org members can view attempts on their org's jobs.
-- -----------------------------------------------------
DROP POLICY IF EXISTS "Candidates manage own attempts" ON public.quiz_attempts;
CREATE POLICY "Candidates manage own attempts"
  ON public.quiz_attempts FOR ALL
  USING (user_id = auth.uid())
  WITH CHECK (user_id = auth.uid());

DROP POLICY IF EXISTS "Org members view job attempts" ON public.quiz_attempts;
CREATE POLICY "Org members view job attempts"
  ON public.quiz_attempts FOR SELECT
  USING (
    EXISTS (
      SELECT 1 FROM public.jobs j
      JOIN public.organization_members om ON om.organization_id = j.organization_id
      WHERE j.id = quiz_attempts.job_id
        AND om.user_id = auth.uid()
        AND om.status = 'active'
    )
  );

-- -----------------------------------------------------
-- Answers RLS:
-- -----------------------------------------------------
DROP POLICY IF EXISTS "Candidates manage own answers" ON public.quiz_answers;
CREATE POLICY "Candidates manage own answers"
  ON public.quiz_answers FOR ALL
  USING (
    EXISTS (
      SELECT 1 FROM public.quiz_attempts a
      WHERE a.id = attempt_id AND a.user_id = auth.uid()
    )
  );

DROP POLICY IF EXISTS "Org members view job answers" ON public.quiz_answers;
CREATE POLICY "Org members view job answers"
  ON public.quiz_answers FOR SELECT
  USING (
    EXISTS (
      SELECT 1 FROM public.quiz_attempts a
      JOIN public.jobs j ON j.id = a.job_id
      JOIN public.organization_members om ON om.organization_id = j.organization_id
      WHERE a.id = attempt_id
        AND om.user_id = auth.uid()
        AND om.status = 'active'
    )
  );
