# 04 - Backend Collaboration, Realtime and Jobs Audit

## 1. Checklist by Group

### Comment and Activity

| Feature | Status | Backend evidence | DB evidence | Test evidence | Missing behavior / risk | Severity |
|---|---|---|---|---|---|---|
| Task comment create | IMPLEMENTED | `TasksController.AddComment`; `PostgresTaskCommands.AddCommentAsync` checks `comment.create` and task access, then inserts `task_comments` | `task_comments` table; RLS policies for accessible task comments | No backend tests found | No input length/moderation/empty-string validation beyond trim | Medium |
| Task comment read | PARTIAL | `PostgresTaskQueries.GetTasksAsync` returns comment content as `ProcessingNotes` array | `task_comments` table | No tests found | No dedicated comment list endpoint; does not expose author/time/internal flag | Medium |
| Mention `@user` | MISSING | No backend code inserts `task_comment_mentions` | `task_comment_mentions` table and RLS policies exist | No tests found | No mention parsing, persistence, validation, notification, or API | High |
| Internal/public note on task comment | PARTIAL | `task_comments.is_internal` exists in DB; create command inserts only `content` | `task_comments.is_internal BOOLEAN DEFAULT FALSE` | No tests found | API cannot set/read internal/public visibility distinctly | Medium |
| Task activity timeline | PARTIAL | `TasksController.GetActivities`; `PostgresTaskQueries.GetActivitiesAsync` reads `task_activities` scoped by task access | `task_activities` table and RLS policies | No tests found | Backend code does not write `task_activities`; DB audit trigger writes `audit_logs`, not `task_activities` | High |
| Edit/delete task comment | MISSING | No controller/command for comment update/delete found | DB RLS allows own comment update, no delete policy observed | No tests found | No API rule or audit for edit/delete | Medium |
| Audit for collaboration changes | PARTIAL | SQL trigger audits task create/status/due/priority only | `audit_logs`; `trg_tasks_audit_changes` | No tests found | No audit for comments, mentions, assignee changes, notifications or attachments | Medium |

### Attachment

| Feature | Status | Backend evidence | DB evidence | Test evidence | Missing behavior / risk | Severity |
|---|---|---|---|---|---|---|
| Upload attachment | MISSING | No `IFormFile`, upload endpoint, storage service, or attachment command found | `attachments` table supports task/inter-request owner and metadata | No tests found | No secure upload path | High |
| Download attachment | MISSING | No download endpoint or storage abstraction found | RLS select policy exists for accessible attachments | No tests found | No authorization-enforced download API | High |
| File validation | MISSING | No code for size, extension, MIME sniffing, path normalization, or path traversal checks found | DB stores `file_name`, `file_url`, `file_size`, `content_type` | No tests found | Client-provided metadata would be unsafe if used directly | High |
| Storage abstraction | MISSING | No local/S3/Azure/MinIO abstraction found | N/A | No tests found | No transaction/orphan cleanup strategy | High |
| Attachment metadata | PARTIAL | `PostgresTaskQueries.GetTasksAsync` reads attachment names; `DuplicateAsync` can copy attachment rows | `attachments` table and indexes | No tests found | Metadata can be copied but cannot be created/downloaded through backend API | Medium |

### Notification, Realtime and Email

| Feature | Status | Backend evidence | DB evidence | Test evidence | Missing behavior / risk | Severity |
|---|---|---|---|---|---|---|
| Persistent notifications | MISSING | No backend code inserts/queries/updates `notifications` | `notifications` table, indexes, RLS policies | No tests found | Assigned/reassigned/comment/mention/due/overdue/review events do not create notifications | High |
| Read/unread API | MISSING | No notification controller/service found | `notifications.is_read`, `read_at`; notification RLS | No tests found | No list/read/read-all/unread count API | High |
| SignalR/WebSocket realtime | MISSING | No `AddSignalR`, `MapHub`, hub class, or WebSocket code found | N/A | No tests found | No authenticated realtime delivery | High |
| Frontend reconnect support | CANNOT_VERIFY in backend prompt | Backend has no realtime endpoint to reconnect to | N/A | No tests found | Must be evaluated in frontend prompt if a client exists | Medium |
| Email fallback/offline | MISSING | No email/Smtp/SendGrid/mail service found | `notification_preferences.email_enabled` exists | No tests found | No email fallback for important/offline events | Medium |
| Duplicate notification protection | MISSING | No notification producer/retry/idempotency code found | No uniqueness key on notifications | No tests found | Duplicate behavior cannot be controlled because producer is absent | Medium |

