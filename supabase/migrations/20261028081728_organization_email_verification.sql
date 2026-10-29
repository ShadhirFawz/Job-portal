-- =====================================================
-- Organization Email Verification Tokens
-- =====================================================
CREATE TABLE public.organization_email_verifications (
  id                UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  organization_id   UUID NOT NULL REFERENCES public.organizations(id) ON DELETE CASCADE,
  email             TEXT NOT NULL,
  token             TEXT NOT NULL UNIQUE,
  expires_at        TIMESTAMPTZ NOT NULL,
  verified_at       TIMESTAMPTZ,
  created_by        UUID REFERENCES public.users(id),
  created_at        TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_org_email_verifications_token ON public.organization_email_verifications(token);
CREATE INDEX idx_org_email_verifications_org ON public.organization_email_verifications(organization_id);

-- RLS
ALTER TABLE public.organization_email_verifications ENABLE ROW LEVEL SECURITY;

CREATE POLICY "Org admins can manage verification tokens"
  ON public.organization_email_verifications
  FOR ALL
  USING (
    EXISTS (
      SELECT 1 FROM public.organization_members om
      WHERE om.organization_id = organization_email_verifications.organization_id
        AND om.user_id = auth.uid()
        AND om.role = 'org_admin'
        AND om.status = 'active'
    )
  );

-- Anyone with the token can read it (needed for verification page)
CREATE POLICY "Anyone can read verification by token"
  ON public.organization_email_verifications
  FOR SELECT
  USING (true);