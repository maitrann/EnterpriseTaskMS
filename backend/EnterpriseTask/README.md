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
dotnet user-secrets set "Jwt:Secret" "CHANGE_ME_TO_A_LONG_LOCAL_DEV_SECRET"
dotnet user-secrets set "Jwt:Issuer" "EnterpriseTask"
dotnet user-secrets set "Jwt:Audience" "EnterpriseTaskFrontend"
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

## Run

```powershell
dotnet run --project EnterpriseTask.Api
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
