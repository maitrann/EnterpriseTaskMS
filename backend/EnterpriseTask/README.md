# EnterpriseTask Backend

ASP.NET Core Web API for the Enterprise Task Management System. The backend exposes secured REST endpoints for authentication, tasks, projects, departments, inter-department requests, development database seeding, and database health checks.

## Stack

- ASP.NET Core Web API
- JWT Bearer Authentication and authorization
- Supabase PostgreSQL
- EF Core DbContext configured with Npgsql
- PostgreSQL query/command services for data access
- Swagger/OpenAPI in development

## Configuration

The API reads the database connection string from `ConnectionStrings:DefaultConnection`.
It also supports `ConnectionStrings:Default` for compatibility with older local configs.
Do not commit the real Supabase password to `appsettings.json`.

For local development, use .NET user secrets from the API project:

```powershell
cd backend\EnterpriseTask\EnterpriseTask.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=YOUR_SUPABASE_HOST;Port=5432;Database=postgres;Username=postgres;Password=YOUR_PASSWORD;SSL Mode=Require;Trust Server Certificate=true"
dotnet user-secrets set "Jwt:Secret" "CHANGE_ME_TO_A_LONG_LOCAL_DEV_SECRET_AT_LEAST_32_CHARS"
dotnet user-secrets set "Jwt:Issuer" "EnterpriseTaskMS"
dotnet user-secrets set "Jwt:Audience" "EnterpriseTaskMSUsers"
dotnet user-secrets set "Auth:RefreshTokenDays" "14"
dotnet user-secrets set "Supabase:Url" "https://YOUR_PROJECT_REF.supabase.co"
dotnet user-secrets set "Supabase:AnonKey" "YOUR_SUPABASE_ANON_PUBLIC_KEY"
```

Use the Supabase project URL without `/rest/v1`. The anon key is the public anon API key, not a secret service-role key.

You can also use an environment variable:

```powershell
$env:ConnectionStrings__DefaultConnection="Host=YOUR_SUPABASE_HOST;Port=5432;Database=postgres;Username=postgres;Password=YOUR_PASSWORD;SSL Mode=Require;Trust Server Certificate=true"
```

Check the database connection:

```http
GET /api/health/database
```

Apply forward-only SQL migrations in Development:

```http
POST /api/dev/migrate
```

The migration endpoint is disabled outside Development. It creates `public.schema_migrations`, applies embedded SQL migrations once, and records the initial migration as a baseline when an existing EnterpriseTask schema is detected. The old `supabase_schema_v2_clean.sql` script is a destructive reset script for disposable local/demo databases only.

`POST /api/dev/seed` is intentionally a no-op. Create users in Supabase Auth, ensure each `auth.users.id` has a matching `public.profiles.id`, then assign roles in `public.user_roles`; the backend does not create default passwords.

See `docs/database/README.md` for clean setup, Supabase Auth profile setup, existing database baseline, and recovery notes.

## Run

```powershell
dotnet run --project EnterpriseTask.Api --launch-profile https
```

In development, Swagger UI is available at `/swagger`.

## API Summary

| Area | Endpoints |
| --- | --- |
| Auth | `POST /api/auth/login`, `GET /api/auth/me` |
| Tasks | `GET /api/tasks`, `POST /api/tasks`, `PUT /api/tasks/{id}`, status update, assignee transfer, comments, subtasks, duplication, extension review |
| Projects | project listing for frontend options and overview pages |
| Departments | department listing and scoped department options |
| Inter-department requests | request list, status updates, close confirmation, request messages |
| Development | local database seed endpoint |
| Health | `GET /api/health/database` |

## Portfolio Wording

Recommended wording:

> Configured EF Core DbContext with Npgsql and implemented PostgreSQL query/command services for backend data access and processing.

Avoid saying the project uses EF Core entity modeling unless entity classes and explicit model mappings are added later.
