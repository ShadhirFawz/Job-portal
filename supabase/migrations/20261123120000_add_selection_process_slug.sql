-- Add stable slugs for selection-process routes.
ALTER TABLE public.job_selection_processes
ADD COLUMN IF NOT EXISTS slug TEXT;

CREATE OR REPLACE FUNCTION public.generate_selection_process_slug(process_title TEXT)
RETURNS TEXT
LANGUAGE plpgsql
AS $$
DECLARE
  base_slug TEXT;
  final_slug TEXT;
  counter INTEGER := 1;
BEGIN
  base_slug := lower(regexp_replace(COALESCE(process_title, 'stage'), '[^a-zA-Z0-9]+', '-', 'g'));
  base_slug := trim(both '-' from base_slug);
  IF base_slug = '' THEN
    base_slug := 'stage';
  END IF;

  final_slug := base_slug;
  WHILE EXISTS (SELECT 1 FROM public.job_selection_processes WHERE slug = final_slug) LOOP
    final_slug := base_slug || '-' || counter;
    counter := counter + 1;
  END LOOP;

  RETURN final_slug;
END;
$$;

UPDATE public.job_selection_processes
SET slug = public.generate_selection_process_slug(title)
WHERE slug IS NULL OR slug = '';

CREATE UNIQUE INDEX IF NOT EXISTS idx_job_selection_processes_slug
ON public.job_selection_processes(slug);

ALTER TABLE public.job_selection_processes
ALTER COLUMN slug SET NOT NULL;

CREATE OR REPLACE FUNCTION public.set_selection_process_slug()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
  IF NEW.slug IS NULL OR NEW.slug = '' THEN
    NEW.slug := public.generate_selection_process_slug(NEW.title);
  END IF;
  RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS trigger_set_selection_process_slug ON public.job_selection_processes;
CREATE TRIGGER trigger_set_selection_process_slug
BEFORE INSERT OR UPDATE OF title
ON public.job_selection_processes
FOR EACH ROW
EXECUTE FUNCTION public.set_selection_process_slug();