### Background Jobs

| Feature | Status | Backend evidence | DB evidence | Test evidence | Missing behavior / risk | Severity |
|---|---|---|---|---|---|---|
| Job framework/hosted service | MISSING | No `BackgroundService`, `IHostedService`, Hangfire, Quartz, cron, timer found | N/A | No tests found | No scheduler runtime | High |
| Due-soon reminders | MISSING | No job scans deadlines | `notification_preferences.due_soon_hours` exists | No tests found | No 24h/8h/1h reminders | High |
| Automatic overdue flag | MISSING | No job sets overdue task/request status | `tasks.overdue_at`, task `overdue` status; inter-request `sla_breached` | No tests found | Overdue is not system-managed | High |
| Retry/idempotency/logging/failure handling | MISSING | No job producer/consumer code found | N/A | No tests found | No operational guarantees | Medium |
| Scheduler dashboard security | NOT_APPLICABLE | No scheduler dashboard exists | N/A | No tests found | Not applicable until scheduler is introduced | Low |
| UTC/time zone handling | PARTIAL | Inter-request SLA uses `DateTimeOffset.UtcNow` for remaining hours; create uses `now()` and due date + 17:00 | `TIMESTAMPTZ` columns | No tests found | No explicit business timezone policy for due-soon/overdue jobs | Medium |

### Inter-department Request and SLA

| Feature | Status | Backend evidence | DB evidence | Test evidence | Missing behavior / risk | Severity |
|---|---|---|---|---|---|---|
| Request types | IMPLEMENTED | `CreateInterDepartmentRequestCommand.Type`; `PostgresInterDepartmentRequestCommands.CreateAsync` casts to `inter_request_type` | `inter_request_type` enum; SLA seed includes procurement, asset, it-support, payment, recruitment, communication-design, legal | No backend tests found | Invalid type relies on DB enum exception rather than friendly validation | Low |
| Routing department | IMPLEMENTED | Create stores `target_department_id`; list joins requester/target department | `inter_department_requests.target_department_id` | No tests found | No rule requiring target department non-null observed | Medium |
| Assignment by receiving manager | IMPLEMENTED | `AssignInterRequestOwnerHandler` plus `PostgresInterDepartmentRequestCommands.AssignOwnerAsync`; policy validates owner belongs to target department | `owner_id`, `target_department_id` | No tests found | Assignment update itself does not create notification | Medium |
| Requester tracking | IMPLEMENTED | `GetRequestsAsync` scopes requester/owner/departments and returns messages/status/SLA snapshot | `inter_department_requests`, `inter_request_messages` | No tests found | No pagination/filtering observed | Medium |
| Request close confirmation | IMPLEMENTED | `CloseAsync` allows coordinator/requester to close only when status is `done` | `inter_request_status` includes `done`, `closed` | No tests found | No close notification/audit observed | Medium |
| SLA due time/warning/breach | PARTIAL | Create stores `sla_started_at` and `sla_due_at`; query computes remaining hours and breached snapshot | `inter_request_sla_policies`, `sla_due_at`, `sla_breached` | No tests found | No background job to set `sla_breached`, warn, or notify | High |
| Internal/public request comments | PARTIAL | `AddMessageAsync` stores `author_role` as requester/processor and returns messages | `inter_request_messages.author_role` | No tests found | No explicit internal/private visibility flag; no mention/notification | Medium |

## 2. Event Mapping

| Event | Receiver | Persistent notification | Realtime | Email | Evidence | Status |
|---|---|---|---|---|---|---|
| Task assigned | Assignee | No | No | No | Assignment writes in `PostgresTaskCommands.AddTaskAssignmentAsync`/`ReplaceSingleAssignmentAsync`; no notification insert found | MISSING |
| Task reassigned | New/old assignee | No | No | No | `TransferAssigneeAsync` replaces assignment and optionally adds comment | MISSING |
| New task comment | Task participants/watchers | No | No | No | `AddCommentAsync` inserts `task_comments` only | MISSING |
| Mention | Mentioned user | No | No | No | Mention table exists, but no mention parsing code found | MISSING |
| Due soon | Assignee/watchers | No | No | No | No background job found | MISSING |
| Overdue | Assignee/manager | No | No | No | No overdue job found | MISSING |
| Task completion/review | Creator/manager/assignee | No | No | No | Status update only updates `tasks.status_id` and optional comment | MISSING |
| Inter-request created | Target department/manager | No | No | No | `CreateAsync` inserts request only | MISSING |
| Inter-request owner assigned | Owner/requester | No | No | No | `AssignOwnerAsync` updates owner/status/latest_message only | MISSING |
| Inter-request message | Related users | No | No | No | `AddMessageAsync` inserts message and updates latest message only | MISSING |
| Inter-request SLA warning/breach | Owner/manager/requester | No | No | No | Query computes breach view; no job/notification found | MISSING |

