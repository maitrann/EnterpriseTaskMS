# 02 - Backend Authentication, User, Department and RBAC Audit

## 1. Checklist

| Feature | Status | Backend evidence | DB evidence | Test evidence | Missing behavior | Severity |
|---|---|---|---|---|---|---|
| Login | IMPLEMENTED | `EnterpriseTask.Api/Controllers/AuthController.cs` - `POST api/auth/login`; `EnterpriseTask.Infrastructure/Auth/JwtAuthService.cs` - `LoginAsync` authenticates through Supabase Auth and optionally profile password bypass | `supabase_schema_v2_clean.sql` - `profiles` table references `auth.users`; `profiles.is_active` | No backend auth tests found | No refresh token returned with login | Medium |
| JWT validation | IMPLEMENTED | `EnterpriseTask.Api/Program.cs` - `AddAuthentication().AddJwtBearer(...)`, issuer/audience/lifetime/signing key validation; protected controllers use `[Authorize]` | N/A | No backend authorization tests found | No role/permission policy registration beyond generic authorization | Medium |
| Refresh/revoke token | MISSING | `IAuthService` exposes only `LoginAsync` and `GetUserAsync`; `AuthController` has only `login` and `me` endpoints | No refresh-token or revoked-token table found in schema | No tests found | No refresh endpoint, revoke endpoint, persistence, or rotation flow | High |
| Inactive user | IMPLEMENTED | `JwtAuthService.LoginAsync` returns `null` when loaded profile has `IsActive == false` | `profiles.is_active BOOLEAN NOT NULL DEFAULT TRUE`; RLS reads active profiles | No tests found | Existing JWT remains valid after user is deactivated because API does not re-check `is_active` per request | High |
| User CRUD | MISSING | No user/admin controller found in `EnterpriseTask.Api/Controllers`; no application user command/query interfaces found | `profiles` table has employee code, email, full name, department, manager, job title, active state | No tests found | No backend API to create/update/lock users | High |
| Department CRUD | PARTIAL | `DepartmentsController.GetCards` only exposes `GET api/departments/cards`; `PostgresDepartmentQueries.GetCardsAsync` is read-only | `departments` table has code, name, parent_department_id, manager_id, is_active | No tests found | No create/update/lock/delete department endpoint; read query does not filter `is_active` | Medium |
| Role CRUD | MISSING | No role controller/service/command found | `roles` table exists and is seeded | No tests found | No backend API for role management | High |
| Permission CRUD | MISSING | No permission controller/service/command found | `permissions` table exists and is seeded | No tests found | No backend API for permission management | High |
| Assign role | MISSING | No backend endpoint/command writes `user_roles` | `user_roles` table exists; seed file comments manual assignment after Supabase user creation | No tests found | Role assignment is not available through backend API | High |
| Assign permission | MISSING | No backend endpoint/command writes `role_permissions` | `role_permissions` table exists and seed grants defaults | No tests found | Permission assignment is not available through backend API | High |
| Self scope | PARTIAL | Task queries allow creator/reporter/assignee access in `PostgresTaskQueries.GetTasksAsync`; `PostgresTaskAccessReader.CanAccessTaskAsync` repeats same logic | `can_access_task` DB function also checks creator/reporter/assignment | No tests found | Scope is implemented for task access, not as a general reusable API authorization policy | Medium |
| Related-task scope | IMPLEMENTED | `PostgresTaskQueries.GetTasksAsync`, `GetActivitiesAsync`, and `PostgresTaskAccessReader.CanAccessTaskAsync` check created_by, reporter_id, and `task_assignments` | `task_assignments`; DB function `can_access_task` | No tests found | None observed for task read/update flows | Medium |
| Department scope | IMPLEMENTED | Manager access checks department match or `user_department_scopes` in task access readers; `PostgresDepartmentQueries.GetCardsAsync` scopes department cards by `UserScope` | `profiles.department_id`; `user_department_scopes`; DB function `has_department_scope` | No tests found | Department cards do not filter `departments.is_active` | Low |
| Multi-department/all scope | PARTIAL | `UserScope.CanSeeAllData` grants admin/director all-data reads; task access checks `user_department_scopes` for managers and scoped users | `user_department_scopes`; `roles` seed includes admin/director/manager | No tests found | Multi-department scope is used in task/department/inter-request flows, but there is no backend API to manage scope assignments | Medium |
| Confidential task rule | IMPLEMENTED | `PostgresTaskQueries.GetTasksAsync`; `PostgresTaskAccessReader.CanAccessTaskAsync`; `PostgresTaskPolicyQueries` require confidential tasks to be creator/reporter/assigned unless admin/director | `tasks.is_confidential`; DB function `can_access_task` has matching confidential rule | No tests found | Command-side `IsConfidential` maps every non-`internal` security level to confidential, which may be stricter than labels imply | Low |
| Permission change audit | MISSING | No backend writes to `audit_logs` for role/permission/user scope changes | `audit_logs` table exists; task trigger audits task changes only | No tests found | No audit trail for role/permission/user scope changes | High |
| Permission cache/invalidation | NOT_APPLICABLE | No permission cache service or `IMemoryCache`/`IDistributedCache` usage found | N/A | No tests found | Not applicable because permissions are queried from DB each time | Low |
| 401 vs 403 handling | PARTIAL | Controllers return `Unauthorized()` when actor id missing and `Forbid()` for failed command authorization | N/A | No tests found | Some access-denied read scenarios are hidden as empty lists or `NotFound`; no centralized exception handling for `UnauthorizedAccessException` from `GetRequiredScope` | Medium |
| Seed/default admin | PARTIAL | `DatabaseSeeder` says seeding is handled by SQL and users must be created in Supabase Auth then assigned roles | SQL seeds roles/permissions/default role permissions; user-role seed is commented | No tests found | No complete default admin user creation or backend seed endpoint for admin role assignment | Medium |
| Authorization tests | MISSING | No backend test project found by repository file search | N/A | `rg --files backend/EnterpriseTask` found no test project; frontend `app.spec.ts` is unrelated | No API-level auth/RBAC tests | High |

