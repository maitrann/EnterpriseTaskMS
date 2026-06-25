-- ============================================================
-- Enterprise Task Management System - Supabase PostgreSQL Schema V2.1
-- Forward-only initial migration for portfolio/demo environments.
--
-- MIGRATION NOTES:
-- Apply through POST /api/dev/migrate or the documented migration workflow.
--
-- SAFETY:
-- - This migration intentionally contains no DROP statements.
-- - The migrator records this migration as a baseline when an existing app schema is detected.
-- - Run on an empty database for first-time setup.
-- ============================================================

DO $$
BEGIN
  IF to_regclass('public.tasks') IS NOT NULL THEN
    RAISE EXCEPTION 'Initial migration can only run on an empty EnterpriseTask schema. Existing schemas must be baselined by the migrator.';
  END IF;
END $$;
-- ---------- 0. Extensions ----------
CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE EXTENSION IF NOT EXISTS vector WITH SCHEMA extensions;

-- ---------- 2. Types ----------
CREATE TYPE public.task_status_code AS ENUM (
  'new',
  'assigned',
  'in_progress',
  'pending_review',
  'completed',
  'closed',
  'on_hold',
  'cancelled',
  'overdue'
);

CREATE TYPE public.task_priority_code AS ENUM ('low', 'medium', 'high', 'critical');

CREATE TYPE public.project_status_code AS ENUM (
  'planning',
  'active',
  'on_hold',
  'completed',
  'cancelled'
);

CREATE TYPE public.extension_request_status AS ENUM ('pending', 'approved', 'rejected');

CREATE TYPE public.task_assignment_type AS ENUM (
  'assignee',
  'co_assignee',
  'watcher'
);

CREATE TYPE public.subtask_status_code AS ENUM (
  'todo',
  'in_progress',
  'done',
  'cancelled'
);

CREATE TYPE public.inter_request_type AS ENUM (
  'procurement',
  'asset',
  'it-support',
  'payment',
  'recruitment',
  'communication-design',
  'legal'
);

CREATE TYPE public.inter_request_status AS ENUM (
  'new',
  'received',
  'processing',
  'waiting-requester',
  'waiting-target',
  'done',
  'closed',
  'rejected'
);

CREATE TYPE public.request_priority_code AS ENUM ('low', 'medium', 'high', 'critical');
CREATE TYPE public.request_message_role AS ENUM ('requester', 'processor', 'coordinator');

CREATE TYPE public.notification_type AS ENUM (
  'task_assigned',
  'task_status_changed',
  'task_comment',
  'task_mention',
  'task_due_soon',
  'task_overdue',
  'inter_request_updated',
  'ai_result'
);

CREATE TYPE public.ai_feature_code AS ENUM (
  'task_draft',
  'task_summary',
  'task_risk',
  'semantic_search',
  'smart_assignment',
  'auto_classification'
);

CREATE TYPE public.ai_request_status AS ENUM ('success', 'failed', 'blocked', 'cancelled');
CREATE TYPE public.ai_risk_level AS ENUM ('low', 'medium', 'high');

-- ---------- 3. Base organization tables ----------
CREATE TABLE public.companies (
  id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  code VARCHAR(50) NOT NULL UNIQUE,
  name VARCHAR(200) NOT NULL,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ
);

CREATE TABLE public.departments (
  id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  company_id BIGINT NOT NULL REFERENCES public.companies(id) ON DELETE CASCADE,
  parent_department_id BIGINT REFERENCES public.departments(id) ON DELETE SET NULL,
  code VARCHAR(50),
  name VARCHAR(200) NOT NULL,
  description TEXT,
  is_active BOOLEAN NOT NULL DEFAULT TRUE,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ,
  UNIQUE (company_id, name),
  UNIQUE (company_id, code)
);

CREATE TABLE public.profiles (
  id UUID PRIMARY KEY REFERENCES auth.users(id) ON DELETE CASCADE,
  employee_code VARCHAR(80) UNIQUE,
  email TEXT UNIQUE,
  full_name VARCHAR(200),
  avatar_url TEXT,
  department_id BIGINT REFERENCES public.departments(id) ON DELETE SET NULL,
  manager_id UUID REFERENCES public.profiles(id) ON DELETE SET NULL,
  job_title VARCHAR(160),
  is_active BOOLEAN NOT NULL DEFAULT TRUE,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ
);

ALTER TABLE public.departments
ADD COLUMN manager_id UUID REFERENCES public.profiles(id) ON DELETE SET NULL;

