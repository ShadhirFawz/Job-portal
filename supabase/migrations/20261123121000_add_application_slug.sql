ALTER TABLE public.job_applications
ADD COLUMN IF NOT EXISTS slug TEXT;

CREATE OR REPLACE FUNCTION public.generate_application_slug()
RETURNS TEXT
LANGUAGE plpgsql
VOLATILE
AS $$
DECLARE
  new_slug TEXT;
BEGIN
  LOOP
    new_slug := 'application-' || substr(replace(gen_random_uuid()::text, '-', ''), 1, 12);
    EXIT WHEN NOT EXISTS (SELECT 1 FROM public.job_applications WHERE slug = new_slug);
  END LOOP;
  RETURN new_slug;
END;
$$;

UPDATE public.job_applications
SET slug = 'application-' || substr(replace(gen_random_uuid()::text, '-', ''), 1, 8) || '-' || substr(replace(id::text, '-', ''), 1, 6)
WHERE slug IS NULL OR slug = '';

CREATE UNIQUE INDEX IF NOT EXISTS idx_job_applications_slug
ON public.job_applications(slug);

ALTER TABLE public.job_applications
ALTER COLUMN slug SET NOT NULL;

CREATE OR REPLACE FUNCTION public.set_application_slug()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
  IF NEW.slug IS NULL OR NEW.slug = '' THEN
    NEW.slug := public.generate_application_slug();
  END IF;
  RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS trigger_set_application_slug ON public.job_applications;
CREATE TRIGGER trigger_set_application_slug
BEFORE INSERT ON public.job_applications
FOR EACH ROW
EXECUTE FUNCTION public.set_application_slug();
