# 05 - Backend Dashboard, Report, Export, Audit Log and Settings Audit

## 1. Dashboard Metric Matrix

| Metric | Formula/query | Scope | Cache | Evidence | Status |
|---|---|---|---|---|---|
| Personal active tasks | No backend dashboard metric endpoint found. Frontend derives from loaded tasks where status is not terminal. | Task list is scoped in `PostgresTaskQueries.GetTasksAsync` by actor access. | None found | `DashboardComponent.kpis`; `TaskService.tasks`; `PostgresTaskQueries.GetTasksAsync` | PARTIAL |
| Personal due soon | Frontend calculates due date state client-side using `diff <= 2 days`; backend has no metric query. | Depends on already-loaded task list. | None found | `DashboardComponent.getDueState`; `PostgresTaskQueries.GetTasksAsync` | PARTIAL |
| Personal overdue | Frontend calculates due date state client-side; no backend overdue metric or job. | Depends on already-loaded task list. | None found | `DashboardComponent.kpis`; `supabase_schema_v2_clean.sql` has `tasks.overdue_at` and status `overdue` | PARTIAL |
| Pending review / waiting confirmation | No backend dashboard metric found; frontend dashboard does not specifically compute pending review. | N/A | None found | No dashboard controller/service in backend | MISSING |
| Manager completion rate on time | No backend metric/query found. | N/A | None found | No dashboard/report controller; no completion-on-time aggregation query | MISSING |
| Manager workload by user | No backend metric/query found. | N/A | None found | No workload aggregation query found | MISSING |
| Manager top overdue tasks | Frontend spotlight sorts open tasks by priority, not overdue ranking; backend no top-overdue query. | Depends on task list scope. | None found | `DashboardComponent.spotlightTasks` | MISSING |
| Department overview | Department cards count members, active tasks, completed tasks grouped by department. | `@isElevated OR d.id = @departmentId`; does not include `user_department_scopes`. | None found | `DepartmentsController.GetCards`; `PostgresDepartmentQueries.GetCardsAsync` | PARTIAL |
| Department SLA | Hard-coded `'95%'` in query. | Same as department cards. | None found | `PostgresDepartmentQueries.GetCardsAsync` | MISSING |
| Admin/director department overview | Same department cards query with `scope.CanSeeAllData`. | Admin/director can see all departments through `UserScope.CanSeeAllData`. | None found | `HttpCurrentUserContext`; `UserScope`; `PostgresDepartmentQueries.GetCardsAsync` | PARTIAL |
| Trend of new tasks | No backend trend query found. | N/A | None found | No dashboard/report controller | MISSING |
| Backlog / pending workload | No backend backlog metric found except active task count in department cards. | Department cards scope only. | None found | `PostgresDepartmentQueries.GetCardsAsync` | PARTIAL |
| Confidential data protection | Task list query excludes confidential tasks unless creator/reporter/assigned or admin/director. Department aggregate counts all tasks by department and does not apply confidential row filtering. | Task list strong; department aggregate can count confidential tasks in totals. | None found | `PostgresTaskQueries.GetTasksAsync`; `PostgresDepartmentQueries.GetCardsAsync` | PARTIAL |
| Inter-request SLA overview | Backend returns SLA snapshot per request and frontend aggregates counts client-side. | Backend query scoped by elevated/requester/owner/departments/scopes. | None found | `InterDepartmentRequestsController.Get`; `PostgresInterDepartmentRequestQueries.GetRequestsAsync`; `InterDepartmentRequestService.summaryFactory` | PARTIAL |

## 2. Export Matrix

