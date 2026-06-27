# Prompt 10 - Test, Security, and NFR Audit

## Scope

- Source reviewed only; no application code changed.
- Evidence includes backend/frontend test files, build/test commands, auth/security code, raw SQL access controls, config, and frontend services/components.
- Status values used: `IMPLEMENTED`, `PARTIAL`, `MISSING`, `CANNOT_VERIFY`, `NOT_APPLICABLE`.

## Test Inventory and Pass/Fail

| Area | Files / command | Result | Notes |
| --- | --- | --- | --- |
| Backend test projects | Search for `*Test*.csproj` under `backend/EnterpriseTask` | MISSING | No backend unit/integration/API test project found. |
| Backend build | `dotnet build backend\EnterpriseTask\EnterpriseTask.slnx` | PASS | Builds API/Application/Domain/Infrastructure. |
| Backend test command | `dotnet test backend\EnterpriseTask\EnterpriseTask.slnx --no-build` | PASS / NO TESTS FOUND | Command exits 0, but there are no test projects to execute. |
| Frontend unit tests | `enterprise-task-ms/src/app/app.spec.ts` | FAIL | 1 test passes, 1 fails because generated title assertion expects old starter content. |
| Frontend build | `npm.cmd run build` in `enterprise-task-ms` | PASS | Angular production build passes. |
| Frontend lint | `npm.cmd run lint` | FAIL / NOT CONFIGURED | `package.json` has no `lint` script. |
| E2E tests | None found | MISSING | No Playwright/Cypress/e2e project found. |
| Security/dependency scan | Not configured | MISSING | No CI/security scan script found in repo. |

## Acceptance Criteria Coverage Matrix

| Acceptance criteria / case | Test coverage | Runtime implementation evidence | Status |
| --- | --- | --- | --- |
| AC-01 Create task | No automated test | `TasksController.Post`, `CreateTaskHandler`, `PostgresTaskCommands.CreateAsync`, FE `TaskService.createTask()` | PARTIAL |
| AC-02 Task data scope | No automated test | `PostgresTaskQueries.GetTasksAsync`, `PostgresTaskAccessReader.CanAccessTaskAsync` enforce scope in SQL | PARTIAL |
| AC-03 Realtime notification | No automated test | No SignalR/WebSocket/notification service implementation | MISSING |
| AC-04 Overdue job | No automated test | No background job implementation found | MISSING |
| AC-05 AI task draft requires confirmation | No automated test | No AI draft endpoint/UI found | MISSING |
| AC-06 AI summary authorization | No automated test | No AI summary endpoint/UI found | MISSING |
| AC-07 Export respects filters/scope | No automated test | No report/export endpoint found | MISSING |
| DueDate before StartDate | No automated test | DB has `chk_task_date_range`; FE create/edit has client validation | PARTIAL |
| Employee accesses out-of-scope task | No automated test | Backend access SQL exists | PARTIAL |
| Progress 50% | No automated test | `TaskProgressPolicy.Normalize()` clamps progress; DB check enforces 0-100 | PARTIAL |
| SignalR comment | No automated test | SignalR not implemented | MISSING |
| AI draft fields | No automated test | AI draft not implemented | MISSING |
| Unauthorized AI summary | No automated test | AI summary not implemented | MISSING |
| Job overdue | No automated test | Background overdue job not implemented | MISSING |

## Test Type Matrix

| Test type | Status | Evidence |
| --- | --- | --- |
| Domain/unit test | MISSING | Domain policies exist (`TaskWorkflowPolicy`, `TaskProgressPolicy`) but no test project found. |
| Application/service test | MISSING | Handlers exist (`CreateTaskHandler`, `UpdateTaskStatusHandler`) but no tests found. |
| Integration test with database | MISSING | Raw SQL services exist, no integration test project or test DB config found. |
| API authorization test | MISSING | Controllers/policy queries exist, no API tests found. |
| Background job test | MISSING | No background job implementation found. |
| SignalR test | MISSING | No SignalR implementation found. |
| Frontend unit/component test | PARTIAL / FAILING | Only `src/app/app.spec.ts`; generated starter assertion fails. |
| E2E test | MISSING | No e2e framework/config found. |
| AI provider mock/contract test | MISSING | No AI provider implementation found. |

## Security Findings

