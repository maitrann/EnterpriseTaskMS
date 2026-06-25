# Database Setup and Migration Lifecycle

The current backend uses an EF Core `DbContext` for the PostgreSQL connection and raw SQL query/command services for application data access. Schema changes are managed as forward-only SQL migrations embedded in `EnterpriseTask.Infrastructure`.

## Local Configuration

Do not commit real connection strings, JWT secrets, Supabase keys, passwords, or access tokens.

Use .NET user secrets from the API project:

```powershell
cd backend\EnterpriseTask\EnterpriseTask.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=YOUR_SUPABASE_HOST;Port=5432;Database=postgres;Username=postgres;Password=YOUR_PASSWORD;SSL Mode=Require;Trust Server Certificate=true"
dotnet user-secrets set "Jwt:Secret" "CHANGE_ME_TO_A_LOCAL_SECRET_WITH_AT_LEAST_32_UTF8_BYTES"
dotnet user-secrets set "Jwt:Issuer" "EnterpriseTaskMS"
dotnet user-secrets set "Jwt:Audience" "EnterpriseTaskMSUsers"
```

`backend/EnterpriseTask/EnterpriseTask.Api/appsettings.Local.example.json` is a sanitized reference only. Prefer user secrets or environment variables for real values.

## Applying Migrations in Development

Start the API in Development, then call:

```http
POST /api/dev/migrate
```

The endpoint is hidden outside Development. It:

- Creates `public.schema_migrations` if needed.
- Applies embedded `Persistence/Migrations/*.sql` files in version order.
- Records each migration once.
- Records `0001_initial_schema.sql` as a baseline without running it when an existing EnterpriseTask schema is detected.

Check status with:

```http
GET /api/health/database
```

The health response distinguishes:

- `Misconfigured`: missing database configuration.
- `Unhealthy`: configured but cannot connect.
- `Healthy`: connected and able to read migration status.

No secret or connection string value is returned.

## Existing Database Baseline

If the database already has the current EnterpriseTask schema, do not run the old reset script. Call `POST /api/dev/migrate` once. The migrator detects `public.tasks` and records the initial migration as a baseline.

This prevents data loss while giving future migrations a versioned starting point.

## Clean Database Setup

For a brand-new empty database:

1. Configure user secrets or environment variables.
2. Start the API in Development.
3. Call `POST /api/dev/migrate`.
4. Create users in Supabase Auth.
5. Assign roles in `public.user_roles`.

`POST /api/dev/seed` is intentionally a no-op. It does not create default users or passwords.

## Recovery Notes

Migrations are forward-only. If a migration fails:

1. Stop deployment.
2. Inspect `public.schema_migrations` to confirm whether the failed version was recorded.
3. Restore from database backup if the migration partially changed data before failure.
4. Fix the migration in a new forward migration when possible.
5. Do not edit already-applied migration files in shared environments.

## Legacy Reset Script

`supabase_schema_v2_clean.sql` is still useful as a local reference and for disposable demo databases only. It contains destructive `DROP ... CASCADE` statements and must not be used as an incremental migration path.
