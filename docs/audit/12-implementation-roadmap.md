# Prompt 12 - Implementation Roadmap

## Purpose and Planning Rules

This roadmap converts `docs/audit/11-srs-traceability-matrix.md` into safe, reviewable implementation increments. It uses evidence from audits 01-11, preserves the current ASP.NET Core/Angular/PostgreSQL stack and layered architecture, and does not implement any feature.

- Sizes are relative: S (1-2 focused days), M (3-5), L (1-2 weeks), XL (multi-sprint or external integration).
- Each item should be one pull request unless its commit boundary says otherwise.
- New modules named below are proposals. Exact filenames must be selected only after inspecting the neighboring module convention during implementation.
- Every database change must be incremental and forward-only; do not reuse the destructive clean-schema script as a production migration.
- Authorization is enforced by the API/database path. Frontend gating is usability only.
- A work item is not done when only schema, API, or UI exists; its stated acceptance path and tests must pass.

## Delivery Order and Gates

| Gate | Exit condition |
| --- | --- |
| Phase 0 | Reproducible configuration/schema, green baseline tests, corrected status semantics, transactions and critical auth controls |
| Phase 1 | Core administration and task/subtask workflows are API-backed, scoped and tested |
| Phase 2 | Collaboration/file/audit/notification paths are persistent, authorized and observable |
| Phase 3 | Time-driven operations, server aggregates and exports work at bounded data volume |
| Phase 4 | AI MVP is draft/suggestion-only, access checked, redacted, failure tolerant and explicitly confirmed |
| Phase 5 | Advanced AI builds on authorized search and the Phase 4 provider/safety foundation |

Do not start Phase 4 before Phase 0 authorization tests and Phase 2 audit controls pass. Do not start AI-06 chatbot before AI-05 authorized semantic retrieval is complete.

## Phase 0 - Stabilization

### P0-01 - Reproducible Configuration and Schema Lifecycle

- **Goal:** Make a clean environment start predictably without committed secrets or destructive schema resets.
- **SRS:** NFR-01, NFR-02, TEST-01.
- **Current gap:** Startup requires external DB/JWT values; `supabase_schema_v2_clean.sql` is destructive; no incremental migration history; `DatabaseSeeder` throws.
- **Backend likely affected:** `EnterpriseTask.Api/Program.cs`, `appsettings*.json`, `EnterpriseTask.Infrastructure/Persistence/ApplicationDbContext*.cs`, `EnterpriseTask.Infrastructure/Development/DatabaseSeeder.cs`, `EnterpriseTask.Infrastructure/DependencyInjection.cs`.
- **Frontend likely affected:** `enterprise-task-ms/src/app/core/constants/app.constants.ts` only if API base configuration is externalized.
- **DB/migration:** Establish a versioned migration mechanism compatible with the current raw-SQL services; baseline the existing schema without dropping data; document local seed behavior.
- **API contract:** Preserve existing routes; health/readiness must distinguish invalid config from DB connectivity failure without exposing secrets.
- **Authorization:** Development seed must be disabled outside Development and must not create a known production credential.
- **Acceptance:** A documented clean setup applies migrations once, reapplies idempotently, starts API/FE, and `/api/health/database` reports correctly.
- **Tests:** Migration smoke test on empty and current schema; configuration validation tests; production-environment seed denial.
- **Dependencies:** None.
- **Risk:** High; baselining a live schema incorrectly can lose data.
- **Estimated size:** L.
- **Definition of Done:** Forward migration, rollback/recovery notes, sanitized config template and setup documentation are reviewed; builds and migration tests pass.
- **Suggested commit boundary:** Migration foundation + config docs; keep feature schema changes out.

### P0-02 - Automated Test Baseline

- **Goal:** Create a green, executable safety net before business changes.
- **SRS:** TEST-01, RBAC-02, TASK-05, NFR-02.
- **Current gap:** No backend test project/E2E harness; `app.spec.ts` has a stale failing assertion.
- **Backend likely affected:** Solution plus proposed Domain/Application/API integration test projects; existing `TaskWorkflowPolicy`, `TaskProgressPolicy`, handlers and controllers are subjects, not production refactors.
- **Frontend likely affected:** `enterprise-task-ms/src/app/app.spec.ts`; proposed focused component tests.
- **DB/migration:** Test database fixture only; no production schema change.
- **API contract:** Lock existing auth/task/request response behavior in integration tests.
- **Authorization:** Fixtures must cover employee, manager, scoped manager, admin, unrelated user and confidential task.
- **Acceptance:** `dotnet test` discovers tests; Angular tests are green; one API IDOR test proves an unrelated actor cannot read/mutate a task.
- **Tests:** Domain workflow/progress; application handler; API auth/scope; frontend bootstrap/login smoke.
- **Dependencies:** P0-01 for repeatable integration DB.
- **Risk:** Medium; brittle fixtures can slow delivery.
- **Estimated size:** L.
- **Definition of Done:** Stable commands are documented and all baseline suites pass twice from clean state.
- **Suggested commit boundary:** Test infrastructure and baseline tests only.

### P0-03 - Correct Task Status Semantics

