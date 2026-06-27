# 01 - Repository Inventory

## 1. Executive summary

Repository is a full-stack Enterprise Task Management System with:

- ASP.NET Core Web API backend under `backend/EnterpriseTask`.
- Angular frontend under `enterprise-task-ms`.
- PostgreSQL/Supabase schema in `supabase_schema_v2_clean.sql`.
- Audit prompt pack under `docs/enterprise_task_codex_audit_prompts`.

The implemented architecture is layered:

- API layer: `backend/EnterpriseTask/EnterpriseTask.Api`.
- Application layer: `backend/EnterpriseTask/EnterpriseTask.Application`.
- Domain layer: `backend/EnterpriseTask/EnterpriseTask.Domain`.
- Infrastructure layer: `backend/EnterpriseTask/EnterpriseTask.Infrastructure`.
- Frontend: standalone Angular app using routes, guards, interceptors, services, signals, mock data sources, and a small NgRx reducer registration.

No detailed feature verdict is made in this prompt. This report only maps the repository and identifies technology, startup flow, config, build/test state, and unusual points.

## 2. Technology matrix

| Area | Observed technology | Evidence |
|---|---|---|
| Backend framework | ASP.NET Core Web API on `net10.0` | `backend/EnterpriseTask/EnterpriseTask.Api/EnterpriseTask.Api.csproj` |
| Backend API style | MVC controllers | `backend/EnterpriseTask/EnterpriseTask.Api/Program.cs` - `builder.Services.AddControllers()`, `app.MapControllers()` |
| OpenAPI/Swagger | Microsoft.AspNetCore.OpenApi + Swashbuckle | `backend/EnterpriseTask/EnterpriseTask.Api/EnterpriseTask.Api.csproj`; `backend/EnterpriseTask/EnterpriseTask.Api/Program.cs` |
| Frontend framework | Angular 21.x | `enterprise-task-ms/package.json` |
| Frontend state | Angular signals/services; NgRx registered for task reducer | `enterprise-task-ms/src/app/core/services/task.service.ts`; `enterprise-task-ms/src/app/app.config.ts`; `enterprise-task-ms/src/app/features/task/store/task.reducer.ts` |
| Frontend routing | Angular Router standalone lazy routes | `enterprise-task-ms/src/app/app.routes.ts` |
| HTTP client | Angular `HttpClient` with auth interceptor | `enterprise-task-ms/src/app/app.config.ts`; `enterprise-task-ms/src/app/core/interceptors/auth.interceptor.ts` |
| Database | PostgreSQL/Supabase SQL schema | `supabase_schema_v2_clean.sql`; `README.md`; `backend/EnterpriseTask/README.md` |
| ORM/data access | EF Core DbContext for connection + raw PostgreSQL query/command services | `backend/EnterpriseTask/EnterpriseTask.Infrastructure/Persistence/ApplicationDbContext.cs`; `PostgresQueryBase.cs`; `PostgresCommandBase.cs` |
| AuthN/AuthZ | JWT Bearer auth + controller `[Authorize]` + role/permission SQL checks | `backend/EnterpriseTask/EnterpriseTask.Api/Program.cs`; `backend/EnterpriseTask/EnterpriseTask.Infrastructure/Auth/JwtAuthService.cs`; controllers |
| Realtime | Not found in runtime code | No `SignalR`, `Hub`, WebSocket registration found by repository search |
| Background jobs | Not found in runtime code | No `BackgroundService`, `IHostedService`, Hangfire, Quartz found by repository search |
| Cache | Not found in runtime code | No `IMemoryCache`/`IDistributedCache` usage found by repository search |
| File storage | Database schema includes attachments table; no storage provider found | `supabase_schema_v2_clean.sql`; no S3/Azure/local storage abstraction found |
| Email | Not found in runtime code | Repository search found no email service/provider implementation |
| AI/vector/search | Database schema includes `embedding_index` and pgvector extension; no AI provider/runtime pipeline found | `supabase_schema_v2_clean.sql` |
| Testing | Angular/Vitest-style `ng test`; no backend test project found | `enterprise-task-ms/package.json`; `enterprise-task-ms/src/app/app.spec.ts`; no `.csproj` test project in backend file list |
| Docker/CI/CD | Not found | No Dockerfile/docker-compose/CI workflow found in file list |