| Export/report feature | Status | Evidence | Missing behavior |
|---|---|---|---|
| Task report by user | MISSING | No report controller/query found | No backend report endpoint |
| Task report by department | MISSING | Department cards exist but are not a report endpoint | No filtered report with period/status/SLA |
| Task report by status/SLA | MISSING | No report/export query found | No backend filter/report contract |
| Backend filters for reports | MISSING | No report DTO/endpoint found | Filters cannot be enforced server-side |
| Excel export | MISSING | No `ClosedXML`, `EPPlus`, `CsvHelper`, Excel code/package found | No file generation/content type/formula-injection protection |
| PDF export | MISSING | No `QuestPDF`, `PdfSharp`, `iText`, PDF code/package found | No PDF generation/content type |
| Export scope enforcement | MISSING | No export endpoint found | Cannot verify scope on export |
| Streaming/large dataset limits | MISSING | No export endpoint found | No streaming, pagination, or row limits |
| Valid file name/content type | MISSING | No export endpoint found | Not implemented |

## 3. Audit Event Coverage Matrix

| Audit event | Status | Evidence | Gap |
|---|---|---|---|
| Task create | PARTIAL | `audit_task_changes()` inserts `audit_logs` on task insert | Actor uses `auth.uid()` in DB trigger; backend direct DB connection may not populate it |
| Task update general fields | PARTIAL | Trigger only checks status, due date and priority on update | Title/description/progress/department/security/estimate changes are not audited |
| Task delete | MISSING | No delete task endpoint; trigger does not handle delete | No delete audit |
| Task status | PARTIAL | `audit_task_changes()` logs `task.status_changed` | Status ID mismatch risk from prompt 3; actor reliability concern |
| Task due date | PARTIAL | `audit_task_changes()` logs `task.due_date_changed` | No API search endpoint for audit |
| Task assignee | MISSING | Assignment commands write `task_assignments`; trigger does not audit assignment table | No audit event for assign/reassign/co-assignee/watcher |
| Task priority | PARTIAL | `audit_task_changes()` logs `task.priority_changed` | Actor reliability concern |
| Comment/mention | MISSING | `AddCommentAsync` inserts `task_comments`; no audit insert | Comment and mention changes are not audited |
| Role/permission changes | MISSING | No backend role/permission CRUD or audit trigger found | No audit for critical RBAC changes |
| AI request metadata | PARTIAL | `ai_request_logs` table exists | No backend AI runtime/API writes metadata |
| Audit API search | MISSING | No audit controller/service found | No admin/manager search by user/time/entity/action |
| User deletion of audit logs | PARTIAL | No backend delete API; DB RLS only defines select/insert policies | No API, but no explicit delete route found |
| Sensitive value masking | MISSING | Trigger stores `to_jsonb(NEW)` for task create | No masking strategy found |
| Reliable transaction/outbox | PARTIAL | DB trigger runs with task insert/update transaction | No outbox or retry for application-level audit events |
| Console-only logging | IMPLEMENTED | No console-only audit substitute found; DB audit table exists | Coverage remains incomplete |

## 4. Settings Matrix

| Setting area | Status | Evidence | Missing behavior |
|---|---|---|---|
| Notification reminders | PARTIAL | `notification_preferences` table has `due_soon_hours` default `[24,8,1]` and RLS | No backend settings API, no reminder job, no admin management |
| SLA policies | PARTIAL | `inter_request_sla_policies` table, seed data, `GET api/inter-department-requests/sla-policies` | Read-only API; no admin CRUD; no audit on changes |
| Workflow/status configuration | PARTIAL | `task_statuses` table and RLS admin manage policy; task priorities table exists | No backend admin API; C# hard-coded status IDs bypass DB configurability |
| AI feature toggles | PARTIAL | `ai_feature_settings` table and seed data; admin RLS policy | No backend API to read/update from application layer |
| Admin-only changes | PARTIAL | DB RLS policies restrict admin for SLA/status/AI settings in direct Supabase context | Backend API for changes is missing; direct backend connection may bypass RLS depending configuration |
| Audit important setting changes | MISSING | No trigger/API audit for settings tables found | No audit for SLA/status/AI/notification setting changes |
| Validation/fallback defaults | PARTIAL | DB defaults/seeds exist for notification and AI/SLA settings | No application-level validation/fallback service |

