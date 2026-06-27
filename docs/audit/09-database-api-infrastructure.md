# Prompt 09 - Database, API Contract, and Infrastructure Audit

## Scope

- Source reviewed only; no application code changed.
- Main evidence: `supabase_schema_v2_clean.sql`, ASP.NET Core controllers, application DTOs/handlers, infrastructure DI, Angular API clients/services, config files, and build/test commands.
- Status values used: `IMPLEMENTED`, `PARTIAL`, `MISSING`, `CANNOT_VERIFY`, `NOT_APPLICABLE`.

## Entity / Schema Matrix

| Logical entity | Database schema | Backend/API usage | Status | Evidence |
| --- | --- | --- | --- | --- |
| Company | `companies` table, PK, unique `code`, timestamps, update trigger | No controller/service found | PARTIAL | `supabase_schema_v2_clean.sql`; no `CompaniesController`/query service |
| Department | `departments`, FK to company/parent, unique `(company_id, name/code)`, timestamps/indexes | `DepartmentsController.GetCards()` via `PostgresDepartmentQueries` | PARTIAL | `supabase_schema_v2_clean.sql`, `backend/.../DepartmentsController.cs` |
| User/Profile | `profiles`, PK references `auth.users(id)`, unique email/employee_code, department/manager FKs, timestamps | Auth login/me loads profile; no `/api/users` management | PARTIAL | `supabase_schema_v2_clean.sql`, `JwtAuthService.cs`, `AuthController.cs` |
| Role | `roles`, unique code, seed data | Used for auth/scope/policy queries; no role management API | PARTIAL | `supabase_schema_v2_clean.sql`, `PostgresTaskAccessReader.cs`, `ControllerScopeExtensions.cs` |
| Permission | `permissions`, unique code, seed data | Used by task/inter-request policy checks; no permission management API | PARTIAL | `supabase_schema_v2_clean.sql`, `PostgresTaskAccessReader.cs` |
| UserRole | `user_roles`, composite PK, FK cascade | Used by auth/token role loading and policy checks | PARTIAL | `supabase_schema_v2_clean.sql`, `JwtAuthService.cs` |
| RolePermission | `role_permissions`, composite PK | Used by task permission checks | PARTIAL | `supabase_schema_v2_clean.sql`, `PostgresTaskAccessReader.cs` |
| UserDepartmentScope | `user_department_scopes`, composite PK | Used by task/inter-request visibility | PARTIAL | `supabase_schema_v2_clean.sql`, `PostgresTaskQueries.cs`, `PostgresInterDepartmentRequestQueries.cs` |
| Project | `projects`, UUID PK, FK department/owner/created_by, date check, indexes | `GET /api/projects` only | PARTIAL | `supabase_schema_v2_clean.sql`, `ProjectsController.cs`, `PostgresProjectQueries.cs` |
| ProjectMember | `project_members`, composite PK | Query support minimal; no management API | PARTIAL | `supabase_schema_v2_clean.sql` |
| TaskStatus / TaskPriority | lookup tables and enum types, seed data | Read through form options and workflow policy uses numeric ids | PARTIAL | `supabase_schema_v2_clean.sql`, `TaskStatusIds.cs`, `PostgresTaskQueries.cs` |
| Task | `tasks`, UUID PK, unique code, FKs, progress/date/self-parent checks, timestamps, indexes | REST list/create/update/status/duplicate | IMPLEMENTED | `supabase_schema_v2_clean.sql`, `TasksController.cs`, `PostgresTaskQueries.cs`, `PostgresTaskCommands.cs` |
| TaskAssignment | `task_assignments`, composite PK `(task_id,user_id,assignment_type)`, indexes | Used for assignee/co-assignee/watcher and transfer | IMPLEMENTED | `supabase_schema_v2_clean.sql`, `PostgresTaskCommands.cs` |
| SubTask | `subtasks`, UUID PK, FK task/assignee, progress check, timestamps | REST create/update/delete | IMPLEMENTED | `supabase_schema_v2_clean.sql`, `TasksController.cs` |
| Comment | `task_comments`, UUID PK, FK task/user, timestamps | `POST /api/tasks/{id}/comments`; comments are also returned as `processing_notes` | PARTIAL | `supabase_schema_v2_clean.sql`, `TasksController.cs`, `PostgresTaskQueries.cs` |
| CommentMention | `task_comment_mentions`, composite PK | No backend command/API or FE mention UI found | MISSING | `supabase_schema_v2_clean.sql` |
| TaskActivity | `task_activities`, UUID PK, FK task/user, created_at | `GET /api/tasks/activities`; local FE also records optimistic activities | PARTIAL | `supabase_schema_v2_clean.sql`, `TasksController.cs`, `TaskService` |
| Tag / TaskTag | `tags`, `task_tags`, unique tag name, composite PK | Task create/update stores tags | IMPLEMENTED | `supabase_schema_v2_clean.sql`, `PostgresTaskCommands.cs` |
| Inter-department Request/SLA | `inter_request_sla_policies`, `inter_department_requests`, `inter_request_messages`, enums/indexes | REST list/create/status/assign/message/close/options | IMPLEMENTED | `supabase_schema_v2_clean.sql`, `InterDepartmentRequestsController.cs` |
| Attachment | `attachments`, owner check exactly one task/request, storage metadata, indexes | No `/api/tasks/{id}/attachments`; FE only uses attachment name tokens | MISSING | `supabase_schema_v2_clean.sql`, `TasksController.cs`, FE task modals |
| Notification | `notifications`, `notification_preferences`, indexes/RLS | No `/api/notifications`; FE service empty/header badge hard-coded | MISSING | `supabase_schema_v2_clean.sql`, `notification.service.ts`, `HeaderComponent` |
| AuditLog | `audit_logs`, indexes/RLS; trigger audits task insert/update/delete | No audit API/report screen | PARTIAL | `supabase_schema_v2_clean.sql` |
| AIRequestLog | `ai_request_logs`, `ai_feature_settings`, `ai_task_insights` | No AI controller/provider/service | MISSING | `supabase_schema_v2_clean.sql`, backend controller list |
| EmbeddingIndex | `embedding_index`, vector column, unique key; HNSW index commented out | No search/vector provider/API | PARTIAL | `supabase_schema_v2_clean.sql` |

