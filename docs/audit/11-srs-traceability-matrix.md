# Prompt 11 - SRS Traceability Matrix and Gap Report

## Scope and Method

- Inputs: `docs/audit/01-repository-inventory.md` through `docs/audit/10-tests-security-nfr.md`; all ten reports are present.
- This report consolidates those audits. It does not upgrade a status merely because a table, interface, model, placeholder, or disabled menu exists.
- `Overall` is `IMPLEMENTED` only where the required backend, frontend, database/API path is usable end-to-end without a known blocking gap.
- Completion rate counts only `IMPLEMENTED`. The weighted progress indicator uses `IMPLEMENTED = 1`, `PARTIAL = 0.5`, and all other states `= 0`.
- The supplied checklist has 34 MVP and 7 Advanced requirements. `SEARCH-01` and `TEST-01` are added because Prompt 11 explicitly requires `SEARCH-xx` and `TEST-xx` traceability groups.
- Paths beginning with `EnterpriseTask.*` are relative to `backend/EnterpriseTask/`; frontend paths such as `core/`, `features/`, and `src/` are relative to `enterprise-task-ms/src/app/` unless the full path is shown. SQL names refer to `supabase_schema_v2_clean.sql`. No secrets or connection values are reproduced.

## Traceability Matrix

| ID | Module | Requirement | Priority | Backend status / evidence | Frontend status / evidence | Database status / evidence | API status | Test status | Documentation | Overall | Gap | Next action | Effort | Impact |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| AUTH-01 | Auth | Login and JWT/access token | MVP | IMPLEMENTED - `EnterpriseTask.Api/Controllers/AuthController.cs` (`Login`, `Me`); `EnterpriseTask.Infrastructure/Auth/JwtAuthService.cs` | PARTIAL - `core/services/auth.service.ts`, `auth.interceptor.ts`; restores local data rather than calling `/me` | IMPLEMENTED - `profiles`, `auth.users` reference | IMPLEMENTED - `POST /api/auth/login`, `GET /api/auth/me` | MISSING - no backend auth/API tests | PARTIAL - setup documented, acceptance test absent | IMPLEMENTED | Session restoration and automated verification remain, but login is usable end-to-end | Restore through `/me`; add login/expiry tests | M | High |
| AUTH-02 | Auth | Refresh and revoke token | MVP | MISSING - `IAuthService` exposes no refresh/revoke flow | MISSING - access token only in localStorage | MISSING - no refresh/revocation persistence | MISSING | MISSING | MISSING | MISSING | Stolen access tokens cannot be rotated or revoked | Add short-lived access token, rotating refresh token and logout revocation | L | High |
| USER-01 | User | User CRUD and account lock | MVP | MISSING - no user controller/application commands | MISSING - `core/services/user.service.ts` is empty; no route | PARTIAL - `profiles.is_active` and user fields exist | MISSING | MISSING | PARTIAL - schema is described by audits | MISSING | Inactive users are checked at login, but there is no management flow and existing JWTs remain valid | Add scoped admin user API/UI and active-state enforcement | L | High |
| DEPT-01 | Department | Hierarchy and manager management | MVP | PARTIAL - `DepartmentsController.GetCards`; read-only `PostgresDepartmentQueries` | PARTIAL - `/departments` renders cards only | IMPLEMENTED - `departments.parent_department_id`, `manager_id`, scope tables | PARTIAL - `GET /api/departments/cards` only | MISSING | PARTIAL | Hierarchy data exists but CRUD/manager assignment is unavailable | Add authorized department management endpoints and screen | L | Medium |
| RBAC-01 | RBAC | Role and permission management | MVP | MISSING - no role/permission controller or commands | MISSING - empty `role.service.ts` and `role.guard.ts`; no screen | IMPLEMENTED - `roles`, `permissions`, `role_permissions`, seeded grants | MISSING | MISSING | PARTIAL | Runtime checks seeded permissions, but roles/grants cannot be managed in the product | Add admin APIs/UI plus change audit | XL | High |
| RBAC-02 | RBAC | Self/related/department/all scope | MVP | IMPLEMENTED - `PostgresTaskQueries`, `PostgresTaskAccessReader`, request/department queries | PARTIAL - role/department checks exist only as UI gating | IMPLEMENTED - `user_department_scopes`, `can_access_task`, scope helpers | PARTIAL - enforced strongly for implemented task/request APIs, not a reusable policy across absent APIs | MISSING - no IDOR/scope tests | PARTIAL | PARTIAL | Good task/request scope logic, but no scope-management API and no regression tests | Add API scope tests and scope assignment management | L | Critical |
| TASK-01 | Task | CRUD and generated code | MVP | PARTIAL - create/update exist; no delete/detail endpoint; timestamp code collision risk | PARTIAL - create is API-backed; edit can remain local-only | IMPLEMENTED - `tasks`, unique `code`, constraints/triggers | PARTIAL - create/update/list exist; delete and `GET /{id}` absent | MISSING | IMPLEMENTED - audits describe actual flow | PARTIAL | CRUD is incomplete and edit behavior is inconsistent | Add detail/delete policy, persist edit, generate collision-safe code | L | High |
| TASK-02 | Task | Assignment/co-assignee/watcher | MVP | IMPLEMENTED - `CreateTaskHandler`; `PostgresTaskCommands` writes all assignment types | IMPLEMENTED - create/edit controls and task actions expose assignment fields | IMPLEMENTED - `task_assignments`, `task_assignment_type` | IMPLEMENTED - create/update/assignee routes | MISSING | PARTIAL | IMPLEMENTED | Usable end-to-end; lacks assignment-specific audit and regression tests | Add assignment audit and authorization tests | M | High |
| TASK-03 | Task | Status workflow | MVP | PARTIAL - `TaskStatusIds` and `TaskWorkflowPolicy` now match seeded DB status IDs; command layer returns conflict for invalid transitions and sets completion/close timestamps | PARTIAL - `task-status.constants.ts` and task UI helpers now use canonical DB IDs; no E2E browser flow run | PARTIAL - lookup statuses exist and initial migration preserves canonical order; no live DB migration/status integration test run | PARTIAL - `/status` maps invalid workflow to `409 Conflict`; no API integration test yet | PARTIAL - domain tests cover seed ID order and transition matrix | IMPLEMENTED - P0-03 evidence recorded below | PARTIAL | Core ID mismatch is fixed, but live DB/API integration tests for status rows and timestamps are still missing | Add API integration tests with a configured test DB before marking implemented | M | Critical |
| TASK-04 | Task | Filter/search/sort/paging | MVP | MISSING - list query has no filter/page DTO | PARTIAL - task board filters and searches loaded data only; no pagination | PARTIAL - indexes exist, but no supporting server query contract | MISSING | MISSING | PARTIAL | Unbounded list and client-only operations fail at production volume | Design scoped paged query with filters/sort and update UI | L | High |
| TASK-05 | Task | Confidential task access | MVP | IMPLEMENTED - query, access reader and policy queries enforce related/admin rules | IMPLEMENTED - inaccessible rows are not surfaced; edit UI is additionally gated | IMPLEMENTED - `tasks.is_confidential`, `can_access_task` | IMPLEMENTED - checks apply to implemented task operations | MISSING | IMPLEMENTED - audits 02/03/10 document the rule | IMPLEMENTED | No known functional blocker; IDOR tests are still absent | Add employee/manager/admin confidential-task API tests | M | Critical |
| TASK-06 | Task | Copy/similar task | MVP | PARTIAL - `DuplicateAsync` copies selected people/subtasks/attachments but omits comments/activity/extensions | PARTIAL - duplicate action exists in task workflow | IMPLEMENTED - target tables support copied records | IMPLEMENTED - `POST /api/tasks/{id}/duplicate` | MISSING | PARTIAL | Copy semantics are incomplete and attachment copying lacks a storage implementation | Define copy contract; exclude or securely clone stored files | M | Medium |
| SUBTASK-01 | Subtask | CRUD and due-date rule | MVP | IMPLEMENTED - task controller and commands implement create/update/delete | PARTIAL - detail drawer invokes subtask actions; optimistic failure can be silent | IMPLEMENTED - `subtasks` and due-date trigger | IMPLEMENTED - subtask routes exist | MISSING | IMPLEMENTED | PARTIAL | DB authorization helper context and silent FE failures are not verified end-to-end | Add friendly validation, error rollback and DB integration tests | M | High |
| SUBTASK-02 | Subtask | Parent progress calculation | MVP | MISSING - no calculation/update flow | PARTIAL - UI displays progress but does not derive parent progress | PARTIAL - flag `subtask_progress_auto_sync` exists without implementation | MISSING | MISSING | PARTIAL | MISSING | Completing subtasks does not recalculate or suggest parent completion | Implement transaction-safe aggregate and pending-review suggestion | M | High |
| COLLAB-01 | Collaboration | Comment and mention | MVP | PARTIAL - comment REST endpoint exists; no mention parsing/notification | PARTIAL - feedback composer and timeline exist, not threaded/realtime | PARTIAL - comment/activity structures exist; runtime activity writes are incomplete | PARTIAL - comment endpoint only | MISSING | PARTIAL | PARTIAL | Basic comments work, mentions and reliable activity propagation do not | Define comment DTO, mentions, recipients, audit and tests | L | High |
| COLLAB-02 | Collaboration | Internal/public note | MVP | MISSING - no visibility semantics in command/API | MISSING - no visibility selector | PARTIAL - schema-level fields may support messages, but no verified note flow | MISSING | MISSING | PARTIAL | MISSING | Notes cannot be classified or access-controlled as internal/public | Add visibility model, authorization, UI indicator and tests | M | Medium |
| FILE-01 | File | Secure attachment upload/download | MVP | MISSING - no storage provider/controller | MISSING - token/name placeholders only; no file input/progress/download | PARTIAL - `attachments` and RLS exist | MISSING | MISSING | PARTIAL | MISSING | Schema and duplicate SQL create the appearance of support, but no file bytes ever flow | Add storage abstraction, validation/scanning policy and authorized APIs/UI | XL | Critical |
| NOTIF-01 | Notification | Persistent realtime notification | MVP | MISSING - no notification service, SignalR hub or event publishing | MISSING - service empty; badge hard-coded to `3` | IMPLEMENTED - notification tables/preferences exist | MISSING | MISSING | PARTIAL | MISSING | No runtime producer, delivery channel, subscription or live UI | Implement transactional notification creation and authenticated SignalR delivery | XL | High |
| NOTIF-02 | Notification | Read/unread and email fallback | MVP | MISSING - no read/email service | MISSING - no list/read actions | IMPLEMENTED - notification/read-preference schema exists | MISSING | MISSING | PARTIAL | MISSING | Badge is cosmetic; no persistence API or fallback channel | Add list/page/read APIs, UI, email provider and failure policy | L | High |
| JOB-01 | Job | Due-soon reminder | MVP | MISSING - no hosted service/Hangfire/Quartz job | NOT_APPLICABLE - no direct UI required beyond notifications | PARTIAL - due dates and notification schema exist | MISSING | MISSING | PARTIAL | MISSING | Nothing schedules or deduplicates reminders | Add idempotent scheduled worker and job tests | L | High |
| JOB-02 | Job | Automatic overdue flag | MVP | MISSING - no system job/command | PARTIAL - UI can display statuses but cannot create system processing | PARTIAL - overdue status and `overdue_at` exist | MISSING | MISSING | PARTIAL | MISSING | Tasks never become overdue automatically | Implement batched UTC worker with locking/idempotency | L | High |
| DASH-01 | Dashboard | Personal dashboard | MVP | PARTIAL - no dedicated metrics API; scoped task/card data exists | PARTIAL - dashboard computes KPIs from loaded tasks | PARTIAL - source tables/indexes exist | PARTIAL - reuses unbounded list APIs | MISSING | PARTIAL | PARTIAL | Metrics are client-derived and depend on an unbounded snapshot | Add scoped aggregate endpoint and loading/error states | M | Medium |
| DASH-02 | Dashboard | Manager dashboard | MVP | PARTIAL - department cards are scoped but not a full manager dashboard | PARTIAL - department and dashboard cards exist | PARTIAL - department/task data supports aggregation | PARTIAL | MISSING | PARTIAL | PARTIAL | Missing server metrics, trends and explicit dashboard permission | Add scoped manager aggregates and permission checks | L | High |
| DASH-03 | Dashboard | Admin/director dashboard | MVP | MISSING - no admin aggregate endpoint | MISSING - no dedicated admin/director screen | PARTIAL - source data exists | MISSING | MISSING | PARTIAL | MISSING | No system-wide operational dashboard | Define KPIs and build aggregate API/UI with limits | L | Medium |
| REPORT-01 | Report | Filtered reports | MVP | MISSING - no report queries/controller | MISSING - disabled menu and empty `report.service.ts` | PARTIAL - data/index foundation exists | MISSING | MISSING | PARTIAL | MISSING | Reporting is only a menu placeholder | Add scoped report query contract and screen | L | High |
| REPORT-02 | Report | Excel/PDF export | MVP | MISSING - no exporter/controller | MISSING | PARTIAL - data exists, no export persistence needed | MISSING | MISSING | PARTIAL | MISSING | No format generation, scope/filter enforcement or size limits | Add streaming scoped export plus authorization tests | L | High |
| AUDIT-01 | Audit | Important action audit trail | MVP | PARTIAL - task activity is read; backend does not consistently write audit/activity events | PARTIAL - task timeline shows a subset; no audit screen | PARTIAL - `audit_logs` and task trigger cover create/status/due/priority, not assignment/RBAC/settings | MISSING - no audit query API | MISSING | IMPLEMENTED - coverage matrix in audit 05 | PARTIAL | Critical mutations and administrative changes are not captured or queryable | Centralize audit writes and add scoped audit API/UI | L | High |
| REQUEST-01 | Request | Inter-department request | Advanced | IMPLEMENTED - controller, queries, commands and owner handler cover list/create/status/assign/message/close | PARTIAL - routed workflow works, but uses mock-shaped auth fields and silent optimistic handling | IMPLEMENTED - request/message/status tables and policies | IMPLEMENTED - primary workflow endpoints exist | MISSING | IMPLEMENTED - audits 04/07/09 map workflow | PARTIAL | Strong demo path, but FE identity/DTO drift and no automated verification prevent a clean end-to-end verdict | Align FE DTOs to authenticated actor and add workflow integration/E2E tests | M | High |
| REQUEST-02 | Request | SLA tracking | Advanced | PARTIAL - fields/options exist; no scheduler/escalation service | PARTIAL - request UI displays workflow data, not verified SLA escalation | IMPLEMENTED - SLA/status fields and settings exist | PARTIAL - fields flow in request API, management/escalation absent | MISSING | PARTIAL | PARTIAL | SLA is stored, not actively monitored or escalated | Add SLA policy, UTC worker, notifications and breach tests | L | High |
| AI-01 | AI | Smart Task Creator | MVP | MISSING - no provider/service/controller | MISSING - no natural-language entry or editable AI draft | PARTIAL - request log/settings tables only | MISSING | MISSING | IMPLEMENTED - absence documented in audit 06 | MISSING | No runtime AI flow | Add provider abstraction, access/redaction policy, draft-only endpoint and UI confirmation | XL | Medium |
| AI-02 | AI | Task summarization | MVP | MISSING | MISSING | PARTIAL - `ai_task_insights.summary` and RLS exist | MISSING | MISSING | IMPLEMENTED | MISSING | Schema is unused; no access check before prompt construction | Add permission-aware summary endpoint, cache/log policy and UI | L | High |
| AI-03 | AI | Priority and risk suggestion | MVP | MISSING | MISSING | PARTIAL - insight fields exist | MISSING | MISSING | IMPLEMENTED | MISSING | No inference or human-confirmation workflow | Add explainable suggestion endpoint and editable UI | L | Medium |
| AI-04 | AI | Smart assignment | Advanced | MISSING | MISSING | PARTIAL - feature setting seed only | MISSING | MISSING | IMPLEMENTED | MISSING | Configuration row is not an implementation | Add candidate retrieval, scoped ranking and explicit confirmation | XL | Medium |
| AI-05 | AI/Search | Semantic search/RAG | Advanced | MISSING - no embedding/index/query pipeline | MISSING - only plain client-side search | PARTIAL - pgvector and `embedding_index`; HNSW index commented | MISSING | MISSING | IMPLEMENTED | PARTIAL | Vector schema exists but is never populated or queried | Build authorized chunk/index lifecycle and semantic query API | XL | High |
| AI-06 | AI | Internal chatbot | Advanced | MISSING | MISSING | PARTIAL - generic AI logs exist, but no conversation model | MISSING | MISSING | IMPLEMENTED | MISSING | No retrieval, conversation, citation or access-control flow | Defer until authorized search is production-ready | XL | High |
| AI-07 | AI | Auto classification | Advanced | MISSING | MISSING | PARTIAL - request classification columns exist | MISSING | MISSING | IMPLEMENTED | PARTIAL | Columns suggest support, but no classifier writes them | Add provider workflow with confidence and human override | L | Medium |
| AI-08 | AI | Meeting/email to task | Advanced | MISSING | MISSING | PARTIAL - generic task source fields exist, but no ingestion model | MISSING | MISSING | IMPLEMENTED | MISSING | No email/calendar ingestion or consent model | Define source connector and draft-confirmation workflow | XL | Medium |
| SEARCH-01 | Search | Authorized keyword search | MVP | MISSING - no server search/filter endpoint | PARTIAL - task board searches loaded data; header input is not an application search flow | PARTIAL - task indexes exist, query support is incomplete | MISSING | MISSING | PARTIAL | PARTIAL | Search is incomplete, client-only and cannot cover all authorized records | Add paged server search with scope predicates and index validation | L | High |
| NFR-01 | NFR | Server pagination and indexes | MVP | MISSING - main list APIs are unbounded | MISSING - no pagination state/control | PARTIAL - many indexes exist; feed/compound/index-plan gaps remain | MISSING | MISSING | IMPLEMENTED - audits 09/10 identify risk | PARTIAL | Index foundation cannot compensate for unbounded API contracts | Add paging contracts and validate plans against representative data | L | High |
| NFR-02 | Security | Authorization at API | MVP | PARTIAL - `[Authorize]`, permissions and task/request scope checks are strong; management/AI/file/report APIs do not exist | PARTIAL - UI gating is role-string based and unused `role.guard.ts` gives no security | IMPLEMENTED - RLS/helpers plus backend SQL checks | PARTIAL | MISSING - no IDOR/authorization suite | IMPLEMENTED | PARTIAL | Existing APIs have a solid base, but inconsistent policies and zero regression coverage are high risk | Introduce named policies and comprehensive API authorization tests | L | Critical |
| NFR-03 | Resilience | Retry/logging/graceful AI failure | MVP | MISSING - default logs only; no retry/circuit breaker/provider fallback | PARTIAL - services often swallow errors, which is not graceful recovery | NOT_APPLICABLE for retry state; AI logs table is unused | MISSING | MISSING | PARTIAL | MISSING | Failures can become silent and AI/email resilience is absent | Add structured errors/logging, retry policy and explicit UI states | L | High |
| NFR-04 | AI Safety | AI suggestions require confirmation | MVP | MISSING - no AI flow or confirmation contract | MISSING | PARTIAL - settings/log schema cannot enforce confirmation | MISSING | MISSING | PARTIAL | MISSING | Cannot verify safety behavior because suggestions do not exist | Make all AI writes draft-only and test explicit user confirmation | M | High |
| TEST-01 | Test | Automated acceptance/security coverage | MVP | MISSING - no backend test project | PARTIAL - one generated `app.spec.ts`; one assertion fails | NOT_APPLICABLE | MISSING - no integration/E2E harness | MISSING - `dotnet test` finds none; Angular test fails | IMPLEMENTED - audit 10 records commands/results | MISSING | No meaningful unit, integration, API authorization or E2E safety net | Add domain/API integration baseline, then critical E2E smoke path | XL | Critical |