## 3. Repository/module map

```text
.
├─ README.md
├─ supabase_schema_v2_clean.sql
├─ backend/
│  └─ EnterpriseTask/
│     ├─ EnterpriseTask.slnx
│     ├─ README.md
│     ├─ EnterpriseTask.Api/
│     │  ├─ Program.cs
│     │  ├─ appsettings*.json
│     │  ├─ Controllers/
│     │  └─ Auth/
│     ├─ EnterpriseTask.Application/
│     │  ├─ Auth/
│     │  ├─ Common/
│     │  ├─ Departments/
│     │  ├─ Development/
│     │  ├─ InterDepartmentRequests/
│     │  ├─ Projects/
│     │  └─ Tasks/
│     ├─ EnterpriseTask.Domain/
│     │  └─ Tasks/
│     └─ EnterpriseTask.Infrastructure/
│        ├─ Auth/
│        ├─ Departments/
│        ├─ Development/
│        ├─ InterDepartmentRequests/
│        ├─ Persistence/
│        ├─ Projects/
│        └─ Tasks/
├─ enterprise-task-ms/
│  ├─ package.json
│  ├─ angular.json
│  ├─ tsconfig*.json
│  └─ src/app/
│     ├─ app.config.ts
│     ├─ app.routes.ts
│     ├─ core/
│     ├─ features/
│     ├─ layout/
│     └─ shared/
└─ docs/
   ├─ screenshots/
   └─ enterprise_task_codex_audit_prompts/
```

Ignored during inventory: `.git`, `bin`, `obj`, `node_modules`, `dist`.

No `AGENTS.md` or `CONTRIBUTING.md` was found in the repository file list.

## 4. Backend startup and dependency flow

Startup entrypoint:

- `backend/EnterpriseTask/EnterpriseTask.Api/Program.cs`

Observed startup pipeline:

- Registers MVC controllers: `builder.Services.AddControllers()`.
- Registers current user context: `ICurrentUserContext` -> `HttpCurrentUserContext`.
- Registers infrastructure dependencies via `builder.Services.AddInfrastructure(...)`.
- Requires `Jwt:Secret`, `Jwt:Issuer`, and `Jwt:Audience`; enforces JWT secret length.
- Configures JWT Bearer authentication.
- Configures CORS policy named `Frontend` for `http://localhost:4200` and `https://localhost:4200`.
- Enables Swagger/OpenAPI in development.
- Middleware order: HTTPS redirection, CORS, authentication, authorization, controllers.

Project dependency flow:

- `EnterpriseTask.Api` references `EnterpriseTask.Infrastructure`.
- `EnterpriseTask.Infrastructure` references `EnterpriseTask.Application` and `EnterpriseTask.Domain`.
- `EnterpriseTask.Application` references `EnterpriseTask.Domain`.
- `EnterpriseTask.Domain` has no project references.

DI registrations in `backend/EnterpriseTask/EnterpriseTask.Infrastructure/DependencyInjection.cs` include:

- Task query/command/policy/access services: `ITaskQueries`, `ITaskCommands`, `ITaskAccessReader`, `ITaskPolicyQueries`, `CreateTaskHandler`, `UpdateTaskStatusHandler`.
- Inter-department request query/command/policy services and `AssignInterRequestOwnerHandler`.
- Project and department query services.
- Auth service: `IAuthService` -> `JwtAuthService`.
- Database health and seeding services.
- EF Core `ApplicationDbContext` configured with Npgsql.

Backend controllers found:

- `AuthController.cs`: `POST /api/auth/login`, `GET /api/auth/me`.
- `TasksController.cs`: task list, activities, form-options, create/update/status/assignee/duplicate/comments/extension/subtasks.
- `ProjectsController.cs`: project list.
- `DepartmentsController.cs`: department cards.
- `InterDepartmentRequestsController.cs`: request list/options/workflow/message endpoints.
- `DatabaseHealthController.cs`: database health.
- `DevelopmentController.cs`: seed endpoint.