- **Goal:** Eliminate the critical mismatch between C# numeric status IDs and SQL seed ordering.
- **SRS:** TASK-03, JOB-02.
- **Current gap:** `TaskStatusIds`/`TaskWorkflowPolicy` IDs conflict with seeded lookup rows; reopen is unreachable; `closed_at` is not reliably maintained.
- **Backend likely affected:** `EnterpriseTask.Domain/Tasks/TaskStatusIds.cs`, `TaskWorkflowPolicy.cs`, `EnterpriseTask.Application/Tasks/UpdateTaskStatusHandler.cs`, `EnterpriseTask.Infrastructure/Tasks/PostgresTaskCommands.cs`, `PostgresTaskQueries.cs`.
- **Frontend likely affected:** `core/constants/task-status.constants.ts`, task models and task-detail action controls.
- **DB/migration:** Stabilize immutable status codes/IDs and closure timestamps with an incremental migration if data differs.
- **API contract:** Keep `PATCH /api/tasks/{id}/status`, but accept/return canonical status code or stable ID; invalid transition returns 409, authorization failure 403.
- **Authorization:** `task.update` for normal transitions; explicit `task.close`/`task.reopen`; overdue reserved for system actor/job.
- **Acceptance:** Every allowed/denied transition matches SRS and DB values; close sets `closed_at`; permitted reopen clears it.
- **Tests:** Exhaustive transition matrix, seeded-status integration test, controller 403/409 mapping, FE action visibility.
- **Dependencies:** P0-02; P0-01 if migration required.
- **Risk:** Critical; existing records may use current IDs.
- **Estimated size:** L.
- **Definition of Done:** No hard-coded semantic drift remains and all transition tests pass against migrated data.
- **Suggested commit boundary:** Status migration/domain/API/FE contract in one atomic commit or migration commit followed immediately by compatible code commit.

### P0-04 - Auth Hardening, Transactions and Error Contract

- **Goal:** Close immediate session/brute-force/partial-write risks and stop silent mutation failures.
- **SRS:** AUTH-02, NFR-02, NFR-03.
- **Current gap:** No refresh/revoke/rate limit; multi-step raw SQL has no explicit transaction; FE catches and ignores mutation errors.
- **Backend likely affected:** `Program.cs`, `AuthController.cs`, `IAuthService.cs`, `JwtAuthService.cs`, persistence command bases, `PostgresTaskCommands.cs`, `PostgresInterDepartmentRequestCommands.cs`.
- **Frontend likely affected:** `auth.service.ts`, `auth.interceptor.ts`, task/request services and affected components' error states.
- **DB/migration:** Refresh-session/revocation table and indexes; no secrets stored in plaintext; transaction changes need no schema migration.
- **API contract:** `POST /api/auth/refresh`, `POST /api/auth/logout`; RFC-style consistent validation/conflict/problem responses; retain login shape compatibly.
- **Authorization:** Refresh tokens rotate once; logout revokes token family; rate limit login by IP/account and mutations by actor; commands retain scope checks inside transactions.
- **Acceptance:** Reuse of rotated token fails; login throttles; induced second-step failure rolls back the first write; FE rolls back optimistic state and shows error.
- **Tests:** Refresh rotation/replay/logout, rate-limit, transaction rollback, problem response and FE failure-state tests.
- **Dependencies:** P0-01, P0-02.
- **Risk:** Critical; token migration and client rollout must be coordinated.
- **Estimated size:** XL.
- **Definition of Done:** Security tests pass, transaction boundaries are explicit, logs contain no tokens, and no covered mutation swallows errors.
- **Suggested commit boundary:** Prefer three reviewable commits: transaction/error foundation, refresh lifecycle, FE adoption; deploy as one compatible release.

## Phase 1 - Core

### P1-01 - Named Authorization Policies and Scope Regression Suite

- **Goal:** Make authorization rules consistent and independently testable across APIs.
- **SRS:** RBAC-02, TASK-05, NFR-02.
- **Current gap:** Generic `[Authorize]` plus per-command SQL checks; no named policies or broad IDOR suite.
- **Backend likely affected:** `Program.cs`, `ControllerScopeExtensions.cs`, `HttpCurrentUserContext.cs`, task/request access and policy query classes, controllers.
- **Frontend likely affected:** `role.guard.ts`, `auth.service.ts`, routes only for UX alignment.
- **DB/migration:** No required schema change; validate RLS/helper behavior under backend connection context.
- **API contract:** Existing endpoints retain shapes; 401, 403 and 404 concealment rules become documented and consistent.
- **Authorization:** Named permission policies plus resource-level self/related/department/all/confidential checks.
- **Acceptance:** A permission/scope matrix passes for list and direct mutation attempts across roles.
- **Tests:** API integration matrix including confidential and cross-department IDOR; DB-context/RLS integration tests.
- **Dependencies:** Phase 0.
- **Risk:** Critical; response changes can affect FE error handling.
- **Estimated size:** L.
- **Definition of Done:** Each protected route has an explicit policy/resource check and negative tests.
- **Suggested commit boundary:** Policy infrastructure and tests, then controller adoption by bounded module.

### P1-02 - User, Role and Permission Administration

- **Goal:** Manage users, lock state, roles, grants and department scopes through the product.
- **SRS:** USER-01, RBAC-01, RBAC-02.
- **Current gap:** Tables/seeds exist; no commands/controllers/UI; empty `user.service.ts` and `role.service.ts`.
- **Backend likely affected:** Proposed Application/Infrastructure Users and Roles modules, new controllers, `DependencyInjection.cs`; `JwtAuthService.cs` for active-user enforcement.
- **Frontend likely affected:** Existing empty user/role services, `role.guard.ts`, `app.routes.ts`, sidebar; proposed admin components.
- **DB/migration:** Audit metadata/indexes or token invalidation version if required; preserve existing role/permission seed keys.
- **API contract:** Paged `/api/users`, `/api/roles`, role grants, user-role and department-scope assignment, lock/unlock endpoints.
- **Authorization:** Admin-only grants/users; prevent last-admin removal and self-lock; existing tokens for locked users become invalid.
- **Acceptance:** Admin can create/update/lock user and assign role/scope; non-admin gets 403; every change is audited.
- **Tests:** CRUD validation, last-admin invariant, lock/session invalidation, grant/scope IDOR and admin UI tests.
- **Dependencies:** P1-01, P2-03 audit service contract may be introduced minimally here then completed there.
- **Risk:** High; identity ownership between Supabase Auth and profiles must be explicit.
- **Estimated size:** XL.
- **Definition of Done:** API/UI/schema/audit/tests are complete and no production password is managed insecurely.
- **Suggested commit boundary:** User lifecycle and role/grant management as separate commits/PRs.
- **Implementation note:** P1-02E frontend administration UI currently uses PrimeNG `p-table` with server-side lazy pagination, rows-per-page controls, `p-select` role/scope assignment controls and `p-tag`/`p-button` status/actions. Build passes, but the PrimeNG integration increases the production initial bundle beyond the current warning budget; budget tuning or further optimization remains a follow-up.