## Missing Migration / Constraint / Index List

| Area | Finding | Impact | Evidence |
| --- | --- | --- | --- |
| Migration framework | No EF migrations found; schema is a single SQL reset script with `DROP TABLE IF EXISTS ... CASCADE` | Cannot apply safely as incremental production migration; destructive if run on populated DB | `supabase_schema_v2_clean.sql`, `ApplicationDbContext.cs` |
| EF model mapping | `ApplicationDbContext` only sets default schema and has no entity mappings | EF migrations/model validation cannot represent schema; raw SQL must stay manually aligned | `backend/.../ApplicationDbContext.cs` |
| Seed endpoint | `POST /api/dev/seed` exists but `DatabaseSeeder.SeedAsync()` throws `NotSupportedException` | Development seed endpoint is effectively unusable | `DevelopmentController.cs`, `DatabaseSeeder.cs` |
| Soft delete | No `deleted_at` found in schema tables reviewed | Delete semantics are hard delete/cascade/restrict only; recovery/audit retention may be incomplete | `supabase_schema_v2_clean.sql` |
| Concurrency token | No row version/xmin/concurrency token found | Lost update protection is missing for task/request edits | `supabase_schema_v2_clean.sql`, command update SQL |
| Vector index | `embedding_index.embedding` exists, but HNSW vector index is commented out | Semantic search would not be performant at scale | `supabase_schema_v2_clean.sql` |
| Task feed ordering | Queries order by `tasks.created_at DESC, id DESC`; no explicit `idx_tasks_created_at` | Full task list may sort without matching index | `PostgresTaskQueries.cs`, `supabase_schema_v2_clean.sql` |
| Activity ordering | Activities query orders by `created_at DESC, id DESC`; only `idx_task_activities_task` exists | Global activity feed may need `created_at`/compound index | `PostgresTaskQueries.cs`, `supabase_schema_v2_clean.sql` |
| Assignment lookup key order | Query checks `ta.task_id = t.id AND ta.user_id = @actorUserId`; schema has separate indexes on user/task/type plus PK `(task_id,user_id,assignment_type)` | Per-task lookup can use PK prefix; user-first lookup may use `idx_task_assignments_user`, but no compound `(user_id, task_id)` | `PostgresTaskQueries.cs`, `supabase_schema_v2_clean.sql` |
| Notifications feed | Has `(user_id,is_read)` and `created_at` separately, but no `(user_id,is_read,created_at DESC)` | Future unread notification list may need compound index for user unread feed | `supabase_schema_v2_clean.sql` |
| Audit feed | Has actor/entity/created indexes separately | Common filtered audit queries may need compound indexes depending on UI/report filters | `supabase_schema_v2_clean.sql` |

## API Endpoint Map FE -> BE -> Handler