## 5. Performance Risks

| Risk | Evidence | Impact | Severity |
|---|---|---|---|
| Unbounded task list | `GET api/tasks` has no pagination/filter parameters; `PostgresTaskQueries.GetTasksAsync` returns all accessible tasks | Large datasets can slow dashboard/task board and client aggregation | High |
| Client-side dashboard aggregation | `DashboardComponent` computes KPIs from loaded task signal | Metrics depend on currently loaded data and can become expensive or incomplete | High |
| Department aggregate scans | `PostgresDepartmentQueries.GetCardsAsync` joins departments/profiles/tasks and groups all visible rows | May become expensive as tasks/profiles grow; counts confidential tasks | Medium |
| No cache/invalidation | No `IMemoryCache`/`IDistributedCache` usage found | Repeated dashboard reads hit DB directly once backend dashboard exists | Medium |
| Inter-request unbounded list | `GetRequestsAsync` has no pagination and then loads messages for all visible IDs | Can load many requests/messages at once | High |
| N+1-ish in memory grouping | Inter-request messages are loaded in one query but grouped in memory; task subtasks/extensions also loaded broadly then filtered per task | Acceptable at small scale, but grows with unbounded parent list | Medium |
| Missing export limits | No export implementation | Future export could accidentally dump huge/scoped data without limits | Medium |
| Index support exists but no report query uses it | DB has task/inter-request/audit indexes | Helpful foundation, but no server-side report endpoints consume them | Low |

## 6. Build and Test Result

| Command | Result | Notes |
|---|---|---|
| `dotnet build backend\EnterpriseTask\EnterpriseTask.slnx` | PASS | 0 warnings, 0 errors |
| `npm.cmd run build` in `enterprise-task-ms` | PASS | Angular production build succeeded |
| `npm.cmd test -- --watch=false` in `enterprise-task-ms` | FAIL | `src/app/app.spec.ts` test `should render title` fails because `querySelector('h1')?.textContent` is `undefined` |

## 7. Files Reviewed

- `backend/EnterpriseTask/EnterpriseTask.Api/Controllers/DepartmentsController.cs`
- `backend/EnterpriseTask/EnterpriseTask.Api/Controllers/ProjectsController.cs`
- `backend/EnterpriseTask/EnterpriseTask.Api/Controllers/TasksController.cs`
- `backend/EnterpriseTask/EnterpriseTask.Api/Controllers/InterDepartmentRequestsController.cs`
- `backend/EnterpriseTask/EnterpriseTask.Infrastructure/Departments/PostgresDepartmentQueries.cs`
- `backend/EnterpriseTask/EnterpriseTask.Infrastructure/Projects/PostgresProjectQueries.cs`
- `backend/EnterpriseTask/EnterpriseTask.Infrastructure/Tasks/PostgresTaskQueries.cs`
- `backend/EnterpriseTask/EnterpriseTask.Infrastructure/InterDepartmentRequests/PostgresInterDepartmentRequestQueries.cs`
- `backend/EnterpriseTask/EnterpriseTask.Infrastructure/InterDepartmentRequests/PostgresInterDepartmentRequestCommands.cs`
- `backend/EnterpriseTask/EnterpriseTask.Application/Common/UserScope.cs`
- `backend/EnterpriseTask/EnterpriseTask.Api/Auth/HttpCurrentUserContext.cs`
- `enterprise-task-ms/src/app/features/dashboard/dashboard.component.ts`
- `enterprise-task-ms/src/app/features/dashboard/dashboard.component.html`
- `enterprise-task-ms/src/app/core/services/department.service.ts`
- `enterprise-task-ms/src/app/core/services/inter-department-request.service.ts`
- `enterprise-task-ms/src/app/core/services/task.service.ts`
- `supabase_schema_v2_clean.sql`