## 5. Frontend startup, routing and data flow

Startup entrypoint:

- `enterprise-task-ms/src/main.ts` bootstraps `App` with `appConfig`.

Application config:

- `enterprise-task-ms/src/app/app.config.ts`
- Registers router, `HttpClient` with `authInterceptor`, NgRx store, task reducer state, and mock data-source providers.

Routes:

- `login`
- `forgot-password`
- Auth-guarded shell using `MainLayoutComponent`
- Child routes: `dashboard`, `tasks`, `projects`, `departments`, `inter-department-requests`

Frontend data flow:

- API base URL is a constant in `enterprise-task-ms/src/app/core/constants/app.constants.ts`.
- Auth token is stored in local storage and attached by `enterprise-task-ms/src/app/core/interceptors/auth.interceptor.ts`.
- Task API calls are centralized in `enterprise-task-ms/src/app/core/services/task-api.client.ts`.
- Task state is stored in `enterprise-task-ms/src/app/core/services/task-state.store.ts`.
- Main task facade/workflow logic is in `enterprise-task-ms/src/app/core/services/task.service.ts`.
- Project, department, auth, and inter-department request services call backend APIs directly.
- Mock data sources remain registered in `app.config.ts` and are used as offline fallback/initial state.

UI modules/features:

- Auth: `features/auth`.
- Dashboard: `features/dashboard`.
- Task board and task detail components: `features/task/components`.
- Project page: `features/project`.
- Department page: `features/department`.
- Inter-department request page: `features/inter-department-request`.

## 6. Database and migration mechanism

Database assets:

- `supabase_schema_v2_clean.sql` is the main schema artifact.
- It includes PostgreSQL objects such as task/request/auth-related tables and `embedding_index`.
- It enables pgvector via `CREATE EXTENSION IF NOT EXISTS vector WITH SCHEMA extensions`.

EF Core:

- `ApplicationDbContext.cs` only sets default schema to `public`.
- `ApplicationDbContextFactory.cs` reads `ConnectionStrings__DefaultConnection` or `ConnectionStrings__Default`.
- No EF Core migration files were found in the repository file list.

Data access:

- Query/command services use raw SQL through `PostgresQueryBase.cs` and `PostgresCommandBase.cs`.
- `ApplicationDbContext` is used mainly to provide Npgsql connection management.

Configuration:

- Backend reads connection strings from `ConnectionStrings:DefaultConnection` or `ConnectionStrings:Default`.
- User secrets are documented in `backend/EnterpriseTask/README.md`.
- `appsettings.json` and `appsettings.Development.json` contain keys with empty secret-like values; no secret values are reported here.

## 7. External integrations

Observed:

- PostgreSQL/Supabase database integration is documented and represented by SQL schema and Npgsql configuration.
- JWT Bearer authentication is configured in backend startup.
- Supabase auth/profile password flow has code evidence in `JwtAuthService.cs`.
- Swagger/OpenAPI is configured for API exploration.

Not found as active runtime integrations:

- SignalR/realtime hub.
- Background job framework or hosted job.
- Cache provider.
- Email provider.
- File storage provider.
- AI/LLM provider or embedding generation pipeline.
- Docker or CI/CD pipeline files.

Database-only indicators:

- `supabase_schema_v2_clean.sql` includes `embedding_index` and pgvector setup, but prompt 01 does not validate AI/search implementation.

## 8. Build/test result

Commands executed:

```powershell
dotnet build backend\EnterpriseTask\EnterpriseTask.slnx
```

Result:

- Success.
- `0 Warning(s)`, `0 Error(s)`.

```powershell
npm.cmd run build
```

Working directory:

```text
enterprise-task-ms
```

Result:

- Success.
- Angular application bundle generated under `enterprise-task-ms/dist/enterprise-task-ms`.

```powershell
npm.cmd test -- --watch=false
```

Working directory:

```text
enterprise-task-ms
```

Result:

- Failed.
- `src/app/app.spec.ts` has 2 tests: 1 passed, 1 failed.
- Failure: `should render title` expects an `h1` text containing the default app title, but `compiled.querySelector('h1')?.textContent` is `undefined`.

