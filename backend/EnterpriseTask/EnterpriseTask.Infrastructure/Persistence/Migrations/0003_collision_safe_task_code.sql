-- Collision-safe task codes.
-- Existing timestamp-style CV-* codes are preserved; the sequence starts after
-- the largest numeric suffix already present to avoid unique-key collisions.

CREATE SEQUENCE IF NOT EXISTS public.task_code_seq
  AS BIGINT
  INCREMENT BY 1
  MINVALUE 1
  START WITH 1
  CACHE 1;

DO $$
DECLARE
  v_max_code BIGINT;
BEGIN
  SELECT MAX(substring(code FROM '^CV-([0-9]+)$')::BIGINT)
  INTO v_max_code
  FROM public.tasks
  WHERE code ~ '^CV-[0-9]+$';

  IF v_max_code IS NULL OR v_max_code < 1 THEN
    PERFORM setval('public.task_code_seq'::regclass, 1, FALSE);
  ELSE
    PERFORM setval('public.task_code_seq'::regclass, v_max_code, TRUE);
  END IF;
END $$;

CREATE OR REPLACE FUNCTION public.next_task_code()
RETURNS VARCHAR(60)
LANGUAGE plpgsql
AS $$
DECLARE
  v_next BIGINT;
  v_suffix TEXT;
BEGIN
  v_next := nextval('public.task_code_seq'::regclass);
  v_suffix := v_next::TEXT;

  IF length(v_suffix) < 6 THEN
    v_suffix := lpad(v_suffix, 6, '0');
  END IF;

  RETURN 'CV-' || v_suffix;
END;
$$;

ALTER TABLE public.tasks
  ALTER COLUMN code SET DEFAULT public.next_task_code();

CREATE OR REPLACE FUNCTION public.set_task_defaults()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
  IF NEW.code IS NULL OR btrim(NEW.code) = '' THEN
    NEW.code = public.next_task_code();
  END IF;

  IF NEW.created_by IS NULL THEN
    NEW.created_by = auth.uid();
  END IF;

  IF NEW.reporter_id IS NULL THEN
    NEW.reporter_id = auth.uid();
  END IF;

  IF NEW.status_id IS NULL THEN
    SELECT id INTO NEW.status_id FROM public.task_statuses WHERE code = 'new';
  END IF;

  IF NEW.priority_id IS NULL THEN
    SELECT id INTO NEW.priority_id FROM public.task_priorities WHERE code = 'medium';
  END IF;

  RETURN NEW;
END;
$$;