### P1-03 - Department Hierarchy Management

- **Goal:** Add authorized department CRUD, hierarchy and manager assignment.
- **SRS:** DEPT-01.
- **Current gap:** `GET /api/departments/cards` is read-only and does not consistently exclude inactive rows.
- **Backend likely affected:** `DepartmentsController.cs`, Application Department contracts, `PostgresDepartmentQueries.cs`; proposed command class; DI.
- **Frontend likely affected:** `department.service.ts`, department component, app route/menu if edit screen is separated.
- **DB/migration:** Add/validate indexes and hierarchy-cycle constraint strategy; use inactive state instead of destructive delete.
- **API contract:** Paged tree/list, create/update/deactivate and manager assignment endpoints.
- **Authorization:** Admin manages hierarchy; director/authorized manager reads within scope; prevent cross-scope manager assignment.
- **Acceptance:** Hierarchy renders, cycles are rejected, inactive departments cannot receive new tasks, manager changes are audited.
- **Tests:** Cycle, scope, deactivate-in-use, manager assignment and UI form tests.
- **Dependencies:** P1-01; P1-02 for user selection.
- **Risk:** Medium.
- **Estimated size:** L.
- **Definition of Done:** End-to-end management works without breaking department cards/task form options.
- **Suggested commit boundary:** Backend/schema tests, then UI integration.
- **Implementation note:** P1-03A is implemented as an admin-only read foundation: `GET /api/departments` and `GET /api/departments/tree` return flat and hierarchy-ready contracts with parent, manager, active state and aggregate counts. Mutation commands, cycle/deactivation invariants and UI remain follow-up P1-03 slices.
- **Implementation note:** P1-03B adds admin-only create/update/manager/deactivate APIs and backend invariants for self-parent, hierarchy cycle prevention, active-manager validation, duplicate company code/name conflicts and safe deactivation. UI, audit writes and DB/API integration tests remain follow-up slices.
- **Implementation note:** P1-03C expands domain policy coverage for allowed/denied parent assignments, deactivation precedence and manager assignment availability. DB/API integration tests and frontend management UI remain follow-up slices.
- **Implementation note:** P1-03D adds an admin-guarded `/admin/departments` PrimeNG UI for create/update, parent selection, manager assignment, deactivate action, include-inactive filtering and paginated department management. Audit writes, DB/API integration tests and browser/E2E verification remain follow-up work.
- **Implementation note:** P1-03E adds transaction-bound department audit writes for create/update/manager assignment/deactivation and API controller regression tests for actor-scope propagation plus conflict/not-found response mapping. Browser mutation E2E passed against a running backend from the CORS-allowed `http://localhost:4200` origin; direct Supabase `audit_logs` assertions remain a follow-up until an audit API or DB integration harness exists.

### P1-04 - Complete Task CRUD and Typed FE/BE Contract

- **Goal:** Finish task detail/update/delete behavior and remove local-only success paths.
- **SRS:** TASK-01, TASK-02, TASK-06.
- **Current gap:** Archive/delete policy and duplicate semantics remain incomplete after typed contract, detail, persisted edit and sequence-backed task codes.
- **Backend likely affected:** `TasksController.cs`, task DTOs/interfaces/handlers, `PostgresTaskQueries.cs`, `PostgresTaskCommands.cs`.
- **Frontend likely affected:** `task-api.client.ts`, `task.service.ts`, task models, create/edit/detail components.
- **DB/migration:** Collision-safe code generation (sequence/function/UUID-backed scheme); decide audited soft delete versus explicit no-delete policy.
- **API contract:** `GET /api/tasks/{id}`, typed `PUT/PATCH`, documented delete/archive contract, typed duplicate options and responses.
- **Authorization:** Resource scope plus `task.update`/delete permission; confidential checks on every detail/mutation; assignment requires `task.assign`.
- **Acceptance:** Create/edit/detail/archive/duplicate persist after reload; failed mutation displays error; generated codes are collision safe.
- **Tests:** Contract serialization, concurrent code generation, resource authorization, rollback and Angular component/service tests.
- **Dependencies:** P0-03, P0-04, P1-01.
- **Risk:** High.
- **Estimated size:** L.
- **Definition of Done:** No `unknown` at task API boundary and no task mutation reports success before confirmed persistence.
- **Suggested commit boundary:** Typed detail/update first; archive/delete and duplicate semantics separately.
- **Implementation note:** P1-04A removes `unknown` payloads from the Angular task API client by introducing explicit FE request/response contracts aligned with backend task command DTOs. Behavior-changing items from P1-04 remain follow-up slices: task detail endpoint, persisted edit flow, collision-safe code generation, archive/delete policy and duplicate semantics.
- **Implementation note:** P1-04B adds `GET /api/tasks/{id}` and a typed Angular `getTask()` client method. The backend detail query reuses the same scope and confidential-task predicate as the list query and returns `404` for missing or inaccessible tasks. UI adoption, persisted edit reload/error behavior, code generation, archive/delete and duplicate semantics remain follow-up slices.
- **Implementation note:** P1-04C makes the Angular task edit flow wait for `PUT /api/tasks/{id}` before reporting success, then refreshes the persisted task through `GET /api/tasks/{id}` and keeps the edit modal open with an error message when persistence fails. Collision-safe code generation, archive/delete policy and duplicate semantics remain follow-up slices.
- **Implementation note:** P1-04D replaces timestamp-based task code generation with a PostgreSQL sequence-backed `public.next_task_code()` default. Create and duplicate task commands now let the database assign `tasks.code`, and migration `0003_collision_safe_task_code.sql` initializes the sequence after existing numeric `CV-*` codes to avoid unique-key collisions. Archive/delete policy and duplicate semantics remain follow-up slices.
- **Implementation note:** P1-04E defines the product delete policy as soft archive, not hard delete. Migration `0004_task_archive_policy.sql` adds archive metadata and indexes, `POST /api/tasks/{id}/archive` archives visible editable tasks and records activity, list/detail/activity queries hide archived tasks by default, and duplicate now returns a persisted `{ id, task }` contract that the Angular drawer awaits before adding the copy. Hard-delete recovery/admin export remains outside this slice.