| Severity | Finding | Evidence | Exploit scenario | Recommendation |
| --- | --- | --- | --- | --- |
| High | No refresh token rotation/revocation; JWT is stored in localStorage | `AuthService` stores `AUTH_TOKEN_KEY` in localStorage; backend only issues access token in `JwtAuthService` | If an attacker gets XSS or local device access, token can be reused until expiry with no revocation path | Use short-lived access token plus refresh token rotation/revocation, or secure httpOnly cookie strategy; add logout/server revocation strategy if required. |
| High | Missing rate limiting/brute-force protection for login and sensitive endpoints | `Program.cs` has no `AddRateLimiter`; `AuthController.Login` is anonymous | Attackers can brute-force login or spam task/comment endpoints | Add ASP.NET Core rate limiting per IP/user/route; monitor failed logins; consider account lockout via Supabase/Auth provider. |
| High | No automated auth/data-scope tests for IDOR | Access SQL exists in `PostgresTaskAccessReader`, but no tests | A future query/controller change could expose out-of-scope tasks without test failure | Add API/integration tests for employee/manager/admin task visibility, confidential task visibility, and direct `/api/tasks/{id}` mutation attempts. |
| Medium | Swagger is dev-only but production exposure depends on `ASPNETCORE_ENVIRONMENT` correctness | `Program.cs` maps Swagger only in development | Misconfigured environment could expose interactive API docs | Keep dev-only mapping; add deployment check/documentation; optionally require auth for Swagger in shared environments. |
| Medium | CORS allows localhost origins and any method/header | `Program.cs` `.WithOrigins("http://localhost:4200", "https://localhost:4200").AllowAnyHeader().AllowAnyMethod()` | Fine for local dev, but production requires explicit frontend origins | Configure environment-specific CORS origins; avoid broad production CORS. |
| Medium | Multi-step raw SQL commands lack explicit transactions | `PostgresTaskCommands.CreateAsync()` inserts task then assignments/tags in separate commands | Partial writes can occur if later assignment/tag insert fails after task creation | Wrap multi-step create/update/duplicate commands in DB transactions. |
| Medium | File upload security is unimplemented | DB `attachments` table exists, but no attachment API/UI upload | Once upload is added, missing validation could allow malware/path traversal/oversized files | Implement storage abstraction with content type/size validation, generated object keys, auth checks, scanning policy, and download authorization tests. |
| Medium | No AI leakage controls because AI layer is not implemented | AI schema exists, no AI provider/API/UI | Future AI endpoints could leak task/confidential data if access checks are skipped | When adding AI, enforce task access before prompt construction, log minimal metadata, redact secrets, and test unauthorized AI summary/search. |
| Low | Password hasher exists but is not used in normal Supabase login path | `PasswordHasher.cs`, `JwtAuthService.AuthenticateWithSupabaseAuthAsync()` | Dead security code can mislead maintainers; bypass mode could become risky if enabled | Document bypass mode as local-only or remove unused hasher if Supabase remains source of truth. |
| Low | XSS posture relies on Angular interpolation; no explicit security tests | Templates render comments/messages via interpolation; no `[innerHTML]` found in reviewed code | Future rich-text rendering could accidentally introduce XSS | Keep avoiding raw HTML; add component tests for escaping comments/messages if rich content is introduced. |
| Low | Secrets are blank in appsettings, but demo credentials are visible in frontend mock UI | `appsettings.json`, `core/mock-data/auth.mock.ts`, login UI displays demo password | Demo credentials can be confused with production credentials | Keep mock credentials dev-only; ensure production build/config does not expose real/demo accounts. |

## Positive Security Notes

- JWT validation checks issuer, audience, signing key, lifetime, and requires at least 32 UTF-8 bytes for the HMAC secret. Evidence: `backend/EnterpriseTask/EnterpriseTask.Api/Program.cs`.
- Controllers for tasks/projects/departments/inter-department requests are protected by `[Authorize]`. Evidence: controller classes.
- Raw SQL uses parameters through `DbCommand` parameter binding, reducing SQL injection risk. Evidence: `PostgresCommandBase.cs`, `PostgresQueryBase.cs`.
- Task access checks enforce role, assignment, department scope, and confidential task constraints in backend SQL. Evidence: `PostgresTaskAccessReader.cs`, `PostgresTaskQueries.cs`.
- PasswordHasher uses PBKDF2-SHA256 with random salt and fixed-time compare, although normal login delegates to Supabase Auth. Evidence: `PasswordHasher.cs`.