-- ---------- 4. RBAC ----------
CREATE TABLE public.roles (
  id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  code VARCHAR(80) NOT NULL UNIQUE,
  name VARCHAR(160) NOT NULL,
  description TEXT,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE public.permissions (
  id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  code VARCHAR(120) NOT NULL UNIQUE,
  name VARCHAR(200) NOT NULL,
  module VARCHAR(80) NOT NULL,
  description TEXT,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE public.user_roles (
  user_id UUID NOT NULL REFERENCES public.profiles(id) ON DELETE CASCADE,
  role_id BIGINT NOT NULL REFERENCES public.roles(id) ON DELETE CASCADE,
  assigned_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  PRIMARY KEY (user_id, role_id)
);

CREATE TABLE public.role_permissions (
  role_id BIGINT NOT NULL REFERENCES public.roles(id) ON DELETE CASCADE,
  permission_id BIGINT NOT NULL REFERENCES public.permissions(id) ON DELETE CASCADE,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  PRIMARY KEY (role_id, permission_id)
);

CREATE TABLE public.user_department_scopes (
  user_id UUID NOT NULL REFERENCES public.profiles(id) ON DELETE CASCADE,
  department_id BIGINT NOT NULL REFERENCES public.departments(id) ON DELETE CASCADE,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  PRIMARY KEY (user_id, department_id)
);

-- ---------- 5. Project ----------
CREATE TABLE public.projects (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  code VARCHAR(60) UNIQUE,
  name VARCHAR(240) NOT NULL,
  description TEXT,
  department_id BIGINT REFERENCES public.departments(id) ON DELETE SET NULL,
  owner_id UUID REFERENCES public.profiles(id) ON DELETE SET NULL,
  start_date DATE,
  end_date DATE,
  status public.project_status_code NOT NULL DEFAULT 'planning',
  created_by UUID REFERENCES public.profiles(id) ON DELETE SET NULL,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ,
  CONSTRAINT chk_project_date_range CHECK (end_date IS NULL OR start_date IS NULL OR end_date >= start_date)
);

CREATE TABLE public.project_members (
  project_id UUID NOT NULL REFERENCES public.projects(id) ON DELETE CASCADE,
  user_id UUID NOT NULL REFERENCES public.profiles(id) ON DELETE CASCADE,
  member_role VARCHAR(120),
  joined_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  PRIMARY KEY (project_id, user_id)
);

-- ---------- 6. Task lookup tables ----------
CREATE TABLE public.task_statuses (
  id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  code public.task_status_code NOT NULL UNIQUE,
  name VARCHAR(120) NOT NULL,
  sort_order INT NOT NULL DEFAULT 0,
  color VARCHAR(20),
  is_closed BOOLEAN NOT NULL DEFAULT FALSE
);

CREATE TABLE public.task_priorities (
  id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  code public.task_priority_code NOT NULL UNIQUE,
  name VARCHAR(120) NOT NULL,
  level INT NOT NULL,
  color VARCHAR(20)
);

-- ---------- 7. Tasks ----------
CREATE TABLE public.tasks (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  code VARCHAR(60) NOT NULL UNIQUE,
  project_id UUID REFERENCES public.projects(id) ON DELETE SET NULL,
  parent_task_id UUID REFERENCES public.tasks(id) ON DELETE SET NULL,
  title VARCHAR(300) NOT NULL,
  description TEXT,
  task_type VARCHAR(100),
  source VARCHAR(120),
  department_id BIGINT REFERENCES public.departments(id) ON DELETE SET NULL,
  status_id BIGINT REFERENCES public.task_statuses(id) ON DELETE SET NULL,
  priority_id BIGINT REFERENCES public.task_priorities(id) ON DELETE SET NULL,
  urgency_level VARCHAR(60),
  security_level VARCHAR(60),
  is_confidential BOOLEAN NOT NULL DEFAULT FALSE,
  reporter_id UUID REFERENCES public.profiles(id) ON DELETE SET NULL,
  created_by UUID REFERENCES public.profiles(id) ON DELETE SET NULL,
  start_date DATE,
  due_date DATE,
  progress SMALLINT NOT NULL DEFAULT 0,
  subtask_progress_auto_sync BOOLEAN NOT NULL DEFAULT TRUE,
  parent_completion_suggested BOOLEAN NOT NULL DEFAULT FALSE,
  estimated_hours NUMERIC(8, 2),
  actual_hours NUMERIC(8, 2),
  completed_at TIMESTAMPTZ,
  closed_at TIMESTAMPTZ,
  overdue_at TIMESTAMPTZ,
  last_activity_at TIMESTAMPTZ,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ,
  CONSTRAINT chk_task_progress CHECK (progress BETWEEN 0 AND 100),
  CONSTRAINT chk_task_date_range CHECK (due_date IS NULL OR start_date IS NULL OR due_date >= start_date),
  CONSTRAINT chk_task_not_self_parent CHECK (parent_task_id IS NULL OR parent_task_id <> id)
);

CREATE TABLE public.task_assignments (
  task_id UUID NOT NULL REFERENCES public.tasks(id) ON DELETE CASCADE,
  user_id UUID NOT NULL REFERENCES public.profiles(id) ON DELETE CASCADE,
  assignment_type public.task_assignment_type NOT NULL,
  assigned_by UUID REFERENCES public.profiles(id) ON DELETE SET NULL,
  assigned_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  PRIMARY KEY (task_id, user_id, assignment_type)
);

CREATE TABLE public.subtasks (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  task_id UUID NOT NULL REFERENCES public.tasks(id) ON DELETE CASCADE,
  title VARCHAR(300) NOT NULL,
  assignee_id UUID REFERENCES public.profiles(id) ON DELETE SET NULL,
  status public.subtask_status_code NOT NULL DEFAULT 'todo',
  due_date DATE,
  progress SMALLINT NOT NULL DEFAULT 0,
  done BOOLEAN NOT NULL DEFAULT FALSE,
  sort_order INT NOT NULL DEFAULT 0,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ,
  completed_at TIMESTAMPTZ,
  CONSTRAINT chk_subtask_progress CHECK (progress BETWEEN 0 AND 100)
);

CREATE TABLE public.task_extension_requests (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  task_id UUID NOT NULL REFERENCES public.tasks(id) ON DELETE CASCADE,
  requested_due_date DATE NOT NULL,
  reason TEXT NOT NULL,
  status public.extension_request_status NOT NULL DEFAULT 'pending',
  requested_by_user_id UUID REFERENCES public.profiles(id) ON DELETE SET NULL,
  requested_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  reviewed_by_user_id UUID REFERENCES public.profiles(id) ON DELETE SET NULL,
  reviewed_at TIMESTAMPTZ,
  review_note TEXT
);

CREATE TABLE public.task_comments (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  task_id UUID NOT NULL REFERENCES public.tasks(id) ON DELETE CASCADE,
  user_id UUID REFERENCES public.profiles(id) ON DELETE SET NULL,
  content TEXT NOT NULL,
  is_internal BOOLEAN NOT NULL DEFAULT FALSE,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ
);

CREATE TABLE public.task_comment_mentions (
  comment_id UUID NOT NULL REFERENCES public.task_comments(id) ON DELETE CASCADE,
  mentioned_user_id UUID NOT NULL REFERENCES public.profiles(id) ON DELETE CASCADE,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  PRIMARY KEY (comment_id, mentioned_user_id)
);

CREATE TABLE public.task_activities (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  task_id UUID NOT NULL REFERENCES public.tasks(id) ON DELETE CASCADE,
  user_id UUID REFERENCES public.profiles(id) ON DELETE SET NULL,
  action_type VARCHAR(120),
  old_value TEXT,
  new_value TEXT,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE public.tags (
  id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
  name VARCHAR(120) NOT NULL UNIQUE,
  color VARCHAR(20),
  created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE public.task_tags (
  task_id UUID NOT NULL REFERENCES public.tasks(id) ON DELETE CASCADE,
  tag_id BIGINT NOT NULL REFERENCES public.tags(id) ON DELETE CASCADE,
  PRIMARY KEY (task_id, tag_id)
);

-- ---------- 8. Inter-department requests ----------
CREATE TABLE public.inter_request_sla_policies (
  key public.inter_request_type PRIMARY KEY,
  label VARCHAR(160) NOT NULL,
  target_hours INT NOT NULL,
  warn_hours INT NOT NULL
);

CREATE TABLE public.inter_department_requests (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  code VARCHAR(60) NOT NULL UNIQUE,
  type public.inter_request_type NOT NULL,
  title VARCHAR(300) NOT NULL,
  description TEXT NOT NULL,
  requester_department_id BIGINT REFERENCES public.departments(id) ON DELETE SET NULL,
  requester_user_id UUID REFERENCES public.profiles(id) ON DELETE SET NULL,
  target_department_id BIGINT REFERENCES public.departments(id) ON DELETE SET NULL,
  owner_id UUID REFERENCES public.profiles(id) ON DELETE SET NULL,
  priority public.request_priority_code NOT NULL DEFAULT 'medium',
  status public.inter_request_status NOT NULL DEFAULT 'new',
  due_date DATE NOT NULL,
  sla_policy_key public.inter_request_type REFERENCES public.inter_request_sla_policies(key),
  sla_started_at TIMESTAMPTZ,
  sla_due_at TIMESTAMPTZ,
  sla_breached BOOLEAN NOT NULL DEFAULT FALSE,
  form_values JSONB NOT NULL DEFAULT '{}'::jsonb,
  latest_message TEXT,
  note TEXT,
  ai_classified_type public.inter_request_type,
  ai_classification_reason TEXT,
  ai_confidence NUMERIC(5, 2),
  received_at TIMESTAMPTZ,
  closed_at TIMESTAMPTZ,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ
);

CREATE TABLE public.inter_request_messages (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  request_id UUID NOT NULL REFERENCES public.inter_department_requests(id) ON DELETE CASCADE,
  author_user_id UUID REFERENCES public.profiles(id) ON DELETE SET NULL,
  author_name VARCHAR(200) NOT NULL,
  author_role public.request_message_role NOT NULL,
  author_department VARCHAR(200),
  body TEXT NOT NULL,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- ---------- 9. Attachments ----------
CREATE TABLE public.attachments (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  task_id UUID REFERENCES public.tasks(id) ON DELETE CASCADE,
  inter_request_id UUID REFERENCES public.inter_department_requests(id) ON DELETE CASCADE,
  file_name VARCHAR(255) NOT NULL,
  file_url TEXT NOT NULL,
  file_size BIGINT,
  content_type VARCHAR(120),
  uploaded_by UUID REFERENCES public.profiles(id) ON DELETE SET NULL,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  CONSTRAINT chk_attachment_exactly_one_owner CHECK (num_nonnulls(task_id, inter_request_id) = 1)
);

-- ---------- 10. Notifications ----------
CREATE TABLE public.notifications (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id UUID NOT NULL REFERENCES public.profiles(id) ON DELETE CASCADE,
  type public.notification_type NOT NULL,
  title VARCHAR(250) NOT NULL,
  content TEXT,
  reference_type VARCHAR(80),
  reference_id TEXT,
  is_read BOOLEAN NOT NULL DEFAULT FALSE,
  read_at TIMESTAMPTZ,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE public.notification_preferences (
  user_id UUID PRIMARY KEY REFERENCES public.profiles(id) ON DELETE CASCADE,
  realtime_enabled BOOLEAN NOT NULL DEFAULT TRUE,
  email_enabled BOOLEAN NOT NULL DEFAULT TRUE,
  due_soon_hours INT[] NOT NULL DEFAULT ARRAY[24, 8, 1],
  updated_at TIMESTAMPTZ
);

-- ---------- 11. Audit logs ----------
CREATE TABLE public.audit_logs (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  actor_id UUID REFERENCES public.profiles(id) ON DELETE SET NULL,
  action VARCHAR(120) NOT NULL,
  entity_name VARCHAR(120) NOT NULL,
  entity_id TEXT NOT NULL,
  old_value JSONB,
  new_value JSONB,
  ip_address INET,
  user_agent TEXT,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- ---------- 12. AI ----------
CREATE TABLE public.ai_feature_settings (
  feature_code public.ai_feature_code PRIMARY KEY,
  is_enabled BOOLEAN NOT NULL DEFAULT TRUE,
  description TEXT,
  updated_by UUID REFERENCES public.profiles(id) ON DELETE SET NULL,
  updated_at TIMESTAMPTZ
);

CREATE TABLE public.ai_request_logs (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id UUID REFERENCES public.profiles(id) ON DELETE SET NULL,
  feature_code public.ai_feature_code NOT NULL,
  reference_type VARCHAR(80),
  reference_id TEXT,
  input_hash TEXT,
  status public.ai_request_status NOT NULL,
  error_message TEXT,
  token_input INT,
  token_output INT,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE public.ai_task_insights (
  task_id UUID PRIMARY KEY REFERENCES public.tasks(id) ON DELETE CASCADE,
  risk_level public.ai_risk_level,
  risk_reason TEXT,
  suggested_action TEXT,
  summary TEXT,
  generated_by UUID REFERENCES public.profiles(id) ON DELETE SET NULL,
  generated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE public.embedding_index (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  entity_type VARCHAR(80) NOT NULL,
  entity_id TEXT NOT NULL,
  text_chunk TEXT NOT NULL,
  embedding extensions.vector(1536),
  metadata JSONB NOT NULL DEFAULT '{}'::jsonb,
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  UNIQUE (entity_type, entity_id, text_chunk)
);

-- ---------- 13. Indexes ----------
CREATE INDEX idx_departments_company ON public.departments(company_id);
CREATE INDEX idx_departments_parent ON public.departments(parent_department_id);
CREATE INDEX idx_profiles_department ON public.profiles(department_id);
CREATE INDEX idx_profiles_manager ON public.profiles(manager_id);
CREATE INDEX idx_profiles_email ON public.profiles(email);

CREATE INDEX idx_user_department_scopes_department ON public.user_department_scopes(department_id);
CREATE INDEX idx_project_members_user ON public.project_members(user_id);
CREATE INDEX idx_projects_department ON public.projects(department_id);
CREATE INDEX idx_projects_owner ON public.projects(owner_id);

CREATE INDEX idx_tasks_project ON public.tasks(project_id);
CREATE INDEX idx_tasks_parent ON public.tasks(parent_task_id);
CREATE INDEX idx_tasks_status ON public.tasks(status_id);
CREATE INDEX idx_tasks_priority ON public.tasks(priority_id);
CREATE INDEX idx_tasks_department_status ON public.tasks(department_id, status_id);
CREATE INDEX idx_tasks_due_date ON public.tasks(due_date);
CREATE INDEX idx_tasks_created_by ON public.tasks(created_by);
CREATE INDEX idx_tasks_reporter ON public.tasks(reporter_id);
CREATE INDEX idx_tasks_confidential ON public.tasks(is_confidential);

CREATE INDEX idx_task_assignments_user ON public.task_assignments(user_id);
CREATE INDEX idx_task_assignments_task ON public.task_assignments(task_id);
CREATE INDEX idx_task_assignments_type ON public.task_assignments(assignment_type);

CREATE INDEX idx_subtasks_task ON public.subtasks(task_id);
CREATE INDEX idx_task_comments_task ON public.task_comments(task_id);
CREATE INDEX idx_task_activities_task ON public.task_activities(task_id);

CREATE INDEX idx_inter_requests_target_department ON public.inter_department_requests(target_department_id);
CREATE INDEX idx_inter_requests_requester_department ON public.inter_department_requests(requester_department_id);
CREATE INDEX idx_inter_requests_owner ON public.inter_department_requests(owner_id);
CREATE INDEX idx_inter_requests_status ON public.inter_department_requests(status);
CREATE INDEX idx_inter_requests_due_date ON public.inter_department_requests(due_date);
CREATE INDEX idx_inter_request_messages_request ON public.inter_request_messages(request_id);

CREATE INDEX idx_attachments_task ON public.attachments(task_id);
CREATE INDEX idx_attachments_inter_request ON public.attachments(inter_request_id);

CREATE INDEX idx_notifications_user_read ON public.notifications(user_id, is_read);
CREATE INDEX idx_notifications_created_at ON public.notifications(created_at);

CREATE INDEX idx_audit_logs_actor ON public.audit_logs(actor_id);
CREATE INDEX idx_audit_logs_entity ON public.audit_logs(entity_name, entity_id);
CREATE INDEX idx_audit_logs_created_at ON public.audit_logs(created_at);

CREATE INDEX idx_ai_request_logs_user ON public.ai_request_logs(user_id);
CREATE INDEX idx_ai_request_logs_feature ON public.ai_request_logs(feature_code);
CREATE INDEX idx_ai_request_logs_created_at ON public.ai_request_logs(created_at);
CREATE INDEX idx_embedding_index_entity ON public.embedding_index(entity_type, entity_id);

-- Enable after enough rows exist.
-- CREATE INDEX idx_embedding_index_embedding
-- ON public.embedding_index
-- USING hnsw (embedding extensions.vector_cosine_ops);

-- ---------- 14. Common triggers and helper functions ----------
CREATE OR REPLACE FUNCTION public.set_updated_at()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
  NEW.updated_at = now();
  RETURN NEW;
END;
$$;

CREATE TRIGGER trg_companies_updated_at BEFORE UPDATE ON public.companies
FOR EACH ROW EXECUTE FUNCTION public.set_updated_at();

CREATE TRIGGER trg_departments_updated_at BEFORE UPDATE ON public.departments
FOR EACH ROW EXECUTE FUNCTION public.set_updated_at();

CREATE TRIGGER trg_profiles_updated_at BEFORE UPDATE ON public.profiles
FOR EACH ROW EXECUTE FUNCTION public.set_updated_at();

CREATE TRIGGER trg_projects_updated_at BEFORE UPDATE ON public.projects
FOR EACH ROW EXECUTE FUNCTION public.set_updated_at();

CREATE TRIGGER trg_tasks_updated_at BEFORE UPDATE ON public.tasks
FOR EACH ROW EXECUTE FUNCTION public.set_updated_at();

CREATE TRIGGER trg_subtasks_updated_at BEFORE UPDATE ON public.subtasks
FOR EACH ROW EXECUTE FUNCTION public.set_updated_at();

CREATE TRIGGER trg_task_comments_updated_at BEFORE UPDATE ON public.task_comments
FOR EACH ROW EXECUTE FUNCTION public.set_updated_at();

CREATE TRIGGER trg_inter_department_requests_updated_at BEFORE UPDATE ON public.inter_department_requests
FOR EACH ROW EXECUTE FUNCTION public.set_updated_at();

CREATE TRIGGER trg_notification_preferences_updated_at BEFORE UPDATE ON public.notification_preferences
FOR EACH ROW EXECUTE FUNCTION public.set_updated_at();

CREATE OR REPLACE FUNCTION public.handle_new_user()
RETURNS TRIGGER
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
BEGIN
  INSERT INTO public.profiles (id, email, full_name, avatar_url)
  VALUES (
    NEW.id,
    NEW.email,
    COALESCE(NEW.raw_user_meta_data ->> 'full_name', NEW.raw_user_meta_data ->> 'name'),
    NEW.raw_user_meta_data ->> 'avatar_url'
  )
  ON CONFLICT (id) DO UPDATE SET
    email = COALESCE(EXCLUDED.email, public.profiles.email),
    full_name = COALESCE(public.profiles.full_name, EXCLUDED.full_name),
    avatar_url = COALESCE(public.profiles.avatar_url, EXCLUDED.avatar_url);

  INSERT INTO public.notification_preferences (user_id)
  VALUES (NEW.id)
  ON CONFLICT (user_id) DO NOTHING;

  RETURN NEW;
END;
$$;

CREATE TRIGGER on_auth_user_created
AFTER INSERT ON auth.users
FOR EACH ROW EXECUTE FUNCTION public.handle_new_user();

CREATE OR REPLACE FUNCTION public.has_role(role_codes TEXT[])
RETURNS BOOLEAN
LANGUAGE sql
SECURITY DEFINER
SET search_path = public
STABLE
AS $$
  SELECT EXISTS (
    SELECT 1
    FROM public.user_roles ur
    JOIN public.roles r ON r.id = ur.role_id
    WHERE ur.user_id = auth.uid()
      AND r.code = ANY(role_codes)
  );
$$;

CREATE OR REPLACE FUNCTION public.has_permission(permission_code TEXT)
RETURNS BOOLEAN
LANGUAGE sql
SECURITY DEFINER
SET search_path = public
STABLE
AS $$
  SELECT EXISTS (
    SELECT 1
    FROM public.user_roles ur
    JOIN public.role_permissions rp ON rp.role_id = ur.role_id
    JOIN public.permissions p ON p.id = rp.permission_id
    WHERE ur.user_id = auth.uid()
      AND p.code = permission_code
  );
$$;

CREATE OR REPLACE FUNCTION public.has_department_scope(p_department_id BIGINT)
RETURNS BOOLEAN
LANGUAGE sql
SECURITY DEFINER
SET search_path = public
STABLE
AS $$
  SELECT
    public.has_role(ARRAY['admin', 'director'])
    OR EXISTS (
      SELECT 1
      FROM public.profiles me
      WHERE me.id = auth.uid()
        AND me.department_id = p_department_id
    )
    OR EXISTS (
      SELECT 1
      FROM public.user_department_scopes uds
      WHERE uds.user_id = auth.uid()
        AND uds.department_id = p_department_id
    );
$$;

CREATE OR REPLACE FUNCTION public.can_access_task(p_task_id UUID)
RETURNS BOOLEAN
LANGUAGE sql
SECURITY DEFINER
SET search_path = public
STABLE
AS $$
  SELECT
    public.has_role(ARRAY['admin', 'director'])
    OR EXISTS (
      SELECT 1
      FROM public.tasks t
      WHERE t.id = p_task_id
        AND (
          t.created_by = auth.uid()
          OR t.reporter_id = auth.uid()
          OR EXISTS (
            SELECT 1
            FROM public.task_assignments ta
            WHERE ta.task_id = t.id
              AND ta.user_id = auth.uid()
          )
          OR (
            public.has_role(ARRAY['manager'])
            AND t.department_id IS NOT NULL
            AND public.has_department_scope(t.department_id)
          )
        )
        AND (
          t.is_confidential = FALSE
          OR public.has_role(ARRAY['admin', 'director'])
          OR t.created_by = auth.uid()
          OR t.reporter_id = auth.uid()
          OR EXISTS (
            SELECT 1
            FROM public.task_assignments ta
            WHERE ta.task_id = t.id
              AND ta.user_id = auth.uid()
          )
        )
    );
$$;

CREATE OR REPLACE FUNCTION public.can_access_inter_request(p_request_id UUID)
RETURNS BOOLEAN
LANGUAGE sql
SECURITY DEFINER
SET search_path = public
STABLE
AS $$
  SELECT
    public.has_role(ARRAY['admin', 'director'])
    OR EXISTS (
      SELECT 1
      FROM public.inter_department_requests r
      WHERE r.id = p_request_id
        AND (
          r.requester_user_id = auth.uid()
          OR r.owner_id = auth.uid()
          OR (r.requester_department_id IS NOT NULL AND public.has_department_scope(r.requester_department_id))
          OR (r.target_department_id IS NOT NULL AND public.has_department_scope(r.target_department_id))
        )
    );
$$;

CREATE OR REPLACE FUNCTION public.set_task_defaults()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
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

CREATE TRIGGER trg_tasks_set_defaults
BEFORE INSERT ON public.tasks
FOR EACH ROW EXECUTE FUNCTION public.set_task_defaults();

CREATE OR REPLACE FUNCTION public.validate_task_status_rules()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
DECLARE
  v_status public.task_status_code;
  v_assignee_count INT;
BEGIN
  SELECT code INTO v_status
  FROM public.task_statuses
  WHERE id = NEW.status_id;

  IF v_status = 'assigned' THEN
    IF NEW.task_type IS NULL OR length(trim(NEW.task_type)) = 0 THEN
      RAISE EXCEPTION 'Assigned tasks require task_type';
    END IF;

    IF NEW.priority_id IS NULL THEN
      RAISE EXCEPTION 'Assigned tasks require priority_id';
    END IF;

    IF NEW.due_date IS NULL THEN
      RAISE EXCEPTION 'Assigned tasks require due_date';
    END IF;

    SELECT count(*) INTO v_assignee_count
    FROM public.task_assignments
    WHERE task_id = NEW.id
      AND assignment_type = 'assignee';

    IF TG_OP = 'UPDATE' AND v_assignee_count = 0 THEN
      RAISE EXCEPTION 'Assigned tasks require at least one assignee';
    END IF;
  END IF;

  IF TG_OP = 'UPDATE' THEN
    IF OLD.closed_at IS NOT NULL
      AND NOT public.has_role(ARRAY['admin'])
      AND NOT public.has_permission('task.reopen') THEN
      RAISE EXCEPTION 'Closed tasks cannot be edited without admin or reopen permission';
    END IF;
  END IF;

  RETURN NEW;
END;
$$;

CREATE TRIGGER trg_tasks_validate_status_rules
BEFORE UPDATE ON public.tasks
FOR EACH ROW EXECUTE FUNCTION public.validate_task_status_rules();

CREATE OR REPLACE FUNCTION public.validate_subtask_due_date()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
DECLARE
  v_task_due_date DATE;
BEGIN
  IF NEW.due_date IS NULL THEN
    RETURN NEW;
  END IF;

  SELECT due_date INTO v_task_due_date
  FROM public.tasks
  WHERE id = NEW.task_id;

  IF v_task_due_date IS NOT NULL
    AND NEW.due_date > v_task_due_date
    AND NOT public.has_role(ARRAY['admin', 'manager'])
    AND NOT public.has_permission('task.override_subtask_due_date') THEN
    RAISE EXCEPTION 'Subtask due_date cannot be after parent task due_date';
  END IF;

  RETURN NEW;
END;
$$;

CREATE TRIGGER trg_subtasks_validate_due_date
BEFORE INSERT OR UPDATE ON public.subtasks
FOR EACH ROW EXECUTE FUNCTION public.validate_subtask_due_date();

CREATE OR REPLACE FUNCTION public.audit_task_changes()
RETURNS TRIGGER
LANGUAGE plpgsql
AS $$
BEGIN
  IF TG_OP = 'INSERT' THEN
    INSERT INTO public.audit_logs (actor_id, action, entity_name, entity_id, new_value)
    VALUES (auth.uid(), 'task.created', 'task', NEW.id::text, to_jsonb(NEW));
    RETURN NEW;
  END IF;

  IF TG_OP = 'UPDATE' THEN
    IF OLD.status_id IS DISTINCT FROM NEW.status_id THEN
      INSERT INTO public.audit_logs (actor_id, action, entity_name, entity_id, old_value, new_value)
      VALUES (auth.uid(), 'task.status_changed', 'task', NEW.id::text, jsonb_build_object('status_id', OLD.status_id), jsonb_build_object('status_id', NEW.status_id));
    END IF;

    IF OLD.due_date IS DISTINCT FROM NEW.due_date THEN
      INSERT INTO public.audit_logs (actor_id, action, entity_name, entity_id, old_value, new_value)
      VALUES (auth.uid(), 'task.due_date_changed', 'task', NEW.id::text, jsonb_build_object('due_date', OLD.due_date), jsonb_build_object('due_date', NEW.due_date));
    END IF;

    IF OLD.priority_id IS DISTINCT FROM NEW.priority_id THEN
      INSERT INTO public.audit_logs (actor_id, action, entity_name, entity_id, old_value, new_value)
      VALUES (auth.uid(), 'task.priority_changed', 'task', NEW.id::text, jsonb_build_object('priority_id', OLD.priority_id), jsonb_build_object('priority_id', NEW.priority_id));
    END IF;

    RETURN NEW;
  END IF;

  RETURN NEW;
END;
$$;

CREATE TRIGGER trg_tasks_audit_changes
AFTER INSERT OR UPDATE ON public.tasks
FOR EACH ROW EXECUTE FUNCTION public.audit_task_changes();

-- ---------- 15. RLS ----------
ALTER TABLE public.companies ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.departments ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.profiles ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.roles ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.permissions ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.user_roles ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.role_permissions ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.user_department_scopes ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.projects ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.project_members ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.task_statuses ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.task_priorities ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.tasks ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.task_assignments ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.subtasks ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.task_extension_requests ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.task_comments ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.task_comment_mentions ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.task_activities ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.tags ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.task_tags ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.inter_request_sla_policies ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.inter_department_requests ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.inter_request_messages ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.attachments ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.notifications ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.notification_preferences ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.audit_logs ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.ai_feature_settings ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.ai_request_logs ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.ai_task_insights ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.embedding_index ENABLE ROW LEVEL SECURITY;

CREATE POLICY "Authenticated can read companies" ON public.companies FOR SELECT TO authenticated USING (true);
CREATE POLICY "Admins can manage companies" ON public.companies FOR ALL TO authenticated USING (public.has_role(ARRAY['admin'])) WITH CHECK (public.has_role(ARRAY['admin']));

CREATE POLICY "Authenticated can read departments" ON public.departments FOR SELECT TO authenticated USING (true);
CREATE POLICY "Admins can manage departments" ON public.departments FOR ALL TO authenticated USING (public.has_role(ARRAY['admin'])) WITH CHECK (public.has_role(ARRAY['admin']));

CREATE POLICY "Authenticated can read active profiles" ON public.profiles FOR SELECT TO authenticated USING (is_active = TRUE OR id = auth.uid() OR public.has_role(ARRAY['admin']));
CREATE POLICY "Users can update own basic profile" ON public.profiles FOR UPDATE TO authenticated USING (id = auth.uid()) WITH CHECK (id = auth.uid());
CREATE POLICY "Admins can manage profiles" ON public.profiles FOR ALL TO authenticated USING (public.has_role(ARRAY['admin'])) WITH CHECK (public.has_role(ARRAY['admin']));

CREATE POLICY "Authenticated can read roles" ON public.roles FOR SELECT TO authenticated USING (true);
CREATE POLICY "Admins can manage roles" ON public.roles FOR ALL TO authenticated USING (public.has_role(ARRAY['admin'])) WITH CHECK (public.has_role(ARRAY['admin']));

CREATE POLICY "Authenticated can read permissions" ON public.permissions FOR SELECT TO authenticated USING (true);
CREATE POLICY "Admins can manage permissions" ON public.permissions FOR ALL TO authenticated USING (public.has_role(ARRAY['admin'])) WITH CHECK (public.has_role(ARRAY['admin']));

CREATE POLICY "Admins can manage user roles" ON public.user_roles FOR ALL TO authenticated USING (public.has_role(ARRAY['admin'])) WITH CHECK (public.has_role(ARRAY['admin']));
CREATE POLICY "Users can read own roles" ON public.user_roles FOR SELECT TO authenticated USING (user_id = auth.uid() OR public.has_role(ARRAY['admin']));

CREATE POLICY "Admins can manage role permissions" ON public.role_permissions FOR ALL TO authenticated USING (public.has_role(ARRAY['admin'])) WITH CHECK (public.has_role(ARRAY['admin']));
CREATE POLICY "Authenticated can read role permissions" ON public.role_permissions FOR SELECT TO authenticated USING (true);

CREATE POLICY "Admins can manage department scopes" ON public.user_department_scopes FOR ALL TO authenticated USING (public.has_role(ARRAY['admin'])) WITH CHECK (public.has_role(ARRAY['admin']));
CREATE POLICY "Users can read own department scopes" ON public.user_department_scopes FOR SELECT TO authenticated USING (user_id = auth.uid() OR public.has_role(ARRAY['admin']));

CREATE POLICY "Authenticated can read task statuses" ON public.task_statuses FOR SELECT TO authenticated USING (true);
CREATE POLICY "Admins can manage task statuses" ON public.task_statuses FOR ALL TO authenticated USING (public.has_role(ARRAY['admin'])) WITH CHECK (public.has_role(ARRAY['admin']));

CREATE POLICY "Authenticated can read task priorities" ON public.task_priorities FOR SELECT TO authenticated USING (true);
CREATE POLICY "Admins can manage task priorities" ON public.task_priorities FOR ALL TO authenticated USING (public.has_role(ARRAY['admin'])) WITH CHECK (public.has_role(ARRAY['admin']));

CREATE POLICY "Authenticated can read tags" ON public.tags FOR SELECT TO authenticated USING (true);
CREATE POLICY "Users with task permission can create tags" ON public.tags FOR INSERT TO authenticated WITH CHECK (public.has_permission('task.create'));
CREATE POLICY "Admins can manage tags" ON public.tags FOR ALL TO authenticated USING (public.has_role(ARRAY['admin'])) WITH CHECK (public.has_role(ARRAY['admin']));

CREATE POLICY "Users can read related projects" ON public.projects FOR SELECT TO authenticated
USING (
  public.has_role(ARRAY['admin', 'director'])
  OR owner_id = auth.uid()
  OR created_by = auth.uid()
  OR EXISTS (SELECT 1 FROM public.project_members pm WHERE pm.project_id = id AND pm.user_id = auth.uid())
  OR (department_id IS NOT NULL AND public.has_role(ARRAY['manager']) AND public.has_department_scope(department_id))
);

CREATE POLICY "Users with permission can create projects" ON public.projects FOR INSERT TO authenticated
WITH CHECK (public.has_permission('project.create') OR public.has_role(ARRAY['admin', 'director', 'manager']));

CREATE POLICY "Project owners and managers can update projects" ON public.projects FOR UPDATE TO authenticated
USING (public.has_role(ARRAY['admin', 'director']) OR owner_id = auth.uid() OR created_by = auth.uid())
WITH CHECK (public.has_role(ARRAY['admin', 'director']) OR owner_id = auth.uid() OR created_by = auth.uid());

CREATE POLICY "Users can read related project members" ON public.project_members FOR SELECT TO authenticated
USING (
  user_id = auth.uid()
  OR EXISTS (SELECT 1 FROM public.projects p WHERE p.id = project_id AND (p.owner_id = auth.uid() OR p.created_by = auth.uid()))
  OR public.has_role(ARRAY['admin', 'director', 'manager'])
);

CREATE POLICY "Project owners can manage project members" ON public.project_members FOR ALL TO authenticated
USING (
  public.has_role(ARRAY['admin', 'director'])
  OR EXISTS (SELECT 1 FROM public.projects p WHERE p.id = project_id AND (p.owner_id = auth.uid() OR p.created_by = auth.uid()))
)
WITH CHECK (
  public.has_role(ARRAY['admin', 'director'])
  OR EXISTS (SELECT 1 FROM public.projects p WHERE p.id = project_id AND (p.owner_id = auth.uid() OR p.created_by = auth.uid()))
);

CREATE POLICY "Users can read accessible tasks" ON public.tasks FOR SELECT TO authenticated USING (public.can_access_task(id));

CREATE POLICY "Users with permission can create tasks" ON public.tasks FOR INSERT TO authenticated
WITH CHECK (
  public.has_permission('task.create')
  AND created_by = auth.uid()
  AND (reporter_id IS NULL OR reporter_id = auth.uid() OR public.has_role(ARRAY['admin', 'director', 'manager']))
);

CREATE POLICY "Users can update accessible tasks with permission" ON public.tasks FOR UPDATE TO authenticated
USING (public.can_access_task(id) AND public.has_permission('task.update'))
WITH CHECK (public.can_access_task(id) AND public.has_permission('task.update'));

CREATE POLICY "Admins can delete tasks" ON public.tasks FOR DELETE TO authenticated USING (public.has_role(ARRAY['admin']));

CREATE POLICY "Users can read task assignments for accessible tasks" ON public.task_assignments FOR SELECT TO authenticated USING (public.can_access_task(task_id));

CREATE POLICY "Users can assign themselves on newly created tasks" ON public.task_assignments FOR INSERT TO authenticated
WITH CHECK (
  user_id = auth.uid()
  AND assigned_by = auth.uid()
  AND EXISTS (SELECT 1 FROM public.tasks t WHERE t.id = task_id AND t.created_by = auth.uid())
);

CREATE POLICY "Users with assign permission can manage task assignments" ON public.task_assignments FOR ALL TO authenticated
USING (public.can_access_task(task_id) AND public.has_permission('task.assign'))
WITH CHECK (public.can_access_task(task_id) AND public.has_permission('task.assign'));

CREATE POLICY "Users can read subtasks for accessible tasks" ON public.subtasks FOR SELECT TO authenticated USING (public.can_access_task(task_id));
CREATE POLICY "Users can manage subtasks for accessible tasks" ON public.subtasks FOR ALL TO authenticated
USING (public.can_access_task(task_id) AND public.has_permission('task.update'))
WITH CHECK (public.can_access_task(task_id) AND public.has_permission('task.update'));

CREATE POLICY "Users can read extension requests for accessible tasks" ON public.task_extension_requests FOR SELECT TO authenticated USING (public.can_access_task(task_id));
CREATE POLICY "Users can create own extension requests" ON public.task_extension_requests FOR INSERT TO authenticated
WITH CHECK (public.can_access_task(task_id) AND requested_by_user_id = auth.uid());
CREATE POLICY "Managers can review extension requests" ON public.task_extension_requests FOR UPDATE TO authenticated
USING (public.can_access_task(task_id) AND public.has_role(ARRAY['admin', 'director', 'manager']))
WITH CHECK (public.can_access_task(task_id) AND public.has_role(ARRAY['admin', 'director', 'manager']));

CREATE POLICY "Users can read comments for accessible tasks" ON public.task_comments FOR SELECT TO authenticated USING (public.can_access_task(task_id));
CREATE POLICY "Users can create comments for accessible tasks" ON public.task_comments FOR INSERT TO authenticated
WITH CHECK (public.can_access_task(task_id) AND user_id = auth.uid());
CREATE POLICY "Users can update own comments" ON public.task_comments FOR UPDATE TO authenticated USING (user_id = auth.uid()) WITH CHECK (user_id = auth.uid());

CREATE POLICY "Users can read mentions for accessible task comments" ON public.task_comment_mentions FOR SELECT TO authenticated
USING (EXISTS (SELECT 1 FROM public.task_comments c WHERE c.id = comment_id AND public.can_access_task(c.task_id)));

CREATE POLICY "Comment authors can create mentions" ON public.task_comment_mentions FOR INSERT TO authenticated
WITH CHECK (EXISTS (SELECT 1 FROM public.task_comments c WHERE c.id = comment_id AND c.user_id = auth.uid() AND public.can_access_task(c.task_id)));

CREATE POLICY "Users can read activities for accessible tasks" ON public.task_activities FOR SELECT TO authenticated USING (public.can_access_task(task_id));
CREATE POLICY "Authenticated can insert activities for accessible tasks" ON public.task_activities FOR INSERT TO authenticated WITH CHECK (public.can_access_task(task_id));

CREATE POLICY "Users can read task tags for accessible tasks" ON public.task_tags FOR SELECT TO authenticated USING (public.can_access_task(task_id));
CREATE POLICY "Users can manage task tags for accessible tasks" ON public.task_tags FOR ALL TO authenticated
USING (public.can_access_task(task_id) AND public.has_permission('task.update'))
WITH CHECK (public.can_access_task(task_id) AND public.has_permission('task.update'));

CREATE POLICY "Authenticated can read SLA policies" ON public.inter_request_sla_policies FOR SELECT TO authenticated USING (true);
CREATE POLICY "Admins can manage SLA policies" ON public.inter_request_sla_policies FOR ALL TO authenticated USING (public.has_role(ARRAY['admin'])) WITH CHECK (public.has_role(ARRAY['admin']));

CREATE POLICY "Users can read related inter requests" ON public.inter_department_requests FOR SELECT TO authenticated USING (public.can_access_inter_request(id));
CREATE POLICY "Authenticated can create inter requests" ON public.inter_department_requests FOR INSERT TO authenticated WITH CHECK (requester_user_id = auth.uid());
CREATE POLICY "Related users can update inter requests" ON public.inter_department_requests FOR UPDATE TO authenticated
USING (public.can_access_inter_request(id) AND (public.has_role(ARRAY['admin', 'director', 'manager']) OR requester_user_id = auth.uid() OR owner_id = auth.uid()))
WITH CHECK (public.can_access_inter_request(id) AND (public.has_role(ARRAY['admin', 'director', 'manager']) OR requester_user_id = auth.uid() OR owner_id = auth.uid()));

CREATE POLICY "Users can read inter request messages" ON public.inter_request_messages FOR SELECT TO authenticated USING (public.can_access_inter_request(request_id));
CREATE POLICY "Users can create inter request messages" ON public.inter_request_messages FOR INSERT TO authenticated
WITH CHECK (author_user_id = auth.uid() AND public.can_access_inter_request(request_id));

CREATE POLICY "Users can read accessible attachments" ON public.attachments FOR SELECT TO authenticated
USING (
  (task_id IS NOT NULL AND public.can_access_task(task_id))
  OR (inter_request_id IS NOT NULL AND public.can_access_inter_request(inter_request_id))
  OR public.has_role(ARRAY['admin', 'director'])
);

CREATE POLICY "Users can insert accessible attachments" ON public.attachments FOR INSERT TO authenticated
WITH CHECK (
  uploaded_by = auth.uid()
  AND (
    (task_id IS NOT NULL AND public.can_access_task(task_id))
    OR (inter_request_id IS NOT NULL AND public.can_access_inter_request(inter_request_id))
  )
);

CREATE POLICY "Users can read own notifications" ON public.notifications FOR SELECT TO authenticated USING (user_id = auth.uid());
CREATE POLICY "Users can update own notifications" ON public.notifications FOR UPDATE TO authenticated USING (user_id = auth.uid()) WITH CHECK (user_id = auth.uid());
CREATE POLICY "Authenticated can insert notifications" ON public.notifications FOR INSERT TO authenticated WITH CHECK (true);

CREATE POLICY "Users can read own notification preferences" ON public.notification_preferences FOR SELECT TO authenticated USING (user_id = auth.uid());
CREATE POLICY "Users can update own notification preferences" ON public.notification_preferences FOR UPDATE TO authenticated USING (user_id = auth.uid()) WITH CHECK (user_id = auth.uid());
CREATE POLICY "Users can insert own notification preferences" ON public.notification_preferences FOR INSERT TO authenticated WITH CHECK (user_id = auth.uid());

CREATE POLICY "Admins and managers can read audit logs" ON public.audit_logs FOR SELECT TO authenticated USING (public.has_role(ARRAY['admin', 'director', 'manager']));
CREATE POLICY "Authenticated can insert audit logs" ON public.audit_logs FOR INSERT TO authenticated WITH CHECK (actor_id = auth.uid() OR actor_id IS NULL);

CREATE POLICY "Authenticated can read enabled AI settings" ON public.ai_feature_settings FOR SELECT TO authenticated USING (true);
CREATE POLICY "Admins can manage AI settings" ON public.ai_feature_settings FOR ALL TO authenticated USING (public.has_role(ARRAY['admin'])) WITH CHECK (public.has_role(ARRAY['admin']));

CREATE POLICY "Users can read own AI request logs" ON public.ai_request_logs FOR SELECT TO authenticated USING (user_id = auth.uid() OR public.has_role(ARRAY['admin']));
CREATE POLICY "Users can insert own AI request logs" ON public.ai_request_logs FOR INSERT TO authenticated WITH CHECK (user_id = auth.uid());

CREATE POLICY "Users can read accessible AI task insights" ON public.ai_task_insights FOR SELECT TO authenticated USING (public.can_access_task(task_id));
CREATE POLICY "Users can upsert accessible AI task insights" ON public.ai_task_insights FOR ALL TO authenticated
USING (public.can_access_task(task_id))
WITH CHECK (public.can_access_task(task_id) AND generated_by = auth.uid());

CREATE POLICY "Users can read accessible embeddings" ON public.embedding_index FOR SELECT TO authenticated
USING (
  public.has_role(ARRAY['admin', 'director'])
  OR (entity_type = 'task' AND public.can_access_task(entity_id::uuid))
  OR (entity_type = 'inter_request' AND public.can_access_inter_request(entity_id::uuid))
);

-- ---------- 16. Seed data ----------
INSERT INTO public.roles (code, name, description) VALUES
  ('admin', 'System Admin', 'Quản trị toàn hệ thống'),
  ('director', 'Director', 'Theo dõi toàn công ty hoặc nhiều phòng ban'),
  ('manager', 'Department Manager', 'Quản lý công việc trong phòng ban'),
  ('employee', 'Employee', 'Người xử lý công việc'),
  ('watcher', 'Watcher / Viewer', 'Người theo dõi công việc')
ON CONFLICT (code) DO NOTHING;

INSERT INTO public.permissions (code, name, module) VALUES
  ('dashboard.view.personal', 'Xem dashboard cá nhân', 'Dashboard'),
  ('dashboard.view.department', 'Xem dashboard phòng ban', 'Dashboard'),
  ('project.create', 'Tạo dự án', 'Project'),
  ('task.view', 'Xem công việc', 'Task'),
  ('task.create', 'Tạo công việc', 'Task'),
  ('task.update', 'Cập nhật công việc', 'Task'),
  ('task.assign', 'Giao việc', 'Task'),
  ('task.close', 'Đóng công việc', 'Task'),
  ('task.reopen', 'Mở lại công việc', 'Task'),
  ('task.override_subtask_due_date', 'Cho phép subtask quá hạn task cha', 'Task'),
  ('comment.create', 'Bình luận công việc', 'Task'),
  ('notification.view', 'Xem thông báo', 'Notification'),
  ('audit.view', 'Xem audit log', 'Audit'),
  ('ai.task_draft', 'AI tạo bản nháp task', 'AI'),
  ('ai.summary', 'AI tóm tắt task', 'AI'),
  ('ai.risk', 'AI phân tích rủi ro task', 'AI'),
  ('ai.configure', 'Cấu hình AI', 'AI'),
  ('report.export', 'Xuất báo cáo', 'Report')
ON CONFLICT (code) DO NOTHING;

INSERT INTO public.role_permissions (role_id, permission_id)
SELECT r.id, p.id
FROM public.roles r
CROSS JOIN public.permissions p
WHERE r.code = 'admin'
ON CONFLICT DO NOTHING;

INSERT INTO public.role_permissions (role_id, permission_id)
SELECT r.id, p.id
FROM public.roles r
JOIN public.permissions p ON p.code IN (
  'dashboard.view.personal',
  'dashboard.view.department',
  'project.create',
  'task.view',
  'task.create',
  'task.update',
  'task.assign',
  'task.close',
  'comment.create',
  'notification.view',
  'audit.view',
  'ai.task_draft',
  'ai.summary',
  'ai.risk',
  'report.export'
)
WHERE r.code = 'director'
ON CONFLICT DO NOTHING;

INSERT INTO public.role_permissions (role_id, permission_id)
SELECT r.id, p.id
FROM public.roles r
JOIN public.permissions p ON p.code IN (
  'dashboard.view.personal',
  'dashboard.view.department',
  'project.create',
  'task.view',
  'task.create',
  'task.update',
  'task.assign',
  'task.close',
  'comment.create',
  'notification.view',
  'audit.view',
  'ai.task_draft',
  'ai.summary',
  'ai.risk',
  'report.export'
)
WHERE r.code = 'manager'
ON CONFLICT DO NOTHING;

INSERT INTO public.role_permissions (role_id, permission_id)
SELECT r.id, p.id
FROM public.roles r
JOIN public.permissions p ON p.code IN (
  'dashboard.view.personal',
  'task.view',
  'task.create',
  'task.update',
  'comment.create',
  'notification.view',
  'ai.task_draft',
  'ai.summary',
  'ai.risk'
)
WHERE r.code = 'employee'
ON CONFLICT DO NOTHING;

INSERT INTO public.role_permissions (role_id, permission_id)
SELECT r.id, p.id
FROM public.roles r
JOIN public.permissions p ON p.code IN (
  'dashboard.view.personal',
  'task.view',
  'comment.create',
  'notification.view',
  'ai.summary'
)
WHERE r.code = 'watcher'
ON CONFLICT DO NOTHING;

INSERT INTO public.task_statuses (code, name, sort_order, color, is_closed) VALUES
  ('new', 'Mới tạo', 10, '#6B7280', FALSE),
  ('assigned', 'Đã phân công', 20, '#3B82F6', FALSE),
  ('in_progress', 'Đang xử lý', 30, '#F59E0B', FALSE),
  ('pending_review', 'Chờ xác nhận', 40, '#8B5CF6', FALSE),
  ('completed', 'Hoàn thành xử lý', 50, '#10B981', FALSE),
  ('closed', 'Đã đóng', 60, '#059669', TRUE),
  ('on_hold', 'Tạm dừng', 70, '#64748B', FALSE),
  ('cancelled', 'Đã hủy', 80, '#EF4444', TRUE),
  ('overdue', 'Quá hạn', 90, '#DC2626', FALSE)
ON CONFLICT (code) DO NOTHING;

INSERT INTO public.task_priorities (code, name, level, color) VALUES
  ('low', 'Thấp', 1, '#10B981'),
  ('medium', 'Trung bình', 2, '#F59E0B'),
  ('high', 'Cao', 3, '#EF4444'),
  ('critical', 'Khẩn cấp', 4, '#7C3AED')
ON CONFLICT (code) DO NOTHING;

INSERT INTO public.inter_request_sla_policies (key, label, target_hours, warn_hours) VALUES
  ('procurement', 'Mua sắm', 72, 24),
  ('asset', 'Tài sản', 48, 12),
  ('it-support', 'Hỗ trợ CNTT', 24, 6),
  ('payment', 'Thanh toán / tạm ứng', 72, 24),
  ('recruitment', 'Tuyển dụng', 120, 48),
  ('communication-design', 'Truyền thông / thiết kế', 96, 24),
  ('legal', 'Pháp chế', 120, 48)
ON CONFLICT (key) DO NOTHING;

INSERT INTO public.ai_feature_settings (feature_code, is_enabled, description) VALUES
  ('task_draft', TRUE, 'AI tạo bản nháp công việc'),
  ('task_summary', TRUE, 'AI tóm tắt task'),
  ('task_risk', TRUE, 'AI phân tích rủi ro trễ hạn'),
  ('semantic_search', FALSE, 'Tìm kiếm ngữ nghĩa'),
  ('smart_assignment', FALSE, 'Gợi ý người xử lý'),
  ('auto_classification', FALSE, 'Tự phân loại yêu cầu liên phòng ban')
ON CONFLICT (feature_code) DO NOTHING;

INSERT INTO public.companies (code, name)
VALUES ('DEMO', 'Demo Company')
ON CONFLICT (code) DO NOTHING;

INSERT INTO public.departments (company_id, code, name)
SELECT c.id, 'IT', 'Information Technology'
FROM public.companies c
WHERE c.code = 'DEMO'
ON CONFLICT (company_id, code) DO NOTHING;

INSERT INTO public.departments (company_id, code, name)
SELECT c.id, 'HR', 'Human Resources'
FROM public.companies c
WHERE c.code = 'DEMO'
ON CONFLICT (company_id, code) DO NOTHING;

INSERT INTO public.departments (company_id, code, name)
SELECT c.id, 'ACC', 'Accounting'
FROM public.companies c
WHERE c.code = 'DEMO'
ON CONFLICT (company_id, code) DO NOTHING;

INSERT INTO public.departments (company_id, code, name)
SELECT c.id, 'MKT', 'Marketing'
FROM public.companies c
WHERE c.code = 'DEMO'
ON CONFLICT (company_id, code) DO NOTHING;

-- ============================================================
-- AFTER RUNNING:
-- 1) Create a user in Supabase Auth.
-- 2) public.profiles is auto-created.
-- 3) Assign admin role to the first user:
--
-- INSERT INTO public.user_roles (user_id, role_id)
-- SELECT p.id, r.id
-- FROM public.profiles p
-- JOIN public.roles r ON r.code = 'admin'
-- WHERE p.id = '<AUTH_USER_UUID>';
--
-- Optional: assign department to that user:
--
-- UPDATE public.profiles
-- SET department_id = (SELECT id FROM public.departments WHERE code = 'IT' LIMIT 1)
-- WHERE id = '<AUTH_USER_UUID>';
-- ============================================================
