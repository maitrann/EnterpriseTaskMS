# EnterpriseTask Backend

## Supabase PostgreSQL

The API reads the database connection string from `ConnectionStrings:DefaultConnection`.
It also supports `ConnectionStrings:Default` for compatibility with older local configs.
Do not commit the real Supabase password to `appsettings.json`.

For local development, use .NET user secrets from the API project:

```powershell
cd backend\EnterpriseTask\EnterpriseTask.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=YOUR_SUPABASE_HOST;Port=5432;Database=postgres;Username=postgres;Password=YOUR_PASSWORD;SSL Mode=Require;Trust Server Certificate=true"
dotnet user-secrets set "Jwt:Secret" "CHANGE_ME_TO_A_LONG_LOCAL_DEV_SECRET"
dotnet user-secrets set "DevelopmentSeed:AdminPassword" "CHANGE_ME_ADMIN_PASSWORD"
dotnet user-secrets set "DevelopmentSeed:UserPassword" "CHANGE_ME_USER_PASSWORD"
```

You can also use an environment variable:

```powershell
$env:ConnectionStrings__DefaultConnection="Host=YOUR_SUPABASE_HOST;Port=5432;Database=postgres;Username=postgres;Password=YOUR_PASSWORD;SSL Mode=Require;Trust Server Certificate=true"
```

Check the database connection:

```http
GET /api/health/database
```