## 3. Job Inventory and Schedule

No runtime job inventory was found.

| Job | Schedule | Evidence | Status |
|---|---|---|---|
| Due-soon reminder | Not registered | No hosted/background scheduler code found | MISSING |
| Automatic task overdue | Not registered | No job updates `tasks.overdue_at` or overdue status | MISSING |
| Inter-request SLA breach/warning | Not registered | No job updates `sla_breached` or sends warnings | MISSING |
| Email fallback/offline notification | Not registered | No email service or job found | MISSING |

## 4. Security Review

### Upload/Download

- No upload/download API exists, so file size, extension, MIME validation, antivirus scanning, path traversal prevention, and content sniffing are all missing at runtime.
- `attachments` table has metadata and RLS policies, but direct backend endpoints do not use those policies as an upload/download boundary.
- No storage abstraction or orphan-file cleanup strategy was found.

### SignalR/WebSocket

- No SignalR/WebSocket runtime exists.
- Therefore there is no hub authentication, per-user group mapping, reconnect handling, authorization by task/request scope, or fanout duplicate control.

## 5. Race Conditions and Duplicate Risks

- If notifications are later added around current task/inter-request commands, assignment/status/message operations are not wrapped in an explicit application transaction with notification creation.
- `TransferAssigneeAsync` deletes then inserts assignment; a notification producer added between those steps could see transient state unless transaction boundaries are introduced.
- Inter-request codes use `IR-{UtcNow:yyyyMMddHHmmss}` and task codes use `CV-{UtcNow:yyyyMMddHHmmss}`, so simultaneous creates can collide.
- Because no idempotency key exists for notifications/jobs, retries could duplicate reminders once jobs are implemented.
- SLA breach is computed at read time in `GetRequestsAsync`; persisted `sla_breached` can become stale without a job.

## 6. Build and Test Result

| Command | Result | Notes |
|---|---|---|
| `dotnet build backend\EnterpriseTask\EnterpriseTask.slnx` | PASS | 0 warnings, 0 errors |
| `npm.cmd run build` in `enterprise-task-ms` | PASS | Angular production build succeeded |
| `npm.cmd test -- --watch=false` in `enterprise-task-ms` | FAIL | `src/app/app.spec.ts` test `should render title` fails because `querySelector('h1')?.textContent` is `undefined` |

## 7. Files Reviewed

- `backend/EnterpriseTask/EnterpriseTask.Api/Controllers/TasksController.cs`
- `backend/EnterpriseTask/EnterpriseTask.Api/Controllers/InterDepartmentRequestsController.cs`
- `backend/EnterpriseTask/EnterpriseTask.Application/Tasks/ITaskQueries.cs`
- `backend/EnterpriseTask/EnterpriseTask.Application/Tasks/TaskCommandDtos.cs`
- `backend/EnterpriseTask/EnterpriseTask.Application/Tasks/TaskDtos.cs`
- `backend/EnterpriseTask/EnterpriseTask.Infrastructure/Tasks/PostgresTaskCommands.cs`
- `backend/EnterpriseTask/EnterpriseTask.Infrastructure/Tasks/PostgresTaskQueries.cs`
- `backend/EnterpriseTask/EnterpriseTask.Infrastructure/InterDepartmentRequests/PostgresInterDepartmentRequestCommands.cs`
- `backend/EnterpriseTask/EnterpriseTask.Infrastructure/InterDepartmentRequests/PostgresInterDepartmentRequestQueries.cs`
- `backend/EnterpriseTask/EnterpriseTask.Infrastructure/InterDepartmentRequests/PostgresInterDepartmentRequestPolicyQueries.cs`
- `backend/EnterpriseTask/EnterpriseTask.Application/InterDepartmentRequests/*`
- `backend/EnterpriseTask/EnterpriseTask.Api/Program.cs`
- `supabase_schema_v2_clean.sql`