## Completion Analysis

### MVP by Module

Completion counts only `IMPLEMENTED`; weighted progress is directional, not an acceptance result.

| Module | Implemented / total | Completion | Weighted progress | Interpretation |
| --- | ---: | ---: | ---: | --- |
| Authentication | 1 / 2 | 50.0% | 50.0% | Login works; token lifecycle is absent |
| User/Department/RBAC | 0 / 4 | 0.0% | 25.0% | Read/scope/schema foundations, no management product |
| Task | 2 / 6 | 33.3% | 66.7% | Assignment and confidentiality are strongest; workflow/list CRUD gaps block acceptance |
| Subtask | 0 / 2 | 0.0% | 25.0% | CRUD is close, parent aggregation is absent |
| Collaboration/File | 0 / 3 | 0.0% | 16.7% | Basic comments only; notes/files missing |
| Notification/Jobs | 0 / 4 | 0.0% | 0.0% | Schema without runtime services |
| Dashboard/Report/Audit | 0 / 6 | 0.0% | 25.0% | Client aggregates and partial triggers only |
| AI MVP | 0 / 3 | 0.0% | 0.0% | Schema only |
| Search/NFR/Test | 0 / 6 | 0.0% | 25.0% | Security base and indexes exist; production safeguards do not |
| **All MVP** | **3 / 36** | **8.3%** | **27.8%** | Strict end-to-end scoring |