### P1-05 - Scoped Paging, Filtering and Keyword Search

- **Goal:** Replace unbounded client-only list operations with server contracts.
- **SRS:** TASK-04, SEARCH-01, NFR-01.
- **Current gap:** Task/request/project lists are unbounded; task board filters loaded rows only.
- **Backend likely affected:** query DTOs/interfaces, `PostgresTaskQueries.cs`, request/project queries and controllers.
- **Frontend likely affected:** task board, `task-api.client.ts`, task/request/project services and pagination controls.
- **DB/migration:** Add only indexes justified by `EXPLAIN` for scope + status/due/department/assignee/text/order predicates.
- **API contract:** `page`, `pageSize`, filters, search, sort; response `{items,total,page,pageSize}` with bounded maximum.
- **Authorization:** Scope/confidential predicates must be inside the paged/count SQL, not applied after paging.
- **Acceptance:** Stable paging has no duplicates/skips for fixed data; total and filters honor scope; FE URL/state reflects query.
- **Tests:** Query combinations, invalid bounds, IDOR counts, deterministic ordering and FE paging tests.
- **Dependencies:** P1-01, P1-04.
- **Risk:** High.
- **Estimated size:** L.
- **Definition of Done:** Main lists are bounded and representative query plans are documented.
- **Suggested commit boundary:** Tasks first; requests/projects in follow-up commits using the same contract.

### P1-06 - Subtask Integrity and Parent Progress

- **Goal:** Make subtask updates transactional and derive parent progress/completion suggestion.
- **SRS:** SUBTASK-01, SUBTASK-02.
- **Current gap:** CRUD exists, but parent aggregation/suggestion does not; due rule depends on unverified DB auth helpers.
- **Backend likely affected:** `TasksController.cs`, task command DTOs, `PostgresTaskCommands.cs`, `TaskProgressPolicy.cs`.
- **Frontend likely affected:** task detail drawer, subtask and task models, activity timeline.
- **DB/migration:** Trigger or application-owned aggregate, chosen once; enforce due-date rule consistently and prevent cycles if nesting remains supported.
- **API contract:** Existing subtask routes return updated parent progress/suggestion or a refreshed task snapshot.
- **Authorization:** Parent task access; assignment targets must be visible/eligible; override permission for due-date exception if retained.
- **Acceptance:** Create/update/delete/completion recalculates parent exactly once in the same transaction and surfaces pending-review suggestion.
- **Tests:** 0/50/100%, empty set, concurrent updates, due-date/scope errors and FE refresh.
- **Dependencies:** P0-04, P1-04.
- **Risk:** High.
- **Estimated size:** M.
- **Definition of Done:** Parent state cannot drift from committed subtasks and tests cover concurrency.
- **Suggested commit boundary:** Aggregate/integrity behavior plus UI adoption.

## Phase 2 - Collaboration

### P2-01 - Comments, Mentions and Note Visibility

- **Goal:** Turn basic feedback into authorized comments with mentions and internal/public semantics.
- **SRS:** COLLAB-01, COLLAB-02.
- **Current gap:** Comment endpoint/composer exists; no mention parsing, visibility, threading or recipient events.
- **Backend likely affected:** `TasksController.cs`, task DTOs/commands; proposed Collaboration application/infrastructure module.
- **Frontend likely affected:** task detail drawer, comment/activity models and timeline component.
- **DB/migration:** Add/validate comment visibility, mention join and reply relation; indexes by task/time.
- **API contract:** Paged comments, create/reply with visibility and mentioned user IDs; return canonical rendered data.
- **Authorization:** Task access; internal notes restricted by role/scope; mentioned users must be eligible and cannot gain task access through mention.
- **Acceptance:** Authorized comment persists, mentions notify eligible users, internal notes are hidden from disallowed actors, unsafe HTML renders as text.
- **Tests:** Visibility/IDOR, mention eligibility/deduplication, XSS rendering and pagination.
- **Dependencies:** P1-01, P1-04; emits notification events consumed by P2-04.
- **Risk:** High.
- **Estimated size:** L.
- **Definition of Done:** Comment UI/API/DB/tests and audit event are connected end-to-end.
- **Suggested commit boundary:** Comment/visibility first; mentions/replies second.

### P2-02 - Secure Attachments