| SRS/API reference | FE caller | BE endpoint | Handler/query/command | Status |
| --- | --- | --- | --- | --- |
| `/api/auth/login` | `AuthService.login()` | `POST /api/auth/login` | `JwtAuthService.LoginAsync()` | IMPLEMENTED |
| `/api/auth/me` | Not called on app startup | `GET /api/auth/me` | `JwtAuthService.GetUserAsync()` | PARTIAL |
| `/api/users` | None | None | None | MISSING |
| `/api/roles` | None | None | None | MISSING |
| `/api/roles/{id}/permissions` | None | None | None | MISSING |
| `/api/tasks` list | `TaskApiClient.loadSnapshot()` | `GET /api/tasks` | `PostgresTaskQueries.GetTasksAsync()` | IMPLEMENTED |
| `/api/tasks` create | `TaskService.createTask()` -> `TaskApiClient.createTask()` | `POST /api/tasks` | `CreateTaskHandler` + `PostgresTaskCommands.CreateAsync()` | IMPLEMENTED |
| `/api/tasks/{id}` update | `TaskApiClient.updateTask()` exists, but main edit flow currently updates local state in `TaskService.updateTask()` | `PUT /api/tasks/{id:guid}` | `PostgresTaskCommands.UpdateAsync()` | PARTIAL |
| `/api/tasks/{id}/status` | Status helpers/actions | `POST /api/tasks/{id:guid}/status` | `UpdateTaskStatusHandler` | IMPLEMENTED |
| `/api/tasks/{id}/subtasks` | Task detail subtask helpers | `POST`, `PUT`, `DELETE /api/tasks/{id}/subtasks/{subTaskId}` | `PostgresTaskCommands` subtask methods | IMPLEMENTED |
| `/api/tasks/{id}/comments` | Task feedback/comment helper | `POST /api/tasks/{id:guid}/comments` | `PostgresTaskCommands.AddCommentAsync()` | PARTIAL |
| `/api/tasks/{id}/attachments` | None | None | None | MISSING |
| `/api/projects` | `ProjectService.loadFromApi()` | `GET /api/projects` | `PostgresProjectQueries.GetProjectsAsync()` | PARTIAL |
| `/api/departments/cards` | `DepartmentService.loadFromApi()` | `GET /api/departments/cards` | `PostgresDepartmentQueries.GetCardsAsync()` | PARTIAL |
| `/api/inter-department-requests` | `InterDepartmentRequestService.loadFromApi()/createRequest()` | `GET`, `POST /api/inter-department-requests` | `PostgresInterDepartmentRequestQueries/Commands` | IMPLEMENTED |
| Inter-request options | FE uses mock-injected options, not API options | `GET department-options`, `owner-options`, `sla-policies` | `PostgresInterDepartmentRequestQueries` | PARTIAL |
| Inter-request workflow | FE calls acknowledge/assign/status/messages/close | Matching `POST` endpoints | `PostgresInterDepartmentRequestCommands`, `AssignInterRequestOwnerHandler` | IMPLEMENTED |
| `/api/notifications` | None | None | None | MISSING |
| `/api/notifications/{id}/read` | None | None | None | MISSING |
| `/api/reports/tasks/export` | None | None | None | MISSING |
| `/api/ai/task-draft` | None | None | None | MISSING |
| `/api/ai/task-summary/{taskId}` | None | None | None | MISSING |
| `/api/ai/task-risk/{taskId}` | None | None | None | MISSING |
| `/api/ai/search` | None | None | None | MISSING |
| Health | Not FE-facing | `GET /api/health/database` | `PostgresDatabaseHealthCheck.CanConnectAsync()` | PARTIAL |

## API Contract Findings

- Swagger/OpenAPI is configured in development with `AddOpenApi`, `AddEndpointsApiExplorer`, and Swashbuckle. Evidence: `Program.cs`, `EnterpriseTask.Api.csproj`.
- Controllers use cancellation tokens consistently in endpoint actions. Evidence: `TasksController.cs`, `InterDepartmentRequestsController.cs`, `AuthController.cs`.
- Status code mapping is basic: success uses `Ok`, `NoContent`, or `CreatedAtAction`; authorization uses `Unauthorized`/`Forbid`; invalid inter-request state maps to `409 Conflict`. Evidence: controllers.
- No API versioning found.
- No pagination contract found for task/inter-request/project/department list endpoints; list endpoints return full visible collections.
- Request DTOs are C# records without explicit DataAnnotations/FluentValidation in the reviewed files. Validation mostly happens in handlers/commands or database constraints.
- Attachments, notifications, users, roles, reports, and AI endpoints are missing despite tables existing in schema.
- FE task API client uses `unknown` payloads, so compile-time contract safety between FE payload and backend DTO is weak.

## Infrastructure Registration Matrix