### Advanced

| Module | Implemented / total | Completion | Weighted progress |
| --- | ---: | ---: | ---: |
| Inter-department request/SLA | 0 / 2 | 0.0% | 50.0% |
| Advanced AI/Search | 0 / 5 | 0.0% | 20.0% |
| **All Advanced** | **0 / 7** | **0.0%** | **28.6%** |

The strict rate is intentionally low: a passing build, broad schema, or convincing UI is not counted as completed SRS behavior unless the necessary layers are connected.

## Top 10 Gaps

| Rank | Gap | Why it matters | Primary requirements |
| ---: | --- | --- | --- |
| 1 | Status IDs in `TaskWorkflowPolicy` do not match seeded DB status IDs | Can apply semantically wrong transitions in the main task flow | TASK-03 |
| 2 | No automated backend/API authorization tests | A future raw-SQL/controller change can create an undetected IDOR or confidential-data leak | RBAC-02, TASK-05, NFR-02, TEST-01 |
| 3 | No refresh rotation/revocation or login rate limiting | Stolen tokens remain usable and login is brute-forceable | AUTH-02, NFR-02 |
| 4 | No server paging/filter/search contract | Unbounded lists make task/request/dashboard behavior unreliable at realistic volume | TASK-04, SEARCH-01, NFR-01 |
| 5 | No transactions around multi-step raw SQL commands | Partial task/request writes can survive a later command failure | TASK-01, TASK-02, NFR-03 |
| 6 | File support is schema/placeholder only | Portfolio demo can imply uploads that cannot store or authorize bytes | FILE-01 |
| 7 | Realtime notifications, read state and email fallback are absent | Collaboration is polling/local state, not the SRS notification system | NOTIF-01, NOTIF-02 |
| 8 | Due-soon/overdue/SLA workers are absent | Time-driven business rules never execute | JOB-01, JOB-02, REQUEST-02 |
| 9 | Dashboard/report/export APIs are absent | UI metrics are client-derived; reporting/export claims are unsupported | DASH-01..03, REPORT-01..02 |
| 10 | All AI runtime features are absent | AI tables/settings can be mistaken for delivered AI capability | AI-01..08, NFR-04 |

## Features That Look Implemented but Are Not End-to-End