- **Goal:** Implement actual authorized upload/download/delete with storage lifecycle.
- **SRS:** FILE-01.
- **Current gap:** Attachment schema/name placeholders exist; no bytes, storage provider or API/UI.
- **Backend likely affected:** Proposed Files application/infrastructure module and controller, `DependencyInjection.cs`; task access reader reused.
- **Frontend likely affected:** attachment model, task create/edit/detail components and proposed attachment service.
- **DB/migration:** Store generated object key, original name, type, size, hash, scan/status and owner/reference; indexes and cleanup state.
- **API contract:** Multipart upload/init-complete flow, authorized download or short-lived URL, metadata list and delete; bounded size/type.
- **Authorization:** Task access plus upload/delete permission; never trust filename/path; confidential task files inherit confidentiality.
- **Acceptance:** Valid file uploads with progress, survives reload, authorized user downloads, unrelated actor gets denied, invalid/oversize file is rejected.
- **Tests:** Path traversal, MIME/extension, size, IDOR, orphan cleanup, storage failure and FE progress/error.
- **Dependencies:** P0-04, P1-01, P1-04; storage choice/config approved.
- **Risk:** Critical.
- **Estimated size:** XL.
- **Definition of Done:** Storage and metadata stay consistent, security tests pass, and secrets are externalized.
- **Suggested commit boundary:** Storage abstraction/metadata, API security, then UI.

### P2-03 - Central Audit and Timeline

- **Goal:** Capture and query important changes consistently.
- **SRS:** AUDIT-01.
- **Current gap:** DB trigger covers limited task fields; assignment/RBAC/settings/file events and query UI are absent.
- **Backend likely affected:** Proposed Audit application/infrastructure service/controller; task/request/user/role/department/file commands call it.
- **Frontend likely affected:** activity timeline and proposed scoped audit screen/service.
- **DB/migration:** Normalize event type, actor, entity, correlation, before/after metadata with redaction; indexes and retention fields.
- **API contract:** Paged task timeline and admin/scoped audit queries with filters; no secret/token/file bytes in payloads.
- **Authorization:** Users see accessible entity timeline; managers scoped; system audit restricted to authorized admin/auditor.
- **Acceptance:** Required mutations create exactly one correlated event and queries never leak inaccessible entity metadata.
- **Tests:** Event coverage, rollback/no phantom event, redaction, scope and pagination.
- **Dependencies:** P0-04, P1-01; coordinate hooks with P1-02/P1-03/P2-02.
- **Risk:** High.
- **Estimated size:** L.
- **Definition of Done:** Coverage matrix from audit 05 is satisfied and retention/redaction documented.
- **Suggested commit boundary:** Audit writer/transaction integration, then read API/UI.

### P2-04 - Persistent Notifications, Realtime and Email Fallback

- **Goal:** Deliver persistent read/unread notifications, live updates and controlled fallback.
- **SRS:** NOTIF-01, NOTIF-02.
- **Current gap:** Tables exist; service empty; badge hard-coded; no API, SignalR or email provider.
- **Backend likely affected:** Proposed Notifications module/controller/hub, `Program.cs`, `DependencyInjection.cs`; event sources in task/comment/request commands.
- **Frontend likely affected:** `notification.service.ts`, notification model, header, app/auth lifecycle; proposed list/dropdown.
- **DB/migration:** Validate recipient/read/deduplication/outbox/preferences indexes and delivery-attempt fields.
- **API contract:** Paged list, unread count, mark one/all read; authenticated hub events carry notification ID/minimal metadata; email uses outbox worker.
- **Authorization:** User can access only own notifications; hub group identity comes from validated token; referenced entity still rechecks access on navigation.
- **Acceptance:** Event commits notification with business transaction, badge/list update live, reconnect refreshes count, read state persists, email fallback honors preference and deduplicates.
- **Tests:** Ownership IDOR, hub auth/reconnect, outbox retry/dedupe, revoked token disconnect and FE list states.
- **Dependencies:** P0-04, P1-01, P2-01, P2-03; P3-01 uses this channel.
- **Risk:** High.
- **Estimated size:** XL.
- **Definition of Done:** REST + SignalR + fallback paths are observable, idempotent and tested.
- **Suggested commit boundary:** Persistent REST, realtime delivery, then email outbox as three commits.

## Phase 3 - Operations

### P3-01 - Deadline, Overdue and SLA Workers

- **Goal:** Execute time-based rules reliably in UTC.
- **SRS:** JOB-01, JOB-02, REQUEST-02.
- **Current gap:** Due/overdue/SLA fields exist; no hosted worker, schedule, dedupe or escalation.
- **Backend likely affected:** Proposed Jobs application/infrastructure services/hosted worker, task/request commands, `Program.cs`/DI.
- **Frontend likely affected:** task/request status and SLA indicators only.
- **DB/migration:** Job lease/checkpoint and unique dedupe keys; indexes for due/overdue/SLA batch selection.
- **API contract:** No public mutation required; optional admin job-health endpoint exposes counts/timestamps, not payload secrets.
- **Authorization:** System identity alone marks overdue/escalates; users cannot submit system-only status.
- **Acceptance:** Due-soon notifies once per window, overdue updates atomically, SLA breach escalates once, restart/concurrent instances do not duplicate.
- **Tests:** Fake clock, timezone boundary, retries, concurrency/lease, dedupe and notification integration.
- **Dependencies:** P0-03, P0-04, P2-04.
- **Risk:** High.
- **Estimated size:** L.
- **Definition of Done:** Idempotent workers have health/logging and deterministic clock-based tests.
- **Suggested commit boundary:** Scheduling foundation, task jobs, then SLA job.

### P3-02 - Server-Side Dashboards

