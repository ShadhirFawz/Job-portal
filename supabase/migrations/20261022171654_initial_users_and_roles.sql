-- Enable necessary extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- =====================================================
-- ENUMS
-- =====================================================
CREATE TYPE user_role AS ENUM (
  'super_admin',
  'org_admin',
  'org_staff',
  'candidate'
);

CREATE TYPE gender_type AS ENUM (
  'male',
  'female',
  'non_binary',
  'prefer_not_to_say',
  'other'
);

CREATE TYPE account_status AS ENUM (
  'active',
  'inactive',
  'suspended',
  'pending_verification',
  'deleted'
);

-- =====================================================
-- USERS TABLE (extends Supabase auth.users)
-- =====================================================
CREATE TABLE public.users (
  id                    UUID PRIMARY KEY REFERENCES auth.users(id) ON DELETE CASCADE,
  
  -- Basic Identity
  email                 TEXT NOT NULL UNIQUE,
  email_verified        BOOLEAN NOT NULL DEFAULT false,
  phone                 TEXT,
  phone_verified        BOOLEAN NOT NULL DEFAULT false,
  
  -- Profile
  first_name            TEXT,
  last_name             TEXT,
  display_name          TEXT,                     -- How name appears publicly
  username              TEXT UNIQUE,              -- Optional public username
  bio                   TEXT,
  date_of_birth         DATE,
  gender                gender_type,
  
  -- Location
  country_code          CHAR(2),                  -- ISO 3166-1 alpha-2
  state_province        TEXT,
  city                  TEXT,
  postal_code           TEXT,
  address_line1         TEXT,
  address_line2         TEXT,
  latitude              DECIMAL(10, 8),
  longitude             DECIMAL(11, 8),
  timezone              TEXT DEFAULT 'UTC',
  
  -- Professional
  current_job_title     TEXT,
  current_company       TEXT,
  years_of_experience   DECIMAL(4,1),
  headline              TEXT,                     -- LinkedIn-style headline
  website_url           TEXT,
  linkedin_url          TEXT,
  github_url            TEXT,
  portfolio_url         TEXT,
  
  -- Media (paths relative to storage bucket)
  avatar_path           TEXT,                     -- profile-images/users/{id}/avatar-...
  cover_path            TEXT,                     -- profile-images/users/{id}/cover-...
  
  -- System
  role                  user_role NOT NULL DEFAULT 'candidate',
  status                account_status NOT NULL DEFAULT 'pending_verification',
  is_profile_complete   BOOLEAN NOT NULL DEFAULT false,
  last_login_at         TIMESTAMPTZ,
  last_active_at        TIMESTAMPTZ,
  
  -- Preferences
  preferred_language    VARCHAR(10) DEFAULT 'en',
  notification_email    BOOLEAN NOT NULL DEFAULT true,
  notification_push     BOOLEAN NOT NULL DEFAULT true,
  notification_sms      BOOLEAN NOT NULL DEFAULT false,
  dark_mode             BOOLEAN DEFAULT false,
  
  -- Metadata
  metadata              JSONB DEFAULT '{}'::jsonb,  -- Flexible extra data
  created_at            TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  updated_at            TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  deleted_at            TIMESTAMPTZ                 -- Soft delete
);

-- Indexes
CREATE INDEX idx_users_email ON public.users(email);
CREATE INDEX idx_users_role ON public.users(role);
CREATE INDEX idx_users_status ON public.users(status);
CREATE INDEX idx_users_username ON public.users(username) WHERE username IS NOT NULL;
CREATE INDEX idx_users_location ON public.users(country_code, city);
CREATE INDEX idx_users_created_at ON public.users(created_at);

-- Updated_at trigger
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
  NEW.updated_at = NOW();
  RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER users_updated_at
  BEFORE UPDATE ON public.users
  FOR EACH ROW
  EXECUTE FUNCTION update_updated_at_column();

-- =====================================================
-- AUTOMATIC USER PROFILE CREATION ON SIGNUP
-- =====================================================
CREATE OR REPLACE FUNCTION public.handle_new_user()
RETURNS TRIGGER
LANGUAGE plpgsql
SECURITY DEFINER SET search_path = public
AS $$
BEGIN
  INSERT INTO public.users (
    id,
    email,
    email_verified,
    first_name,
    last_name,
    display_name,
    role,
    status
  )
  VALUES (
    NEW.id,
    NEW.email,
    COALESCE((NEW.raw_user_meta_data->>'email_verified')::boolean, false),
    NEW.raw_user_meta_data->>'first_name',
    NEW.raw_user_meta_data->>'last_name',
    COALESCE(
      NEW.raw_user_meta_data->>'display_name',
      NEW.raw_user_meta_data->>'full_name',
      split_part(NEW.email, '@', 1)
    ),
    'candidate',
    CASE 
      WHEN NEW.email_confirmed_at IS NOT NULL THEN 'active'::account_status
      ELSE 'pending_verification'::account_status
    END
  );
  RETURN NEW;
END;
$$;

-- Trigger on auth.users
CREATE TRIGGER on_auth_user_created
  AFTER INSERT ON auth.users
  FOR EACH ROW
  EXECUTE FUNCTION public.handle_new_user();

-- =====================================================
-- UPDATE email_verified when confirmed
-- =====================================================
CREATE OR REPLACE FUNCTION public.handle_user_email_confirmed()
RETURNS TRIGGER
LANGUAGE plpgsql
SECURITY DEFINER SET search_path = public
AS $$
BEGIN
  IF NEW.email_confirmed_at IS NOT NULL AND OLD.email_confirmed_at IS NULL THEN
    UPDATE public.users
    SET 
      email_verified = true,
      status = 'active',
      updated_at = NOW()
    WHERE id = NEW.id;
  END IF;
  RETURN NEW;
END;
$$;

CREATE TRIGGER on_auth_user_email_confirmed
  AFTER UPDATE OF email_confirmed_at ON auth.users
  FOR EACH ROW
  EXECUTE FUNCTION public.handle_user_email_confirmed();

-- =====================================================
-- ROW LEVEL SECURITY (basic for now)
-- =====================================================
ALTER TABLE public.users ENABLE ROW LEVEL SECURITY;

-- Users can read their own profile
CREATE POLICY "Users can view own profile"
  ON public.users
  FOR SELECT
  USING (auth.uid() = id);

-- Users can update their own profile (limited fields later)
CREATE POLICY "Users can update own profile"
  ON public.users
  FOR UPDATE
  USING (auth.uid() = id);

-- Super admins can do everything (we will refine later)
CREATE POLICY "Super admins full access"
  ON public.users
  FOR ALL
  USING (
    EXISTS (
      SELECT 1 FROM public.users u
      WHERE u.id = auth.uid() AND u.role = 'super_admin'
    )
  );