- Attachments: `attachments` table, RLS, duplicate SQL and filename fields exist, but there is no storage provider, upload/download API, file picker or authorization test.
- Notifications: tables/preferences and a bell exist, but the badge is hard-coded and there is no producer, API, SignalR client, read action or email provider.
- AI and semantic search: settings/log/insight/vector tables exist, but no provider, indexing job, controller, service or UI consumes them.
- Reports: a disabled sidebar item and empty service exist, but no report query or exporter exists.
- User/RBAC management: schema and seeded grants are substantial, but management services/screens and change audit are absent.
- Dashboard: KPI cards render, but values are calculated from an unbounded client task snapshot rather than a server aggregate contract.
- Task edit: the modal changes local state, while only selected action helpers persist through API endpoints.
- Subtask progress: a configuration flag exists, but no parent aggregation is executed.
- SLA: fields/settings exist and are displayed, but no clock, breach worker, escalation or notification is active.

## Unused or Misleading Code Paths

- `enterprise-task-ms/src/app/core/guards/role.guard.ts`, `user.service.ts`, `role.service.ts`, `report.service.ts`, and `notification.service.ts` are empty/unwired.
- NgRx task reducer is registered, but effects are commented out and the running application primarily uses signal services.
- `PasswordHasher.cs` is not used by the normal Supabase authentication path.
- `DatabaseSeeder` is registered, but its runtime path throws `NotSupportedException`; SQL is the actual seed path.
- `TaskWorkflowPolicy` has a reopen branch, but callers never enable it.
- `tasks.subtask_progress_auto_sync`, AI settings, AI logs/insights and `embedding_index` are not consumed by runtime services.
- The database HNSW vector index is commented out.
- `GET /api/auth/me` exists, but frontend session restoration reads localStorage rather than invoking it.
- The development seed endpoint is mapped but cannot complete seeding.

## Frontend/Backend Mismatches

- Frontend edit can still report success in local state without calling the general backend task update endpoint, but task/request optimistic API mutations now roll back and expose mutation errors when persistence fails.
- Frontend filters/searches locally while backend list endpoints accept no filter, sort or pagination parameters.
- Frontend uses `unknown` task API payloads and duplicated status/priority models, weakening compile-time contract checks.
- C# task workflow uses numeric status IDs that conflict with SQL seed ordering; frontend action labels can therefore diverge from persisted meaning.
- Inter-department request UI relies on mock-shaped user fields/role strings while backend authorization derives identity and scope from JWT claims/database queries.
- Header search and notification badge look interactive but have no corresponding application API flow.
- `/api/auth/me` is available but not used for startup validation or token-expiry recovery.

## Portfolio Demo Risks

- The demo can appear healthy because both builds pass and read paths retain mock/local state when the API is offline. Covered task/request mutations now roll back on API failure, but edit/paging gaps remain.
- Changing task status is the highest functional demo risk because of the C#/DB status-ID mismatch.
- Any attachment, notification, report/export or AI interaction will expose a non-functional placeholder immediately.
- A larger dataset will expose unbounded queries and client-derived dashboard metrics.
- The Angular test suite is red, there are no backend/E2E tests, and no CI pipeline prevents regressions.
- Startup requires external DB/JWT configuration, while the registered development seeder cannot provision a working environment.
- Direct PostgreSQL access and Supabase RLS/helper assumptions need live integration verification; build success alone does not validate runtime authorization context.

## CV Claim Guidance

### Reasonable to Claim Now

- Built a layered ASP.NET Core and Angular task-management application backed by PostgreSQL/Supabase.
- Implemented JWT bearer login, protected controllers, parameterized SQL access, task-related data scope and confidential-task checks.
- Implemented task creation, multi-role assignment, tags, comments/subtasks and an inter-department request REST workflow.
- Designed a broad PostgreSQL schema with RBAC, task workflow, audit, notification, SLA and AI/vector-ready structures.
- Built Angular task board/detail, department/project and inter-department request screens integrated with core REST endpoints.

Qualify these as project implementation experience, not production readiness; mention that automated coverage and deployment automation are still in progress.

### Do Not Claim Yet

- Production-ready RBAC/security, because refresh rotation, logout revocation and rate limiting now exist but still lack DB-backed integration/replay tests and broad authorization tests.
- Complete task workflow, because status IDs are mismatched and reopen/overdue paths are inactive.
- Realtime collaboration/notifications, background scheduling, email fallback or secure file management.
- Reporting, Excel/PDF export, server-side search/pagination or production-scale dashboards.
- Any implemented AI, RAG, chatbot, smart assignment, classification, or meeting/email ingestion capability.
- Comprehensive testing, CI/CD, containerized deployment, observability or production readiness.

## Verification Baseline

No source code was changed and builds/tests were not rerun for this consolidation prompt. The latest recorded verification from audits 09 and 10 is:

- `dotnet build backend\EnterpriseTask\EnterpriseTask.slnx` - passed.
- `dotnet test backend\EnterpriseTask\EnterpriseTask.slnx --no-build` - exited successfully, but discovered no backend test projects.
- `npm.cmd run build` in `enterprise-task-ms` - passed.
- `npm.cmd test -- --watch=false` in `enterprise-task-ms` - failed at the stale generated title assertion in `src/app/app.spec.ts`.
- `npm.cmd run lint` in `enterprise-task-ms` - failed because no lint script is configured.

## Post-Roadmap Implementation Evidence

### P0-01 - Reproducible Configuration and Schema Lifecycle

Evidence added after the initial traceability audit:

- Backend migration contract: `backend/EnterpriseTask/EnterpriseTask.Application/Common/IDatabaseHealthCheck.cs` - `IDatabaseMigrator`, `DatabaseMigrationStatus`, `DatabaseMigrationResult`.
- Backend migrator: `backend/EnterpriseTask/EnterpriseTask.Infrastructure/Persistence/PostgresDatabaseMigrator.cs` - embedded SQL migration discovery, `public.schema_migrations`, existing-schema baseline detection.
- Initial forward migration: `backend/EnterpriseTask/EnterpriseTask.Infrastructure/Persistence/Migrations/0001_initial_schema.sql` - generated from `supabase_schema_v2_clean.sql` with destructive `DROP` block removed.
- Migration docs: `docs/database/README.md` and `backend/EnterpriseTask/EnterpriseTask.Infrastructure/Persistence/Migrations/README.md`.
- Sanitized config template: `backend/EnterpriseTask/EnterpriseTask.Api/appsettings.Local.example.json`.
- Dev-only API: `backend/EnterpriseTask/EnterpriseTask.Api/Controllers/DevelopmentController.cs` - `POST /api/dev/migrate`; `POST /api/dev/seed` remains no-op and creates no credentials.
- Health API: `backend/EnterpriseTask/EnterpriseTask.Api/Controllers/DatabaseHealthController.cs` and `PostgresDatabaseHealthCheck.cs` - returns `Misconfigured`, `Unhealthy`, or `Healthy` without exposing secrets.

Verification:

- `dotnet build backend\EnterpriseTask\EnterpriseTask.slnx` - passed.
- `dotnet test backend\EnterpriseTask\EnterpriseTask.slnx --no-build` - exited 0; no backend test projects are present yet.
- `npm.cmd run build` in `enterprise-task-ms` - passed.
- `npm.cmd test -- --watch=false` in `enterprise-task-ms` - still failed at the pre-existing stale `src/app/app.spec.ts` title assertion.
- Static migration safety check for `DROP TABLE|DROP TYPE|DROP FUNCTION|DROP VIEW|DROP TRIGGER` in `backend/EnterpriseTask/EnterpriseTask.Infrastructure/Persistence/Migrations/*.sql` - no matches.
- Runtime missing-DB health check with JWT config set and no connection string - `GET /api/health/database` returned HTTP 503 with `status = Misconfigured`.

Traceability note: this improves the migration/config blocker recorded in Prompt 09/12, but does not change any SRS row to `IMPLEMENTED` until migration smoke tests run against both an empty database and an existing EnterpriseTask schema.

### P0-02 - Automated Test Baseline

Evidence added after P0-01:

- Backend test project: `backend/EnterpriseTask/EnterpriseTask.Domain.Tests/EnterpriseTask.Domain.Tests.csproj` added to `backend/EnterpriseTask/EnterpriseTask.slnx`.
- Domain progress tests: `backend/EnterpriseTask/EnterpriseTask.Domain.Tests/Tasks/TaskProgressPolicyTests.cs` verifies `TaskProgressPolicy.Normalize()` clamps below `0`, preserves `0/50/100`, and clamps above `100`.
- Domain workflow tests: `backend/EnterpriseTask/EnterpriseTask.Domain.Tests/Tasks/TaskWorkflowPolicyTests.cs` verifies current allowed/disallowed `TaskWorkflowPolicy.CanTransition()` behavior and explicit closed-task reopen branch.
- Frontend bootstrap test: `enterprise-task-ms/src/app/app.spec.ts` now asserts the actual app shell renders `router-outlet` instead of the stale starter `h1`.
- Stable test commands documented in `README.md`.

Verification:

- `dotnet test backend\EnterpriseTask\EnterpriseTask.slnx` - restored and discovered `EnterpriseTask.Domain.Tests`; passed 26 tests.
- `dotnet test backend\EnterpriseTask\EnterpriseTask.slnx --no-restore` - passed again: 26 passed, 0 failed.
- `dotnet build backend\EnterpriseTask\EnterpriseTask.slnx --no-restore` - passed.
- `npm.cmd run build` in `enterprise-task-ms` - passed.
- `npm.cmd test -- --watch=false` in `enterprise-task-ms` - passed twice: 2 passed, 0 failed.

Traceability note: `TEST-01` improves from no executable backend tests and a failing frontend starter spec to a green baseline. It is still not fully `IMPLEMENTED` because API authorization/IDOR and database integration tests require a configured test database and are planned in later baseline expansion.

### P0-03 - Correct Task Status Semantics

Evidence added after P0-02:

- Backend canonical IDs: `backend/EnterpriseTask/EnterpriseTask.Domain/Tasks/TaskStatusIds.cs` now matches `public.task_statuses` seed order: `new=1`, `assigned=2`, `in_progress=3`, `pending_review=4`, `completed=5`, `closed=6`, `on_hold=7`, `cancelled=8`, `overdue=9`.
- Backend workflow policy: `backend/EnterpriseTask/EnterpriseTask.Domain/Tasks/TaskWorkflowPolicy.cs` no longer references non-seeded statuses such as accepted/rejected/waiting.
- Backend command behavior: `backend/EnterpriseTask/EnterpriseTask.Infrastructure/Tasks/PostgresTaskCommands.cs` validates status transitions through the canonical policy, checks `task.close`/`task.reopen` where applicable, returns `TaskCommandResult.Conflict` for invalid transitions, and sets `completed_at`/`closed_at` when status changes.
- API mapping: `backend/EnterpriseTask/EnterpriseTask.Api/Controllers/TasksController.cs` maps `TaskCommandResult.Conflict` to HTTP `409 Conflict`.
- Frontend constants: `enterprise-task-ms/src/app/core/constants/task-status.constants.ts` now uses the same canonical IDs and transitions as the backend.
- Frontend hardcoded overdue IDs were replaced with `TASK_STATUS_IDS.QUA_HAN` in task/project views.
- Tests: `backend/EnterpriseTask/EnterpriseTask.Domain.Tests/Tasks/TaskWorkflowPolicyTests.cs` now locks the PostgreSQL seed order and current transition matrix.

Verification:

- `dotnet test backend\EnterpriseTask\EnterpriseTask.slnx --no-restore` - passed: 42 tests.
- `dotnet build backend\EnterpriseTask\EnterpriseTask.slnx --no-restore` - passed.
- `npm.cmd run build` in `enterprise-task-ms` - passed.
- `npm.cmd test -- --watch=false` in `enterprise-task-ms` - passed: 2 tests.

Traceability note: the critical semantic ID mismatch in `TASK-03` is resolved in code and domain tests. `TASK-03` remains `PARTIAL` until a configured test database verifies status lookup IDs, timestamp updates, and `/api/tasks/{id}/status` conflict behavior end-to-end.

### P0-04 - Auth Hardening, Transactions and Error Contract

Evidence added after P0-03:

- Refresh-token persistence: `backend/EnterpriseTask/EnterpriseTask.Infrastructure/Persistence/Migrations/0002_auth_refresh_sessions.sql` adds `public.auth_refresh_sessions` with hashed token storage, family IDs, expiry, revocation and replacement tracking.
- Auth API contract: `backend/EnterpriseTask/EnterpriseTask.Api/Controllers/AuthController.cs` adds `POST /api/auth/refresh` and `POST /api/auth/logout`; login remains compatible and now also returns a refresh token.
- Auth service: `backend/EnterpriseTask/EnterpriseTask.Infrastructure/Auth/JwtAuthService.cs` issues random refresh tokens, stores SHA-256 hashes only, rotates refresh tokens on refresh, rejects revoked/expired/inactive sessions, and revokes a token family on logout.
- Rate limiting: `backend/EnterpriseTask/EnterpriseTask.Api/Program.cs` registers `AuthLogin` and `ApiMutation` rate-limit policies; auth and task/request mutation endpoints apply them with `[EnableRateLimiting]`.
- Transaction foundation: `backend/EnterpriseTask/EnterpriseTask.Infrastructure/Persistence/PostgresCommandBase.cs` adds `ExecuteInTransactionAsync`; command/query helpers attach the current EF transaction to raw SQL commands.
- Transaction adoption: `PostgresTaskCommands.cs` wraps multi-step create/update/status-with-note/transfer/duplicate/extension-review flows; `PostgresInterDepartmentRequestCommands.cs` wraps message insert plus request latest-message update.
- Error contract: `TasksController.cs` and `InterDepartmentRequestsController.cs` now return RFC-style `ProblemDetails` for mutation conflicts and not-found results instead of empty `409`/`404` responses.
- Frontend auth adoption: `enterprise-task-ms/src/app/core/interceptors/auth.interceptor.ts` retries one failed API request after `401` by calling `/api/auth/refresh`; `auth.service.ts` stores refresh tokens and calls logout revocation.
- Frontend mutation handling: `task.service.ts` and `inter-department-request.service.ts` expose `mutationError` signals and roll back optimistic task/request state when covered API mutations fail.
- Configuration/docs: README files, `docs/database/README.md`, migration README and local appsettings examples document `Auth:RefreshTokenDays`, Supabase auth config and hashed refresh-session storage.

Verification:

- `dotnet build backend\EnterpriseTask\EnterpriseTask.slnx --no-restore` - passed after backend transaction/rate-limit/refresh changes.
- `dotnet test backend\EnterpriseTask\EnterpriseTask.slnx --no-restore` - passed: 42 tests.
- `npm.cmd run build` in `enterprise-task-ms` - passed after auth interceptor refresh changes.
- `npm.cmd test -- --watch=false` in `enterprise-task-ms` - passed: 2 tests.
- `git diff --check` - no whitespace errors; Windows CRLF warnings only.

Traceability note: `AUTH-02`, `NFR-02` and `NFR-03` move materially forward: refresh rotation/revocation, login/mutation throttling, explicit transaction boundaries for covered multi-step writes, ProblemDetails conflict/not-found responses, and FE rollback/error signaling are implemented. They should remain `PARTIAL` rather than `IMPLEMENTED` until DB-backed integration tests prove refresh-token replay/logout behavior and induced rollback cases against a configured test database.

### P1-01A - Named Authorization Policy Foundation

Evidence added after P0-04:

- Policy constants: `backend/EnterpriseTask/EnterpriseTask.Application/Common/AuthorizationPolicyNames.cs` defines `AuthenticatedUser`, `AdminOnly`, `ElevatedDataReader`, and `DepartmentDataReader`.
- Role/permission constants: `RoleCodes.cs` and `PermissionCodes.cs` centralize canonical role and permission code strings used by JWT claims and SQL-backed permission checks.
- Policy registration: `backend/EnterpriseTask/EnterpriseTask.Api/Program.cs` registers named ASP.NET Core authorization policies for authenticated users, admin-only routes, elevated data readers, and department data readers.
- Claim parsing foundation: `backend/EnterpriseTask/EnterpriseTask.Api/Auth/ClaimsPrincipalScopeReader.cs` centralizes user id, department id, and role-code parsing for `UserScope`.
- Scope semantics: `backend/EnterpriseTask/EnterpriseTask.Application/Common/UserScope.cs` now builds scopes from exact canonical role codes instead of substring matching.
- Controller foundation adoption: current protected controllers use `[Authorize(Policy = AuthorizationPolicyNames.AuthenticatedUser)]` rather than unnamed `[Authorize]`.
- Permission usage: task command/handler code now references `PermissionCodes.*` instead of hard-coded permission strings.
- Tests: `backend/EnterpriseTask/EnterpriseTask.Domain.Tests/Common/UserScopeTests.cs` verifies canonical elevated role detection and rejects role-name fragments such as `not-admin`.

Verification:

- `dotnet test backend\EnterpriseTask\EnterpriseTask.slnx --no-restore` - passed: 44 tests.
- `dotnet build backend\EnterpriseTask\EnterpriseTask.slnx --no-restore` - passed.

Traceability note: this completes the low-risk policy foundation slice only. `P1-01` remains incomplete until the scope/IDOR regression suite and deeper controller/resource-policy adoption are implemented in follow-up slices.

### P1-01B - Task Scope Regression Tests

Evidence added after P1-01A:

- Domain task-scope contract: `backend/EnterpriseTask/EnterpriseTask.Domain/Tasks/TaskScopeContext.cs` captures the actor, elevated/data-scope flags, department grants, task relationships and confidentiality flag needed to evaluate task visibility.
- Domain task-scope policy: `backend/EnterpriseTask/EnterpriseTask.Domain/Tasks/TaskScopePolicy.cs` codifies the current backend SQL rule: admin/director can read all; creator/reporter/assignee can read related tasks including confidential tasks; managers can read non-confidential tasks in their own or granted departments; unrelated employees are denied.
- Regression tests: `backend/EnterpriseTask/EnterpriseTask.Domain.Tests/Tasks/TaskScopePolicyTests.cs` covers elevated confidential access, creator/reporter/assignee confidential access, manager own-department access, scoped-manager granted-department access, manager denial for confidential department-only access and unrelated employee denial.

Verification:

- `dotnet test backend\EnterpriseTask\EnterpriseTask.slnx --no-restore` - passed: 53 tests.
- `dotnet build backend\EnterpriseTask\EnterpriseTask.slnx --no-restore` - passed.

Traceability note: this gives `RBAC-02`, `TASK-05` and `NFR-02` executable regression coverage for the intended task-scope semantics without requiring a live database. `P1-01` remains `PARTIAL` until API/DB-backed IDOR tests prove that `PostgresTaskQueries`, `PostgresTaskAccessReader` and task mutation endpoints enforce the same policy against real rows.

### P1-01C - Controller Adoption

Evidence added after P1-01B:

- Permission abstraction: `backend/EnterpriseTask/EnterpriseTask.Application/Common/IPermissionChecker.cs` defines a controller-safe permission lookup contract.
- PostgreSQL permission checker: `backend/EnterpriseTask/EnterpriseTask.Infrastructure/Auth/PostgresPermissionChecker.cs` checks `user_roles`/`role_permissions`/`permissions` without exposing database concerns to API controllers.
- Authorization handler: `backend/EnterpriseTask/EnterpriseTask.Api/Auth/PermissionRequirement.cs` and `PermissionAuthorizationHandler.cs` allow ASP.NET Core authorization policies to enforce database-backed permission codes.
- Policy registration: `backend/EnterpriseTask/EnterpriseTask.Api/Program.cs` registers task-create, task-update, task-assign and comment-create policies.
- Controller adoption: `backend/EnterpriseTask/EnterpriseTask.Api/Controllers/TasksController.cs` now uses explicit permission policies on task mutation endpoints while retaining command-level resource/scope checks.

Verification:

- `dotnet test backend\EnterpriseTask\EnterpriseTask.slnx --no-restore` - passed: 53 tests.
- `dotnet build backend\EnterpriseTask\EnterpriseTask.slnx --no-restore` - passed.

Traceability note: task controller mutations now fail earlier at the ASP.NET policy layer when the actor lacks the required permission, and command handlers still enforce resource-level access/IDOR checks. Inter-department request mutation policies are intentionally not adopted yet because `inter_request.create` and `inter_request.process` are not seeded in the current baseline migration; adopting them requires a later permission seed/backfill migration.

### P1-02A - User and Role Read/Admin Foundation

Evidence added after P1-01C:

- Common paging contract: `backend/EnterpriseTask/EnterpriseTask.Application/Common/PagedResult.cs` defines a reusable API page response shape.
- User query contract: `backend/EnterpriseTask/EnterpriseTask.Application/Users/IUserQueries.cs` and `UserDtos.cs` define paged user listing, user detail and filter DTOs.
- Role query contract: `backend/EnterpriseTask/EnterpriseTask.Application/Roles/IRoleQueries.cs` and `RoleDtos.cs` define role and permission read models.
- User read implementation: `backend/EnterpriseTask/EnterpriseTask.Infrastructure/Users/PostgresUserQueries.cs` reads `profiles`, departments, managers, role codes/names and department scopes with search/filter/paging.
- Role read implementation: `backend/EnterpriseTask/EnterpriseTask.Infrastructure/Roles/PostgresRoleQueries.cs` reads roles and attached permissions from `roles`, `permissions` and `role_permissions`.
- Admin APIs: `backend/EnterpriseTask/EnterpriseTask.Api/Controllers/UsersController.cs` adds admin-only `GET /api/users` and `GET /api/users/{id}`; `RolesController.cs` adds admin-only `GET /api/roles` and `GET /api/roles/permissions`.
- Frontend contracts: `enterprise-task-ms/src/app/core/services/user.service.ts`, `role.service.ts`, `user.model.ts` and `role.model.ts` now expose admin read services/models against the new APIs.

Verification:

- `dotnet test backend\EnterpriseTask\EnterpriseTask.slnx --no-restore` - passed: 53 tests.
- `dotnet build backend\EnterpriseTask\EnterpriseTask.slnx --no-restore` - passed.
- `npm.cmd run build` in `enterprise-task-ms` - passed.
- `npm.cmd test -- --watch=false` in `enterprise-task-ms` - passed: 2 tests.

Traceability note: `USER-01`, `RBAC-01` and `RBAC-02` move from missing user/role API contracts to a read-only admin foundation. They remain `PARTIAL`: create/update, lock/unlock, role grants, department-scope assignment, last-admin invariant, token invalidation and audit writes are intentionally deferred to later P1-02 slices.

### P1-02B - User Lock/Unlock and Active Session Enforcement

Evidence added after P1-02A:

- Lock invariant policy: `backend/EnterpriseTask/EnterpriseTask.Domain/Users/UserLockPolicy.cs` prevents self-lock and locking the last active administrator.
- Policy tests: `backend/EnterpriseTask/EnterpriseTask.Domain.Tests/Users/UserLockPolicyTests.cs` covers self-lock denial, last-admin denial, non-last-admin lock and unlock allowance.
- User admin command contract: `backend/EnterpriseTask/EnterpriseTask.Application/Users/IUserAdministrationCommands.cs` defines active-state mutation results.
- User admin command implementation: `backend/EnterpriseTask/EnterpriseTask.Infrastructure/Users/PostgresUserAdministrationCommands.cs` updates `profiles.is_active`, revokes active refresh sessions when locking a user and applies the lock invariants.
- Active session validator: `backend/EnterpriseTask/EnterpriseTask.Application/Auth/IUserSessionValidator.cs` and `backend/EnterpriseTask/EnterpriseTask.Infrastructure/Auth/PostgresUserSessionValidator.cs` verify `profiles.is_active` during JWT validation.
- JWT enforcement: `backend/EnterpriseTask/EnterpriseTask.Api/Program.cs` now fails bearer-token validation when the authenticated profile is missing or inactive.
- Admin APIs: `backend/EnterpriseTask/EnterpriseTask.Api/Controllers/UsersController.cs` adds admin-only `POST /api/users/{id}/lock` and `POST /api/users/{id}/unlock`.
- Frontend service methods: `enterprise-task-ms/src/app/core/services/user.service.ts` exposes `lockUser()` and `unlockUser()` and updates local admin user state.