- **Goal:** Provide scoped personal, manager and admin aggregates from bounded server queries.
- **SRS:** DASH-01, DASH-02, DASH-03.
- **Current gap:** FE computes KPIs from loaded tasks; department cards are not a full dashboard.
- **Backend likely affected:** Proposed Dashboard application/infrastructure queries/controller; reuse scope services.
- **Frontend likely affected:** dashboard component/service and department cards.
- **DB/migration:** Add indexes/materialization only after query-plan evidence; document freshness if cached.
- **API contract:** Separate personal/department/system summary endpoints with explicit date range/timezone and stable metric definitions.
- **Authorization:** Personal self; manager permitted departments; admin/director all according to named permission; confidential counts must follow policy.
- **Acceptance:** Metrics match trusted fixture data by role and date boundary; FE has loading/empty/error states.
- **Tests:** Aggregate correctness, scope/confidential leakage, timezone, performance budget and component states.
- **Dependencies:** P1-01, P1-05, P3-01.
- **Risk:** High.
- **Estimated size:** L.
- **Definition of Done:** Client aggregation is removed for SRS KPIs and query performance is measured.
- **Suggested commit boundary:** Personal, manager, admin dashboards as separate commits.

### P3-03 - Filtered Reports and Excel/PDF Export

- **Goal:** Add scoped report preview and bounded exports that match filters.
- **SRS:** REPORT-01, REPORT-02.
- **Current gap:** Disabled menu, empty service, no backend report/export path.
- **Backend likely affected:** Proposed Reports module/controller/exporters and DI.
- **Frontend likely affected:** `report.service.ts`, sidebar/routes and proposed report screen.
- **DB/migration:** Usually none beyond P1-05 indexes; optional export job metadata for large asynchronous exports.
- **API contract:** Paged preview and `POST /api/reports/tasks/exports` or bounded streaming endpoint; format, filters and timezone explicit.
- **Authorization:** Same row scope/confidential rules as task query; export permission; maximum range/row limits; audit every export.
- **Acceptance:** Preview and file contain identical authorized rows/filters; Excel/PDF opens correctly; large requests are rejected/queued safely.
- **Tests:** Scope, formula injection escaping, PDF/Excel smoke, limits, timezone and audit event.
- **Dependencies:** P1-05, P2-03, P3-02 metric definitions where shared.
- **Risk:** High.
- **Estimated size:** L.
- **Definition of Done:** Both formats, UI, audit, security and representative performance tests pass.
- **Suggested commit boundary:** Report query/UI, Excel, PDF separately.

## Phase 4 - AI MVP

### P4-01 - AI Provider Foundation and Smart Task Draft

- **Goal:** Introduce a provider-neutral, failure-tolerant AI foundation and editable task draft.
- **SRS:** AI-01, NFR-03, NFR-04.
- **Current gap:** AI settings/log schema exists; no provider/service/API/UI or confirmation contract.
- **Backend likely affected:** Proposed AI Application/Infrastructure provider, policy/logging and controller; DI/config.
- **Frontend likely affected:** task create modal and proposed AI service/models.
- **DB/migration:** Validate request log fields, retention/redaction, model/config version and cost/latency metadata; never store raw secrets.
- **API contract:** `POST /api/ai/task-drafts` returns suggestion fields, rationale/warnings and request ID; it never creates a task.
- **Authorization:** `task.create` and usable department scope before context construction; minimize/redact input; per-user rate/usage limit.
- **Acceptance:** Natural language produces editable draft; user must explicitly submit normal task create; provider timeout gives recoverable error and manual form remains usable.
- **Tests:** Mock provider contract, redaction, rate limit, prompt injection boundaries, timeout/retry, no automatic mutation and FE confirmation.
- **Dependencies:** Phase 0, P1-04, P2-03.
- **Risk:** High.
- **Estimated size:** XL.
- **Definition of Done:** Provider is replaceable, secrets externalized, logs redacted, confirmation enforced by API/UI tests.
- **Suggested commit boundary:** Provider/safety foundation, draft API, then UI.

### P4-02 - Authorized Task Summary

- **Goal:** Summarize only tasks and collaboration data the actor may view.
- **SRS:** AI-02, NFR-03.
- **Current gap:** Insight summary column/RLS exists; no runtime path.
- **Backend likely affected:** AI module, task access reader/queries, proposed AI controller/cache policy.
- **Frontend likely affected:** task detail drawer and AI service.
- **DB/migration:** Insight version/source hash/expiry if caching; redacted request logs.
- **API contract:** `POST /api/ai/tasks/{id}/summary` or permission-aware GET with explicit regeneration; response identifies AI suggestion and freshness.
- **Authorization:** Recheck task/confidential access before loading every source; never use inaccessible comments/internal notes.
- **Acceptance:** Authorized actor receives labeled summary; unrelated actor gets concealment response; stale content invalidates cache; failure does not block task detail.
- **Tests:** Unauthorized access, source selection, cache invalidation, provider failure and FE loading/retry.
- **Dependencies:** P4-01, P2-01, P1-01.
- **Risk:** Critical.
- **Estimated size:** L.
- **Definition of Done:** No prompt is constructed before access checks and all leakage tests pass.
- **Suggested commit boundary:** Summary backend/security, then UI.

### P4-03 - Priority and Risk Suggestion

- **Goal:** Return explainable, non-mutating priority/risk suggestions with explicit human adoption.
- **SRS:** AI-03, NFR-04.
- **Current gap:** Insight fields exist; no inference/UI/confirmation.
- **Backend likely affected:** AI module/controller and task access/query services.
- **Frontend likely affected:** create/edit/detail components and AI service/models.
- **DB/migration:** Version/confidence/rationale/suggested action and accepted/rejected feedback if retained.
- **API contract:** Suggest endpoint returns proposed priority/risk/reason/action; separate normal task update applies user-edited value.
- **Authorization:** Task view for analysis; `task.update` to apply; confidential context redacted and never cross-tenant/scoped.
- **Acceptance:** Suggestion is visibly labeled, explainable, editable and never mutates task without confirmed update.
- **Tests:** No auto-write, apply permission, invalid output validation, failure fallback and FE confirmation.
- **Dependencies:** P4-01, P4-02 patterns.
- **Risk:** High.
- **Estimated size:** L.
- **Definition of Done:** Provider output is schema validated and confirmation is proven in API/UI tests.
- **Suggested commit boundary:** Suggestion contract/backend, then adoption UI.