## 2. Actual Auth Flow

`POST api/auth/login` accepts email/password in `AuthController.Login`. `JwtAuthService.LoginAsync` normalizes email, tries Supabase Auth password grant using configured Supabase URL and anon key, then loads the matching `profiles` row. If Supabase config/auth is unavailable, local profile lookup is allowed only when `Auth:AllowProfilePasswordBypass` is enabled.

If no profile is found or `profiles.is_active` is false, login returns `401 Unauthorized`. If login succeeds, backend creates a signed JWT with subject/name identifier, email, username, `department_id`, and role claims loaded from `user_roles -> roles`. The API validates JWT issuer, audience, lifetime, and HMAC signing key in `Program.cs`.

Protected endpoints rely on `[Authorize]`, then extract the actor through `HttpCurrentUserContext` or controller helpers. Task commands additionally check permission codes such as `task.create`, `task.update`, `task.assign`, and `comment.create` against `user_roles -> role_permissions -> permissions`.

There is no refresh-token or revoke-token flow.

## 3. Actual Permission Matrix vs SRS

| SRS permission area | Actual backend behavior | Status |
|---|---|---|
| Personal dashboard for all suitable roles | No backend dashboard endpoint found in prompt 2 scope; DB seed has `dashboard.view.personal` | PARTIAL |
| Department dashboard for Admin/Director/Manager by scope | `DepartmentsController.GetCards` provides scoped department cards, but it is not a full dashboard API and does not check `dashboard.view.department` permission | PARTIAL |
| Create task: Admin/Director/Manager/Employee | Backend checks `task.create` permission; seed grants it to admin/director/manager/employee | IMPLEMENTED |
| Assign task to others by role and department scope | Create/update/transfer checks `task.assign`; department selection checks `CanUseDepartmentAsync` | IMPLEMENTED |
| Close task: creator/manager/admin by rule | Permission `task.close` is seeded, but task status update currently checks `task.update` and workflow transition only | PARTIAL |
| Audit log: admin or manager by scope | DB has `audit_logs` RLS and task activities query, but no backend audit-log controller found | MISSING |
| AI summary only when user can view task | DB RLS for `ai_task_insights` references `can_access_task`; no backend AI summary endpoint found | PARTIAL |
| AI settings: admin | DB RLS grants AI settings management to admin; no backend AI settings API found | PARTIAL |