| Capability | Declared/configured | Registered | Used | Tested | Status | Evidence |
| --- | --- | --- | --- | --- | --- | --- |
| ASP.NET Core controllers | Yes | `AddControllers`, `MapControllers` | Yes | Build only | IMPLEMENTED | `Program.cs` |
| JWT auth | `Jwt:*` config in appsettings/user-secrets | `AddAuthentication().AddJwtBearer()` | Auth endpoints/controllers | Build only | PARTIAL | `Program.cs`, `appsettings.json` |
| Configuration validation | Checks missing DB connection and JWT secret length at startup | Runtime checks only | Yes | Not integration-tested | PARTIAL | `DependencyInjection.cs`, `Program.cs` |
| Database provider | PostgreSQL/Supabase via Npgsql/EF Core DbContext | `UseNpgsql` | Raw SQL services | Build only | PARTIAL | `DependencyInjection.cs`, csproj |
| EF migrations | None found | Not applicable | Not used | Not tested | MISSING | No migrations folder; `ApplicationDbContext` minimal |
| Raw SQL query/command services | Yes | Registered in DI | Used by controllers | Build only | IMPLEMENTED | `DependencyInjection.cs`, `Postgres*` services |
| Swagger/OpenAPI | Yes | Swagger/OpenAPI in development | `/swagger` in dev | Build only | PARTIAL | `Program.cs` |
| CORS | Frontend origins configured | `UseCors("Frontend")` | API runtime | Build only | PARTIAL | `Program.cs` |
| Health check | Custom DB health controller | `IDatabaseHealthCheck` registered | `/api/health/database` | Build only | PARTIAL | `DatabaseHealthController.cs`, `PostgresDatabaseHealthCheck.cs` |
| Redis/MemoryCache | Not found | Not found | Not used | No | MISSING | Source search |
| SignalR | Not found | Not found | Not used | No | MISSING | Source search |
| Hangfire/Quartz/jobs | Not found | Not found | Not used | No | MISSING | Source search |
| Storage/files | DB table exists | No storage provider/API | Not used | No | MISSING | `attachments` table, no attachment controller/service |
| Email | Not found | Not found | Forgot password is local/mock | No | MISSING | `AuthService.forgotPassword()` frontend, backend search |
| AI provider | AI tables/settings exist | No provider/service/controller | Not used | No | MISSING | `supabase_schema_v2_clean.sql`, controller list |
| Search/vector provider | `pgvector` extension and table exist; HNSW index commented | No provider/service/controller | Not used | No | MISSING/PARTIAL | `supabase_schema_v2_clean.sql` |
| Structured logging | Default ASP.NET logging config only | No Serilog/OpenTelemetry found | Basic framework logs | No | PARTIAL | `appsettings.json`, source search |
| Docker/docker-compose | None found outside `node_modules` | Not applicable | Not used | No | MISSING | repo file search |
| CI pipeline | No `.github` workflow files found | Not applicable | Not used | No | MISSING | `.github` file search |
| Environment templates | No `.env.example`; appsettings contain blank placeholders; README documents user secrets | Runtime config via user secrets | Manual | No | PARTIAL | `appsettings.json`, README |
| Secret handling | Appsettings blank; README says user secrets | Startup validates critical values | Manual | No | PARTIAL | `README.md`, `Program.cs`, `DependencyInjection.cs` |

## Build / Deploy Blockers

- Backend startup requires non-empty `ConnectionStrings:DefaultConnection` and `Jwt:Secret`; committed appsettings keep these blank by design, so local/production deployment needs user secrets or environment config.
- No Docker/compose/CI workflow found, so deploy path is manual.
- Database schema apply path is a destructive SQL reset script, not an incremental migration path.
- Development seed endpoint is not usable because `DatabaseSeeder` throws `NotSupportedException`.
- Frontend still has a failing generated app spec, which blocks a clean test run.
- Runtime DB integration was not verified because no live database connection/credentials were provided in this audit context.

## Verification

- `dotnet build backend\EnterpriseTask\EnterpriseTask.slnx` - passed.
- `npm.cmd run build` in `enterprise-task-ms` - passed.
- `npm.cmd test -- --watch=false` in `enterprise-task-ms` - failed: `src/app/app.spec.ts:21` still expects an `h1` containing `Hello, enterprise-task-ms`, but the rendered app does not provide that element/text.

## Conclusion

Prompt 09 is **PARTIAL overall**.

The database schema is broad and close to the desired enterprise model, including RBAC, task workflow, inter-department requests, notification, audit, AI, and embedding tables. The current backend/API/infrastructure only uses a subset: auth, tasks, departments, projects, inter-department requests, and DB health. The largest gaps are missing incremental migrations, missing management/report/notification/attachment/AI APIs, no pagination/versioning/strong validation contract, no realtime/jobs/storage/email/AI/search infrastructure, and no CI/Docker deployment path.
