CREATE TABLE IF NOT EXISTS public.auth_refresh_sessions (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id UUID NOT NULL REFERENCES public.profiles(id) ON DELETE CASCADE,
  token_hash TEXT NOT NULL UNIQUE,
  family_id UUID NOT NULL,
  expires_at TIMESTAMPTZ NOT NULL,
  revoked_at TIMESTAMPTZ,
  replaced_by_session_id UUID REFERENCES public.auth_refresh_sessions(id) ON DELETE SET NULL,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  last_used_at TIMESTAMPTZ
);

CREATE INDEX IF NOT EXISTS idx_auth_refresh_sessions_user
ON public.auth_refresh_sessions(user_id);

CREATE INDEX IF NOT EXISTS idx_auth_refresh_sessions_family
ON public.auth_refresh_sessions(family_id);

CREATE INDEX IF NOT EXISTS idx_auth_refresh_sessions_active
ON public.auth_refresh_sessions(user_id, expires_at)
WHERE revoked_at IS NULL;