## Phase 5 - Advanced

### P5-01 - Smart Assignment

- **Goal:** Rank eligible assignees without bypassing scope or automatically assigning.
- **SRS:** AI-04.
- **Current gap:** Feature setting only; no candidate retrieval/ranking/UI.
- **Backend files likely affected:** Proposed AI assignment service/controller; existing task policy queries/commands and DI registrations.
- **Frontend files likely affected:** Task create/edit assignee controls and proposed AI assignment service/model.
- **DB/migration:** Optional workload/skill feature tables only with validated source/retention.
- **API contract:** Suggest candidates with score/reasons; normal assignment endpoint applies confirmed choice.
- **Authorization:** `task.assign`; candidates limited to eligible department/scope and active users; do not expose private workload details.
- **Acceptance criteria:** Candidate list is explainable, eligible-only and never changes assignment automatically.
- **Tests required:** Deterministic mock ranking, eligibility/IDOR, no auto-assign, explanation and confirmation tests.
- **Dependencies:** P4-01, P1-02, P1-03, P1-04.
- **Risk:** High; ranking can leak workload or encode unfair business rules.
- **Estimated size:** XL.
- **Definition of Done:** Model/provider can be disabled without breaking manual assignment and all candidates pass backend eligibility checks.
- **Suggested commit boundary:** Scoped suggestion backend first, then UI adoption.

### P5-02 - Authorized Semantic Search/RAG

- **Goal:** Build an access-preserving embedding lifecycle and semantic task search.
- **SRS:** AI-05.
- **Current gap:** pgvector/table exist, HNSW index commented, no writer/query/provider.
- **Backend files likely affected:** Proposed Search/AI indexing services, worker and controller; task/comment access services.
- **Frontend files likely affected:** Header search, task board search and proposed semantic search result UI/service.
- **DB/migration:** Chunk provenance/version/deletion, embedding dimensions/provider, HNSW after measured data, secure backfill/checkpoint.
- **API contract:** Paged hybrid search with result snippets/source IDs and no raw embedding exposure.
- **Authorization:** Candidate retrieval must enforce current task access before returning content; deletion/permission changes invalidate index promptly.
- **Acceptance criteria:** Search never returns inaccessible task/comment content and updates when source content or permissions change.
- **Tests required:** Backfill/update/delete, confidential/cross-scope leakage, ranking fixture, stale permission, provider failure and query-plan tests.
- **Dependencies:** P1-05, P2-01, P3-01 worker foundation, P4-01.
- **Risk:** Critical; RAG leakage would expose confidential task data.
- **Estimated size:** XL.
- **Definition of Done:** Index lifecycle, authorized query and UI are complete, with mandatory leakage suite passing.
- **Suggested commit boundary:** Index lifecycle first, authorized query second, UI third.

### P5-03 - Internal AI Chatbot

- **Goal:** Answer internal questions with citations from authorized retrieval only.
- **SRS:** AI-06.
- **Current gap:** No conversation, retrieval, citation or UI path.
- **Backend files likely affected:** Proposed chatbot service/controller; reuse P5-02 retriever and P4-01 provider.
- **Frontend files likely affected:** Proposed chatbot route/component/service; header/sidebar entry if enabled.
- **DB/migration:** Optional bounded conversation metadata with retention/delete controls; avoid storing full sensitive prompts by default.
- **API contract:** Conversation turn/stream contract returns citations and refusal/error state.
- **Authorization:** Per-turn retrieval rechecks current access; citations only to accessible entities; rate and token limits.
- **Acceptance criteria:** Answers cite accessible sources, refuse unsupported/leaky requests and remain disabled if retrieval is unavailable.
- **Tests required:** Cited answers, explicit uncertainty, prompt-injection/leakage/refusal, permission-change and provider-timeout tests.
- **Dependencies:** P5-02 complete and leakage suite passing.
- **Risk:** Critical; chatbot can amplify search leakage and hallucinate authority.
- **Estimated size:** XL.
- **Definition of Done:** Backend guardrails, eval set, UI and feature flag are complete.
- **Suggested commit boundary:** Guardrails/eval set before UI/streaming.

### P5-04 - Auto Classification

- **Goal:** Suggest request/task classification with confidence and human override.
- **SRS:** AI-07.
- **Current gap:** Request classification columns exist; no classifier writes them.
- **Backend files likely affected:** Proposed AI classification service/controller; request/task command integration.
- **Frontend files likely affected:** Inter-department request component and proposed classification review UI.
- **DB/migration:** Model/version/confidence/review outcome fields and audit event if existing columns are insufficient.
- **API contract:** Suggest classification; explicit accept/reject endpoint or existing update applies confirmed value.
- **Authorization:** Actor must view source and have update permission to accept; labels restricted to allowed taxonomy.
- **Acceptance criteria:** Suggestions are labeled, confidence-scored, reviewable and reversible by an authorized user.
- **Tests required:** Schema validation, confidence threshold, no automatic irreversible routing, override/audit, leakage and failure tests.
- **Dependencies:** P4-01, P2-03, stable request taxonomy.
- **Risk:** Medium; wrong classification can misroute work if human review is weak.
- **Estimated size:** L.
- **Definition of Done:** Metrics distinguish suggested, accepted and rejected classifications.
- **Suggested commit boundary:** Suggestion backend first, then review UI.

### P5-05 - Meeting/Email to Task Draft