Verification:

- `dotnet test backend\EnterpriseTask\EnterpriseTask.slnx --no-restore` - passed: 57 tests.
- `dotnet build backend\EnterpriseTask\EnterpriseTask.slnx --no-restore` - passed.
- `npm.cmd run build` in `enterprise-task-ms` - passed.
- `npm.cmd test -- --watch=false` in `enterprise-task-ms` - passed: 2 tests.

Traceability note: locked users are now blocked at login, refresh and bearer-token validation; locking also revokes outstanding refresh sessions. `USER-01`, `RBAC-01` and `NFR-02` remain `PARTIAL` until DB/API integration tests prove lock/logout behavior against Supabase data and until role grants, department scopes and audit writes are implemented in later P1-02 slices.

### P1-02C - Role Grants

Evidence added after P1-02B:

- Role-grant invariant policy: `backend/EnterpriseTask/EnterpriseTask.Domain/Users/UserRoleGrantPolicy.cs` prevents removing the `admin` role from the last active administrator.
- Policy tests: `backend/EnterpriseTask/EnterpriseTask.Domain.Tests/Users/UserRoleGrantPolicyTests.cs` verifies non-admin role removal, last-active-admin denial and admin-role removal when another active admin exists.
- User administration contract: `backend/EnterpriseTask/EnterpriseTask.Application/Users/IUserAdministrationCommands.cs` adds assign/remove role commands and a role assignment request DTO.
- PostgreSQL implementation: `backend/EnterpriseTask/EnterpriseTask.Infrastructure/Users/PostgresUserAdministrationCommands.cs` inserts/deletes `user_roles`, validates user/role existence, preserves the last-admin invariant and revokes refresh sessions after role changes.
- JWT role invalidation: `IUserSessionValidator`, `PostgresUserSessionValidator`, `ClaimsPrincipalScopeReader` and `Program.cs` now compare role claims against current database roles during bearer-token validation, so role changes invalidate stale access tokens on the next request.
- Admin APIs: `backend/EnterpriseTask/EnterpriseTask.Api/Controllers/UsersController.cs` adds admin-only `POST /api/users/{id}/roles` and `DELETE /api/users/{id}/roles/{roleId}`.
- Frontend service methods: `enterprise-task-ms/src/app/core/services/user.service.ts` exposes `assignRole()` and `removeRole()` for the future admin UI.

Verification:

- `dotnet test backend\EnterpriseTask\EnterpriseTask.slnx --no-restore` - passed: 60 tests.
- `dotnet build backend\EnterpriseTask\EnterpriseTask.slnx --no-restore` - passed.
- `npm.cmd run build` in `enterprise-task-ms` - passed.
- `npm.cmd test -- --watch=false` in `enterprise-task-ms` - passed: 2 tests.

Traceability note: role grants are now implemented at API/service level with last-admin protection and stale-token invalidation. `RBAC-01`, `RBAC-02` and `NFR-02` remain `PARTIAL` until DB/API integration tests prove assign/remove behavior against Supabase data and until department-scope assignment, audit writes and admin UI are completed in later P1-02 slices.

### P1-02D - Department Scopes

Evidence added after P1-02C:

- User administration contract: `backend/EnterpriseTask/EnterpriseTask.Application/Users/IUserAdministrationCommands.cs` adds assign/remove department-scope commands and `UserDepartmentScopeAssignmentRequest`.
- PostgreSQL implementation: `backend/EnterpriseTask/EnterpriseTask.Infrastructure/Users/PostgresUserAdministrationCommands.cs` inserts/deletes `user_department_scopes`, validates target user existence, validates active departments on assignment and revokes refresh sessions after scope changes.
- Admin APIs: `backend/EnterpriseTask/EnterpriseTask.Api/Controllers/UsersController.cs` adds admin-only `POST /api/users/{id}/department-scopes` and `DELETE /api/users/{id}/department-scopes/{departmentId}`.
- Frontend service methods: `enterprise-task-ms/src/app/core/services/user.service.ts` exposes `assignDepartmentScope()` and `removeDepartmentScope()` and updates local scoped department ids for future admin UI.

Verification:

- `dotnet test backend\EnterpriseTask\EnterpriseTask.slnx --no-restore` - passed: 60 tests.
- `dotnet build backend\EnterpriseTask\EnterpriseTask.slnx --no-restore` - passed.
- `npm.cmd run build` in `enterprise-task-ms` - passed.
- `npm.cmd test -- --watch=false` in `enterprise-task-ms` - passed: 2 tests.

Traceability note: department-scope assignment is now implemented at API/service level and uses the existing `user_department_scopes` schema, so no migration is required. `RBAC-02` remains `PARTIAL` until DB/API integration tests prove scope mutations against Supabase data and until audit writes/admin UI are completed.

### P1-02E - Frontend Admin UI

Evidence added after P1-02D:

- Admin route guard: `enterprise-task-ms/src/app/core/guards/admin.guard.ts` restricts admin screens to users whose current auth role includes `admin`.
- Auth role helpers: `enterprise-task-ms/src/app/core/services/auth.service.ts` adds exact role-code parsing helpers used by the admin guard/sidebar.
- Admin route/sidebar: `enterprise-task-ms/src/app/app.routes.ts` adds lazy route `/admin/users`; `layout/sidebar/sidebar.component.*` adds an admin-only navigation group.
- Admin user screen: `enterprise-task-ms/src/app/features/admin/users/admin-users.component.*` adds a user administration UI for loading/filtering users, lock/unlock, assign/remove roles and assign/remove department scopes.
- PrimeNG admin table: `enterprise-task-ms/package.json`, `package-lock.json`, `src/app/app.config.ts`, `src/styles.scss` and `admin-users.component.*` add PrimeNG/Aura/PrimeIcons integration, render the admin user list with `p-table`, enable server-side lazy pagination with rows-per-page controls, and use `p-select`, `p-tag` and `p-button` for compact management actions.
- Self-management guard: `admin-users.component.*` disables lock/unlock, role changes and department-scope changes for the currently authenticated admin user to avoid invalidating the active token during normal UI use.
- Department options: `GET /api/departments/options` was added through `DepartmentsController`, `IDepartmentQueries`, `DepartmentOptionDto`, `PostgresDepartmentQueries`, `department-card.model.ts` and `DepartmentService` so the admin UI can select active departments by id/name.

Verification:

- `dotnet build backend\EnterpriseTask\EnterpriseTask.slnx --no-restore` - passed.
- `dotnet test backend\EnterpriseTask\EnterpriseTask.slnx --no-restore` - passed: 60 tests.
- `npm.cmd run build` in `enterprise-task-ms` - passed; `admin-users-component` is emitted as a lazy chunk. The PrimeNG integration currently triggers a production initial bundle budget warning (`578.83 kB` vs `500 kB` warning budget), but it does not fail the build.
- `npm.cmd test -- --watch=false` in `enterprise-task-ms` - passed: 2 tests.

Traceability note: P1-02 now has read/admin APIs, lock/unlock, role grants, department scopes and a frontend administration screen. `USER-01`, `RBAC-01`, `RBAC-02` and `NFR-02` remain `PARTIAL` until runtime DB/API tests are performed against Supabase, audit writes are introduced, and a future slice covers user profile create/update details if required.

### P1-03A - Department Read Contract and Tree Foundation

Evidence added after P1-02E:

