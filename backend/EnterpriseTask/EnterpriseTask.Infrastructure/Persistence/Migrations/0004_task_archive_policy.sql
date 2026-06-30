-- Product-level task deletion policy.
-- Tasks are archived instead of hard-deleted so history, comments, assignments,
-- attachments and audit evidence remain available to privileged data owners.

ALTER TABLE public.tasks
  ADD COLUMN IF NOT EXISTS archived_at TIMESTAMPTZ,
  ADD COLUMN IF NOT EXISTS archived_by UUID REFERENCES public.profiles(id) ON DELETE SET NULL,
  ADD COLUMN IF NOT EXISTS archive_reason TEXT;

CREATE INDEX IF NOT EXISTS idx_tasks_unarchived_created_at
  ON public.tasks(created_at DESC, id DESC)
  WHERE archived_at IS NULL;

CREATE INDEX IF NOT EXISTS idx_tasks_archived_at
  ON public.tasks(archived_at)
  WHERE archived_at IS NOT NULL;