- **Goal:** Ingest an authorized meeting/email source into a reviewable task draft.
- **SRS:** AI-08, NFR-04.
- **Current gap:** Only generic task source fields; no connector, consent, ingestion or dedupe.
- **Backend files likely affected:** Proposed connector/ingestion module and AI draft orchestration; task create endpoint integration only after confirmation.
- **Frontend files likely affected:** Task create UI/source review screen and proposed connector consent/status components.
- **DB/migration:** External source identity, consent/ownership, dedupe hash, ingestion status and retention; encrypt provider tokens outside business tables.
- **API contract:** Connector callback/import creates an ingestion record and draft, never a task; explicit user confirmation uses normal task create API.
- **Authorization:** User grants least-privilege connector access; imported content scoped to owner; admins cannot silently ingest private mail.
- **Acceptance criteria:** User can consent, import, review/edit a draft, confirm creation or delete source-derived draft data.
- **Tests required:** Consent/revoke, duplicate webhook, malicious content, attachment policy, provider outage, draft edit/confirm and deletion tests.
- **Dependencies:** P4-01, P2-02, P2-03, P0-04.
- **Risk:** Critical; external connectors introduce privacy and token-storage risks.
- **Estimated size:** XL.
- **Definition of Done:** One connector is implemented with least privilege, revocation, retention and explicit confirmation.
- **Suggested commit boundary:** Connector, ingestion safety and review UI as separate commits.

## Cross-Cutting Release Checklist

For every work item:

1. State the intended files before editing and distinguish existing from new files.
2. Update or add incremental migrations and verify both empty and upgrade paths when schema changes.
3. Test the negative authorization path, not only success.
4. Run `dotnet build`, discovered `dotnet test`, `npm.cmd run build`, and non-watch frontend tests.
5. Add focused logs/metrics without secrets or user content beyond the approved retention policy.
6. Update `docs/audit/11-srs-traceability-matrix.md` only when evidence justifies a status change.
7. Keep generated artifacts, package upgrades and unrelated refactors out of the commit.

## Copyable Implementation Prompts

Use one prompt at a time. Replace `<WORK-ITEM>` with exactly one ID below.

### Common Prompt Template

```text
Implement <WORK-ITEM> from docs/audit/12-implementation-roadmap.md.

First read docs/audit/01 through 12 and inspect the current source. Before editing, list the existing and proposed files you expect to change and explain why. Implement only this work item end-to-end across backend, frontend, database and API where applicable. Preserve the current ASP.NET Core, Angular and PostgreSQL architecture. Add an incremental migration when needed; never reset or destroy existing data. Enforce authorization in the backend and add positive and negative tests, including scope/IDOR cases where relevant. Run backend build/tests and frontend build/tests. Update docs/audit/11-srs-traceability-matrix.md with concrete evidence only after verification. Do not upgrade unrelated packages, refactor unrelated modules or implement adjacent roadmap items. Finish with changed files, migration result, test commands/results, remaining risks and the suggested commit boundary from the roadmap.
```

### Work-Item Prompts

1. `P0-01`: Use the common template for reproducible config and incremental schema lifecycle. Do not change business features.
2. `P0-02`: Use the common template for the automated test baseline. Production behavior may change only to enable legitimate test hosting.
3. `P0-03`: Use the common template for canonical task status semantics. Migrate existing data safely and test every transition.
4. `P0-04`: Use the common template for auth hardening, transactions and error contracts. Split commits as specified; do not log tokens.
5. `P1-01`: Use the common template for named policies and the scope/IDOR regression suite. Do not add management UI.
6. `P1-02`: Use the common template for user and RBAC administration. Keep identity ownership and last-admin safeguards explicit.
7. `P1-03`: Use the common template for department hierarchy management. Reject cycles and unsafe deactivation.
8. `P1-04`: Use the common template for complete typed task CRUD. Remove local-only success paths without redesigning state management.
9. `P1-05`: Use the common template for scoped paging/filter/search. Prove scope predicates occur before paging.
10. `P1-06`: Use the common template for subtask integrity and parent progress. Use one authoritative aggregate mechanism.
11. `P2-01`: Use the common template for comments, mentions and note visibility. A mention must not grant task access.
12. `P2-02`: Use the common template for secure attachments. Confirm storage choice before coding and include hostile upload tests.
13. `P2-03`: Use the common template for central audit/timeline. Redact secrets and avoid audit events for rolled-back transactions.
14. `P2-04`: Use the common template for persistent notifications, SignalR and email fallback. Implement REST persistence before live delivery.
15. `P3-01`: Use the common template for deadline/overdue/SLA workers. Use an injectable clock and prove idempotency.
16. `P3-02`: Use the common template for server dashboards. Define each metric and verify confidential-data scope.
17. `P3-03`: Use the common template for reports and Excel/PDF export. Test formula injection and row-scope parity.
18. `P4-01`: Use the common template for AI provider foundation and smart task draft. The AI endpoint must never create a task.
19. `P4-02`: Use the common template for authorized summaries. Perform access checks before constructing provider input.
20. `P4-03`: Use the common template for priority/risk suggestions. Require explicit human adoption.
21. `P5-01`: Use the common template for smart assignment. Return eligible suggestions only and never auto-assign.
22. `P5-02`: Use the common template for authorized semantic search. Permission-change and deletion invalidation tests are mandatory.
23. `P5-03`: Use the common template for the internal chatbot. Do not begin unless P5-02 is complete and its leakage suite passes.
24. `P5-04`: Use the common template for auto classification. Preserve human override and audit accepted/rejected suggestions.
25. `P5-05`: Use the common template for meeting/email-to-task draft. Implement one connector, least privilege and explicit consent.

## Roadmap Completion Criterion

The roadmap is complete only when traceability statuses are supported by executable evidence, not when all planned files exist. A reasonable first release target is completion of Phases 0-2 plus P3-01 and the personal/manager subset of P3-02. AI phases should be independently feature-flagged and must not be used to mask unfinished core authorization, data integrity or operational work.