## 4. Security Gaps

### Critical

- No Critical gap found from static source review in this prompt.

### High

- Refresh/revoke token is missing. A stolen access token remains valid until expiry because there is no refresh/revoke model.
- Deactivated users are blocked at login, but existing JWTs are not invalidated or checked against `profiles.is_active` during protected API calls.
- User CRUD, lock user, role CRUD, permission CRUD, assign role, and assign permission are absent from backend API despite DB tables existing.
- Permission/user-role changes are not audited by backend code or DB triggers; only task changes have an observed audit trigger.
- No backend authorization tests exist for 401/403, IDOR, inactive user, confidential task, or permission combinations.

### Medium

- Authorization is mostly implemented inside query/command SQL rather than centralized policies, making coverage harder to verify across future endpoints.
- `DepartmentController.GetCards` is read-only and scopes by department/all-data, but does not filter inactive departments.
- Default admin bootstrap is incomplete: role/permission seeds exist, but user creation and role assignment are manual/commented.
- `401` vs `403` behavior is partially consistent; list endpoints may hide unauthorized rows as empty result, while command endpoints return `403`.

### Low

- Permission cache/invalidation is not applicable because permissions are queried directly from DB; this is simpler but potentially less performant.
- Confidential mapping in task create treats any non-`internal` security level as confidential, which may be acceptable but should be explicitly documented in later functional prompts.

## 5. Build and Test Result

| Command | Result | Notes |
|---|---|---|
| `dotnet build backend\EnterpriseTask\EnterpriseTask.slnx` | PASS | 0 warnings, 0 errors |
| `npm.cmd run build` in `enterprise-task-ms` | PASS | Angular production build succeeded |
| `npm.cmd test -- --watch=false` in `enterprise-task-ms` | FAIL | `src/app/app.spec.ts` test `should render title` fails because `querySelector('h1')?.textContent` is `undefined` |

## 6. Files Reviewed

- `backend/EnterpriseTask/EnterpriseTask.Api/Program.cs`
- `backend/EnterpriseTask/EnterpriseTask.Api/Controllers/AuthController.cs`
- `backend/EnterpriseTask/EnterpriseTask.Api/Controllers/TasksController.cs`
- `backend/EnterpriseTask/EnterpriseTask.Api/Controllers/DepartmentsController.cs`
- `backend/EnterpriseTask/EnterpriseTask.Api/Auth/HttpCurrentUserContext.cs`
- `backend/EnterpriseTask/EnterpriseTask.Application/Auth/*`
- `backend/EnterpriseTask/EnterpriseTask.Application/Common/UserScope.cs`
- `backend/EnterpriseTask/EnterpriseTask.Infrastructure/Auth/JwtAuthService.cs`
- `backend/EnterpriseTask/EnterpriseTask.Infrastructure/Tasks/PostgresTaskQueries.cs`
- `backend/EnterpriseTask/EnterpriseTask.Infrastructure/Tasks/PostgresTaskAccessReader.cs`
- `backend/EnterpriseTask/EnterpriseTask.Infrastructure/Tasks/PostgresTaskPolicyQueries.cs`
- `backend/EnterpriseTask/EnterpriseTask.Infrastructure/Tasks/PostgresTaskCommands.cs`
- `backend/EnterpriseTask/EnterpriseTask.Infrastructure/Departments/PostgresDepartmentQueries.cs`
- `backend/EnterpriseTask/EnterpriseTask.Infrastructure/Development/DatabaseSeeder.cs`
- `supabase_schema_v2_clean.sql`