## 9. Unknowns and blockers

- No backend automated test project was found, so backend behavior is build-verified only in this prompt.
- No EF migration folder was found; schema appears SQL-script based.
- Mock data sources are still registered in frontend app config; later prompts must distinguish API-connected flows from mock fallback.
- `docs/enterprise_task_codex_audit_prompts` appears untracked in `git status`; this does not affect source audit but should be noted before committing reports.
- `backend/EnterpriseTask/.cr/` appears untracked; it was not inspected in detail for this prompt.
- Some documentation text says `GET /api/departments`, while actual controller evidence shows `GET /api/departments/cards`.
- Prompt files are displayed with mojibake in terminal output, but filenames and instructions are readable enough to execute.

## 10. Important files for later prompts

Backend:

- `backend/EnterpriseTask/EnterpriseTask.Api/Program.cs`
- `backend/EnterpriseTask/EnterpriseTask.Api/Controllers/AuthController.cs`
- `backend/EnterpriseTask/EnterpriseTask.Api/Controllers/TasksController.cs`
- `backend/EnterpriseTask/EnterpriseTask.Api/Controllers/InterDepartmentRequestsController.cs`
- `backend/EnterpriseTask/EnterpriseTask.Api/Auth/HttpCurrentUserContext.cs`
- `backend/EnterpriseTask/EnterpriseTask.Application/Auth/IAuthService.cs`
- `backend/EnterpriseTask/EnterpriseTask.Application/Auth/AuthDtos.cs`
- `backend/EnterpriseTask/EnterpriseTask.Application/Common/UserScope.cs`
- `backend/EnterpriseTask/EnterpriseTask.Application/Tasks/*`
- `backend/EnterpriseTask/EnterpriseTask.Application/InterDepartmentRequests/*`
- `backend/EnterpriseTask/EnterpriseTask.Domain/Tasks/*`
- `backend/EnterpriseTask/EnterpriseTask.Infrastructure/DependencyInjection.cs`
- `backend/EnterpriseTask/EnterpriseTask.Infrastructure/Auth/JwtAuthService.cs`
- `backend/EnterpriseTask/EnterpriseTask.Infrastructure/Tasks/*`
- `backend/EnterpriseTask/EnterpriseTask.Infrastructure/InterDepartmentRequests/*`
- `backend/EnterpriseTask/EnterpriseTask.Infrastructure/Persistence/*`

Frontend:

- `enterprise-task-ms/src/app/app.config.ts`
- `enterprise-task-ms/src/app/app.routes.ts`
- `enterprise-task-ms/src/app/core/constants/app.constants.ts`
- `enterprise-task-ms/src/app/core/interceptors/auth.interceptor.ts`
- `enterprise-task-ms/src/app/core/guards/auth.guard.ts`
- `enterprise-task-ms/src/app/core/guards/role.guard.ts`
- `enterprise-task-ms/src/app/core/services/auth.service.ts`
- `enterprise-task-ms/src/app/core/services/task-api.client.ts`
- `enterprise-task-ms/src/app/core/services/task-state.store.ts`
- `enterprise-task-ms/src/app/core/services/task.service.ts`
- `enterprise-task-ms/src/app/core/services/inter-department-request.service.ts`
- `enterprise-task-ms/src/app/core/services/project.service.ts`
- `enterprise-task-ms/src/app/core/services/department.service.ts`
- `enterprise-task-ms/src/app/features/task/components/*`
- `enterprise-task-ms/src/app/features/dashboard/dashboard.component.ts`
- `enterprise-task-ms/src/app/features/inter-department-request/inter-department-request.component.ts`

Database/config/test:

- `supabase_schema_v2_clean.sql`
- `backend/EnterpriseTask/EnterpriseTask.Api/appsettings.json`
- `backend/EnterpriseTask/EnterpriseTask.Api/appsettings.Development.json`
- `backend/EnterpriseTask/EnterpriseTask.Api/Properties/launchSettings.json`
- `enterprise-task-ms/package.json`
- `enterprise-task-ms/angular.json`
- `enterprise-task-ms/src/app/app.spec.ts`
