-- =====================================================
-- ENUMS
-- =====================================================
CREATE TYPE organization_status AS ENUM (
  'pending_verification',
  'active',
  'suspended',
  'rejected',
  'deleted'
);

CREATE TYPE organization_member_role AS ENUM (
  'org_admin',
  'org_staff'
);

CREATE TYPE organization_member_status AS ENUM (
  'pending',
  'active',
  'rejected',
  'revoked'
);

-- =====================================================
-- ORGANIZATIONS TABLE
-- =====================================================
CREATE TABLE public.organizations (
  id                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  
  -- Basic Info
  name                    TEXT NOT NULL,
  slug                    TEXT UNIQUE,                          -- for nice URLs (auto-generated)
  legal_name              TEXT,                                 -- Official registered name
  description             TEXT,
  tagline                 TEXT,                                 -- Short one-liner
  
  -- Contact & Official Email
  official_email          TEXT NOT NULL,                       -- Must be verified
  official_email_verified BOOLEAN NOT NULL DEFAULT false,
  phone                   TEXT,
  website_url             TEXT,
  support_email           TEXT,
  
  -- Media
  avatar_path             TEXT,                                 -- profile-images/organizations/{id}/avatar-...
  cover_path              TEXT,                                 -- profile-images/organizations/{id}/cover-...
  
  -- Location (optional physical office)
  has_physical_office     BOOLEAN NOT NULL DEFAULT false,
  country_code            CHAR(2),
  state_province          TEXT,
  city                    TEXT,
  postal_code             TEXT,
  address_line1           TEXT,
  address_line2           TEXT,
  latitude                DECIMAL(10, 8),
  longitude               DECIMAL(11, 8),
  
  -- Company Details
  industry                TEXT,
  company_size            TEXT,                                 -- e.g. '1-10', '11-50', '51-200', etc.
  founded_year            INTEGER,
  company_type            TEXT,                                 -- Private, Public, Non-profit, Startup, etc.
  
  -- Social & Links
  linkedin_url            TEXT,
  twitter_url             TEXT,
  facebook_url            TEXT,
  github_url              TEXT,
  glassdoor_url           TEXT,
  
  -- System
  status                  organization_status NOT NULL DEFAULT 'pending_verification',
  created_by              UUID NOT NULL REFERENCES public.users(id) ON DELETE RESTRICT,
  verified_at             TIMESTAMPTZ,
  metadata                JSONB DEFAULT '{}'::jsonb,
  
  created_at              TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  updated_at              TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  deleted_at              TIMESTAMPTZ
);

-- Indexes
CREATE INDEX idx_organizations_slug ON public.organizations(slug);
CREATE INDEX idx_organizations_status ON public.organizations(status);
CREATE INDEX idx_organizations_created_by ON public.organizations(created_by);
CREATE INDEX idx_organizations_official_email ON public.organizations(official_email);
CREATE INDEX idx_organizations_industry ON public.organizations(industry);

-- Updated_at trigger
CREATE TRIGGER organizations_updated_at
  BEFORE UPDATE ON public.organizations
  FOR EACH ROW
  EXECUTE FUNCTION update_updated_at_column();

-- =====================================================
-- ORGANIZATION MEMBERS TABLE
-- =====================================================
CREATE TABLE public.organization_members (
  id                UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  organization_id   UUID NOT NULL REFERENCES public.organizations(id) ON DELETE CASCADE,
  user_id           UUID NOT NULL REFERENCES public.users(id) ON DELETE CASCADE,
  
  role              organization_member_role NOT NULL DEFAULT 'org_staff',
  status            organization_member_status NOT NULL DEFAULT 'pending',
  
  -- Invitation / Approval
  invited_by        UUID REFERENCES public.users(id),
  invited_at        TIMESTAMPTZ,
  joined_at         TIMESTAMPTZ,
  revoked_at        TIMESTAMPTZ,
  revoked_by        UUID REFERENCES public.users(id),
  rejection_reason  TEXT,
  
  -- Permissions (flexible for future)
  permissions       JSONB DEFAULT '{}'::jsonb,   -- e.g. {"can_post_jobs": true, "can_manage_staff": false}
  
  title             TEXT,                         -- Job title inside the company (e.g. HR Manager)
  department        TEXT,
  
  created_at        TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  updated_at        TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  
  UNIQUE(organization_id, user_id)
);

CREATE INDEX idx_org_members_org_id ON public.organization_members(organization_id);
CREATE INDEX idx_org_members_user_id ON public.organization_members(user_id);
CREATE INDEX idx_org_members_status ON public.organization_members(status);

CREATE TRIGGER organization_members_updated_at
  BEFORE UPDATE ON public.organization_members
  FOR EACH ROW
  EXECUTE FUNCTION update_updated_at_column();

-- =====================================================
-- HELPER FUNCTION: Generate unique slug
-- =====================================================
CREATE OR REPLACE FUNCTION generate_organization_slug(org_name TEXT)
RETURNS TEXT
LANGUAGE plpgsql
AS $$
DECLARE
  base_slug TEXT;
  final_slug TEXT;
  counter INTEGER := 0;
BEGIN
  base_slug := lower(regexp_replace(org_name, '[^a-zA-Z0-9]+', '-', 'g'));
  base_slug := trim(both '-' from base_slug);
  final_slug := base_slug;
  
  WHILE EXISTS (SELECT 1 FROM public.organizations WHERE slug = final_slug) LOOP
    counter := counter + 1;
    final_slug := base_slug || '-' || counter;
  END LOOP;
  
  RETURN final_slug;
END;
$$;

-- =====================================================
-- ROW LEVEL SECURITY
-- =====================================================
ALTER TABLE public.organizations ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.organization_members ENABLE ROW LEVEL SECURITY;

-- Organizations: Anyone can view active organizations
CREATE POLICY "Public can view active organizations"
  ON public.organizations
  FOR SELECT
  USING (status = 'active' AND deleted_at IS NULL);

-- Organizations: Creator can view their own (even pending)
CREATE POLICY "Creators can view own organizations"
  ON public.organizations
  FOR SELECT
  USING (created_by = auth.uid());

-- Organizations: Authenticated users can create
CREATE POLICY "Authenticated users can create organizations"
  ON public.organizations
  FOR INSERT
  TO authenticated
  WITH CHECK (created_by = auth.uid());

-- Organizations: Admins of the org can update
CREATE POLICY "Org admins can update organization"
  ON public.organizations
  FOR UPDATE
  USING (
    EXISTS (
      SELECT 1 FROM public.organization_members om
      WHERE om.organization_id = id
        AND om.user_id = auth.uid()
        AND om.role = 'org_admin'
        AND om.status = 'active'
    )
  );

-- Organization Members policies (basic for now)
CREATE POLICY "Members can view own memberships"
  ON public.organization_members
  FOR SELECT
  USING (user_id = auth.uid());

CREATE POLICY "Org admins can view all members of their org"
  ON public.organization_members
  FOR SELECT
  USING (
    EXISTS (
      SELECT 1 FROM public.organization_members om
      WHERE om.organization_id = organization_members.organization_id
        AND om.user_id = auth.uid()
        AND om.role = 'org_admin'
        AND om.status = 'active'
    )
  );