- Department read DTOs: `backend/EnterpriseTask/EnterpriseTask.Application/Departments/DepartmentDtos.cs` adds `DepartmentListItemDto` and `DepartmentTreeNodeDto` with hierarchy, manager, active-state, member-count and active-task-count fields.
- Department query contract: `backend/EnterpriseTask/EnterpriseTask.Application/Departments/IDepartmentQueries.cs` adds `GetListAsync()` and `GetTreeAsync()` with `includeInactive` support.
- PostgreSQL implementation: `backend/EnterpriseTask/EnterpriseTask.Infrastructure/Departments/PostgresDepartmentQueries.cs` reads department list data from `departments`, parent departments, manager profiles, active members and active task counts, then builds a defensive tree shape for admin hierarchy screens.
- Admin read APIs: `backend/EnterpriseTask/EnterpriseTask.Api/Controllers/DepartmentsController.cs` adds admin-only `GET /api/departments?includeInactive=` and `GET /api/departments/tree?includeInactive=` while preserving existing `/cards` and `/options`.
- Frontend contract: `enterprise-task-ms/src/app/core/models/department-card.model.ts` adds `DepartmentListItem` and `DepartmentTreeNode`; `DepartmentService` adds `departmentList`, `departmentTree`, `loadAdminList()` and `loadAdminTree()` for later admin UI adoption.

Verification:

- `dotnet build backend\EnterpriseTask\EnterpriseTask.slnx --no-restore` - passed; existing NU1903 warnings remain in the test project.
- `dotnet test backend\EnterpriseTask\EnterpriseTask.slnx --no-restore` - passed: 60 tests.
- `npm.cmd run build` in `enterprise-task-ms` - passed; existing PrimeNG initial bundle budget warning remains.
- `npm.cmd test -- --watch=false` in `enterprise-task-ms` - passed: 2 tests.

Traceability note: `DEPT-01` now has an admin read/list/tree contract suitable for hierarchy management UI and command work. It remains `PARTIAL` until create/update/deactivate/manager assignment endpoints, hierarchy-cycle checks, scope/authorization tests and the department management UI are implemented in later P1-03 slices.

### P1-03B - Department Commands and Invariants

Evidence added after P1-03A:

- Domain hierarchy policy: `backend/EnterpriseTask/EnterpriseTask.Domain/Departments/DepartmentHierarchyPolicy.cs` defines parent-assignment and deactivation decisions for self-parent, cycle, active-task and active-child-department cases.
- Domain tests: `backend/EnterpriseTask/EnterpriseTask.Domain.Tests/Departments/DepartmentHierarchyPolicyTests.cs` covers self-parent denial, descendant-as-parent cycle denial, null-parent allowance, active-task deactivation denial, active-child deactivation denial and safe deactivation allowance.
- Department command contract: `backend/EnterpriseTask/EnterpriseTask.Application/Departments/IDepartmentAdministrationCommands.cs` defines create, update, manager assignment and deactivate command APIs plus request/result DTOs.
- PostgreSQL command implementation: `backend/EnterpriseTask/EnterpriseTask.Infrastructure/Departments/PostgresDepartmentAdministrationCommands.cs` implements create/update/deactivate/manager assignment, validates company, same-company active parent departments, active managers, duplicate company code/name conflicts and hierarchy cycles, and blocks deactivation when active tasks or active child departments exist.
- API adoption: `backend/EnterpriseTask/EnterpriseTask.Api/Controllers/DepartmentsController.cs` adds admin-only, rate-limited `POST /api/departments`, `PUT /api/departments/{id}`, `PUT /api/departments/{id}/manager` and `POST /api/departments/{id}/deactivate`, with validation and ProblemDetails-style conflict/not-found responses.
- DI registration: `backend/EnterpriseTask/EnterpriseTask.Infrastructure/DependencyInjection.cs` registers `IDepartmentAdministrationCommands`.
- Frontend service contract: `enterprise-task-ms/src/app/core/models/department-card.model.ts` and `DepartmentService` add create/update/assign-manager/deactivate request models and service methods for later department admin UI adoption.

Verification:

- `dotnet build backend\EnterpriseTask\EnterpriseTask.slnx --no-restore` - passed; existing NU1903 warnings remain in the test project.
- `dotnet test backend\EnterpriseTask\EnterpriseTask.slnx --no-restore` - passed: 66 tests.
- `npm.cmd run build` in `enterprise-task-ms` - passed; existing PrimeNG initial bundle budget warning remains.
- `npm.cmd test -- --watch=false` in `enterprise-task-ms` - passed: 2 tests.

Traceability note: `DEPT-01` now has backend mutation APIs and core hierarchy/deactivation invariants. It remains `PARTIAL` until DB/API integration tests prove the SQL behavior against Supabase data, manager assignment scope rules are refined if required, mutations are audited, and a frontend department management UI is implemented in later P1-03 slices.

### P1-03C - Department Policy Tests

Evidence added after P1-03B:

- Manager assignment policy: `backend/EnterpriseTask/EnterpriseTask.Domain/Departments/DepartmentManagerAssignmentPolicy.cs` defines the domain decision for clearing a manager, assigning an active manager and rejecting unavailable managers.
- Manager policy tests: `backend/EnterpriseTask/EnterpriseTask.Domain.Tests/Departments/DepartmentManagerAssignmentPolicyTests.cs` covers clear-manager, unavailable-manager denial and active-manager allowance.
- Expanded hierarchy tests: `DepartmentHierarchyPolicyTests.cs` now also covers non-descendant parent allowance, create-with-parent allowance and active-task deactivation precedence when active child departments also exist.
- Production adoption: `backend/EnterpriseTask/EnterpriseTask.Infrastructure/Departments/PostgresDepartmentAdministrationCommands.cs` now uses the manager assignment policy for create and manager-assignment commands instead of embedding that decision inline.

Verification:

- `dotnet build backend\EnterpriseTask\EnterpriseTask.slnx --no-restore` - passed; existing NU1903 warnings remain in the test project.
- `dotnet test backend\EnterpriseTask\EnterpriseTask.slnx --no-restore` - passed on rerun: 72 tests. A previous parallel build/test attempt hit a DLL file lock, not a test failure.

Traceability note: P1-03 now has executable domain coverage for hierarchy parent decisions, cycle prevention, safe deactivation decisions and manager assignment availability. `DEPT-01` remains `PARTIAL` until DB/API integration tests exercise these policies through real PostgreSQL rows and HTTP endpoints, and until the department management UI is implemented.

### P1-03D - Frontend Admin Department UI

Evidence added after P1-03C:

- Admin route: `enterprise-task-ms/src/app/app.routes.ts` adds admin-guarded lazy route `/admin/departments`.
- Admin navigation: `enterprise-task-ms/src/app/layout/sidebar/sidebar.component.ts` adds an admin-only department management menu item.
- Admin department screen: `enterprise-task-ms/src/app/features/admin/departments/admin-departments.component.*` adds a PrimeNG-based administration screen with summary cards, create/update form, parent department selector, manager selector, include-inactive toggle and a paginated department table.
- Department command adoption: the UI calls the P1-03B service methods in `DepartmentService` for create, update, manager assignment and deactivate, then refreshes the admin list/tree contracts.
- Backend protection remains authoritative: the frontend excludes the current department from the parent selector for usability, while backend P1-03B invariants still enforce self-parent and cycle prevention.

Verification:

- `npm.cmd run build` in `enterprise-task-ms` - passed; `admin-departments-component` is emitted as a lazy chunk. The existing PrimeNG initial bundle budget warning remains (`579.06 kB` vs `500 kB` warning budget).
- `npm.cmd test -- --watch=false` in `enterprise-task-ms` - passed: 2 tests.

Traceability note: `DEPT-01` now has backend read/write contracts, policy tests and a frontend admin department management screen. It remains `PARTIAL` until DB/API integration tests prove the hierarchy and deactivation behavior against Supabase data, mutations are audited, and the UI is exercised in a browser/E2E flow.

## Overall Verdict

The repository is a credible full-stack foundation and supports a focused task/request demo, but it is not yet an end-to-end implementation of the SRS. The most valuable next work is to add DB-backed integration tests for auth refresh/replay, transaction rollback and authorization scope, then complete server-side list contracts. Realtime/jobs/files/reports and AI should remain explicitly marked as roadmap capabilities until runtime paths and tests exist.