## NFR Matrix

| NFR | Status | Evidence | Notes |
| --- | --- | --- | --- |
| Server-side pagination | MISSING | `GET /api/tasks`, inter-request/project endpoints return full lists | Needed for production data volume. |
| Query/index posture | PARTIAL | Schema has many indexes; prompt 09 noted missing created_at/compound indexes for actual feeds | Needs query-plan validation against real DB. |
| Cache | MISSING | No MemoryCache/Redis usage found | Could matter for lookup data/form options. |
| Retry/idempotency | MISSING | No retry policy/idempotency keys found | Multi-submit create endpoints may duplicate records. |
| Health check | PARTIAL | `/api/health/database` exists | Only DB connectivity; not full readiness/liveness. |
| Observability | PARTIAL | Default ASP.NET logging config only | No structured logging, tracing, metrics, correlation IDs. |
| UTC/timezone | PARTIAL | Backend uses `DateTimeOffset.UtcNow`; FE uses local date conversions and Angular date pipe | Needs explicit timezone policy for SLA/deadlines. |
| Graceful AI/email failure | MISSING | AI/email not implemented; forgot password frontend is local mock | Add fallback/error model when implemented. |
| Usability states | PARTIAL | Login has submitting/error; core services often swallow API errors | Need global error/loading/empty states. |
| Cancellation token | IMPLEMENTED | Controllers/services pass `CancellationToken` broadly | Good baseline. |
| Transaction boundaries | MISSING/PARTIAL | Multi-step raw SQL commands run separate statements | Add transactions for task create/update/duplicate and inter-request updates with messages. |
| Accessibility | PARTIAL | Labels exist in forms, but dialog focus management not evident | Needs component/e2e accessibility checks. |
| Performance budgets | MISSING | No bundle/perf budget or load tests found | Angular build reports sizes, but no enforced budget/test policy. |

## Priority Tests to Add

1. Domain unit tests for `TaskWorkflowPolicy.CanTransition()` and `TaskProgressPolicy.Normalize()`, including progress `50`, below `0`, and above `100`.
2. Application tests for `CreateTaskHandler`: create allowed, missing `task.create`, invalid department scope, assignment without `task.assign`.
3. API integration tests for task scope/IDOR: employee cannot list or mutate out-of-scope/confidential tasks.
4. API tests for due date before start date, verifying backend/database validation response.
5. API tests for task status transitions and invalid transition rejection.
6. Integration tests for task create/update transaction behavior once transactions are added.
7. Frontend component tests for login error/loading, task create validation, task board empty/error state, and task detail comment/subtask actions.
8. E2E smoke test: login -> create task -> update status -> add comment -> verify timeline.
9. Realtime notification/comment tests after SignalR is implemented.
10. Job tests for overdue detection after background job is implemented.
11. AI mock/contract tests for task draft confirmation, unauthorized summary denial, and no automatic task mutation.
12. Report/export scope tests after export endpoint exists.

## Verification

- `dotnet build backend\EnterpriseTask\EnterpriseTask.slnx` - passed.
- `dotnet test backend\EnterpriseTask\EnterpriseTask.slnx --no-build` - exits 0, but no backend test projects were found.
- `npm.cmd run build` in `enterprise-task-ms` - passed.
- `npm.cmd test -- --watch=false` in `enterprise-task-ms` - failed: `src/app/app.spec.ts:21` still expects an `h1` containing `Hello, enterprise-task-ms`, but the rendered app does not provide that element/text.
- `npm.cmd run lint` in `enterprise-task-ms` - failed because `package.json` has no `lint` script.

## Conclusion

Prompt 10 is **MISSING/PARTIAL overall**. Security foundations exist for JWT validation, protected controllers, parameterized SQL, and backend data-scope checks, but the project lacks meaningful automated coverage for the acceptance criteria and has important NFR gaps: no server pagination, no rate limiting, no refresh/revocation, no CI/security scan, no backend tests, no E2E tests, no realtime/jobs/AI/export tests, and only one failing frontend starter spec.
