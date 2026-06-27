# 03 - Backend Task, Assignment, Status Workflow and Subtask Audit

## 1. Detailed Checklist

| Feature | Status | Backend evidence | DB evidence | Test evidence | Missing behavior / risk | Severity |
|---|---|---|---|---|---|---|
| Create task with generated code | IMPLEMENTED | `TasksController.Create`; `CreateTaskHandler.HandleAsync`; `PostgresTaskCommands.CreateAsync` generates `CV-{UtcNow:yyyyMMddHHmmss}` | `tasks.code` is unique; trigger defaults status/priority | No backend tests found | Timestamp-only code can collide if multiple tasks are created in the same second | Medium |
| Task core fields | IMPLEMENTED | `CreateTaskRequest` and `UpdateTaskRequest` include title, description, task type, priority, dates, progress, department, source, urgency, security | `tasks` table has matching columns | No backend tests found | No explicit API model validation attributes; DB catches some invalid data | Medium |
| CreatedBy and reporter | IMPLEMENTED | `PostgresTaskCommands.CreateAsync` inserts `reporter_id` and `created_by` as actor id | `tasks.reporter_id`, `tasks.created_by`; trigger also defaults to `auth.uid()` | No backend tests found | Backend uses direct connection; DB trigger `auth.uid()` may be null outside Supabase JWT context | Low |
| IsConfidential | IMPLEMENTED | `PostgresTaskCommands.IsConfidential`; access checks in `PostgresTaskQueries`, `PostgresTaskAccessReader`, `PostgresTaskPolicyQueries` | `tasks.is_confidential`; DB function `can_access_task` | No backend tests found | Mapping treats every non-`internal` security level as confidential | Low |
| Assignee/co-assignee/watcher | IMPLEMENTED | `CreateTaskHandler` checks `task.assign`; `PostgresTaskCommands` writes `task_assignments` for `assignee`, `co_assignee`, `watcher` | `task_assignment_type` enum and `task_assignments` table | No backend tests found | Assignment changes are not audited as assignment-specific events | Medium |
| Tags | IMPLEMENTED | `PostgresTaskCommands.AddTaskTagAsync` and `ReplaceTagsAsync`; `PostgresTaskQueries.GetTasksAsync` returns tag names | `tags`, `task_tags` tables | No backend tests found | No tag search/filter endpoint | Medium |
| Attachment on create/after create | MISSING | No upload/download attachment controller or command found | `attachments` table and RLS policies exist | No tests found | Task create request has no attachment payload; no secure file API | High |
| Copy/create similar task | PARTIAL | `TasksController.Duplicate`; `PostgresTaskCommands.DuplicateAsync` copies task, optional people/subtasks/attachments | `tasks`, `task_assignments`, `subtasks`, `attachments`, `task_tags` | No backend tests found | Does not copy comments/activities/extension requests; access failure returns `NotFound` | Low |
| Server-side filter/search/sort/paging | MISSING | `ITaskQueries.GetTasksAsync(Guid actorUserId)` has no filter/search/page parameters; controller `GET api/tasks` accepts no query DTO | DB has indexes on task-related fields, but query only orders by created date/id | No tests found | Status/deadline/department/assignee/priority/tag filters, search, sort and pagination are absent | High |
| Read task detail | PARTIAL | No `GET api/tasks/{id}` endpoint; list returns all accessible tasks including subtasks/extensions | Access SQL scopes list rows | No tests found | No detail endpoint with per-id 404/403 behavior | Medium |
| Update task | PARTIAL | `TasksController.Update`; `PostgresTaskCommands.UpdateAsync` checks `task.update`, access, department, assignment permission and workflow if `StatusId` changes | `tasks` table constraints and triggers | No backend tests found | No closed-task check in C#; relies on DB trigger with `auth.uid()`/role helper context | High |
| Delete task | MISSING | No `DELETE api/tasks/{id}` and no `DELETE FROM tasks` command found | RLS has admin delete policy | No tests found | No backend delete or soft delete flow | Medium |
| Closed task content lock | PARTIAL | `TaskWorkflowPolicy` disallows transitions from C# `Closed`; no command-specific content lock | DB trigger blocks edits when `OLD.closed_at IS NOT NULL` unless admin/reopen permission | No tests found | Code never sets `closed_at`; DB trigger checks `closed_at`, not `status.is_closed`, so closed status may still be editable | High |
| Reopen closed task | PARTIAL | `TaskWorkflowPolicy.CanTransition(... allowClosedReopen = false)` has optional reopen branch, but callers never pass true | Permission `task.reopen` is seeded; DB trigger references it | No tests found | No ReopenTask endpoint/command; optional branch is inactive | Medium |
| Status workflow centralized | PARTIAL | `TaskWorkflowPolicy` centralizes transition table; both update paths call it | DB status lookup table has SRS codes | No backend tests found | C# hard-coded IDs do not match DB seed/order; this can make workflow transitions semantically wrong | Critical |
| Overdue status by system | MISSING | No background service/job found; no command sets overdue safely | DB has `overdue` status and `tasks.overdue_at` | No tests found | User could attempt status update to overdue if transition IDs allow; no system-driven overdue process | High |
| Status/assignee/due/priority audit | PARTIAL | No direct code inserts task activity/audit; SQL trigger audits task create/status/due/priority | `audit_logs`; trigger `trg_tasks_audit_changes`; `task_activities` table | No backend tests found | Assignee changes are not audited by trigger; task activity table is read but never written by backend | Medium |
| Due date >= start date | IMPLEMENTED | Not explicit in C# | DB constraint `chk_task_date_range` | No tests found | API returns generic failure if DB exception bubbles; no friendly validation result | Medium |
| Progress 0..100 | PARTIAL | `TaskProgressPolicy.Normalize` clamps update/subtask progress | DB constraints `chk_task_progress`, `chk_subtask_progress` | No tests found | Create task always inserts 0; invalid progress is silently clamped, not rejected | Low |
| Optimistic locking/concurrency | MISSING | No row version, `xmin`, ETag, `updated_at` condition, or concurrency token found | No version column/constraint found | No tests found | Last write wins | Medium |
| Soft delete | MISSING | No soft delete fields/commands found | `tasks` has no `deleted_at`/`is_deleted` | No tests found | Deletes are not implemented; soft delete absent | Low |
| Subtask CRUD | IMPLEMENTED | `TasksController` exposes create/update/delete subtask; `PostgresTaskCommands` implements insert/update/delete | `subtasks` table | No backend tests found | Delete is hard delete | Medium |
| Subtask assignee/deadline | IMPLEMENTED | `CreateSubTaskRequest`, `UpdateSubTaskRequest`; commands write assignee and due_date | `subtasks.assignee_id`, `subtasks.due_date` | No tests found | No validation that subtask assignee can access parent task | Medium |
| Subtask due-date rule | IMPLEMENTED | Not explicit in C# | DB trigger `trg_subtasks_validate_due_date` blocks due date after parent unless admin/manager/permission | No tests found | Relies on DB auth helper context; may not work as intended from backend direct DB connection | Medium |
| Parent progress calculation | MISSING | No backend update from subtask completion to parent progress found | `tasks.subtask_progress_auto_sync` exists | No tests found | Parent progress is not recalculated from subtasks | High |
| Suggest pending review when subtasks complete | MISSING | No code updates `parent_completion_suggested` or status when all subtasks complete | `tasks.parent_completion_suggested` exists | No tests found | No suggestion flow | Medium |
| Cycle prevention | PARTIAL | `chk_task_not_self_parent` blocks self-parent | No recursive/cycle constraint found | No tests found | Multi-level cycles are not prevented | Medium |

## 2. Actual State Machine

Source of active backend state machine: `EnterpriseTask.Domain/Tasks/TaskWorkflowPolicy.cs`.

```text
Created(1) -> Assigned(2), Cancelled(8)
Assigned(2) -> Accepted(3), Rejected(9), Cancelled(8)
Accepted(3) -> InProgress(4), Cancelled(8)
InProgress(4) -> Waiting(5), Completed(6), Cancelled(8)
Waiting(5) -> InProgress(4), Cancelled(8)
Completed(6) -> Closed(7), Cancelled(8)
Closed(7) -> no transition
Cancelled(8) -> no transition
Rejected(9) -> no transition
```

Important mismatch: the DB seed in `supabase_schema_v2_clean.sql` inserts SRS-like statuses in this order:

```text
1 new
2 assigned
3 in_progress
4 pending_review
5 completed
6 closed
7 on_hold
8 cancelled
9 overdue
```

Because C# uses hard-coded IDs with different meanings from ID 3 onward, workflow validation can allow or block the wrong business status. For example, DB `closed` is likely id 6, but C# treats id 6 as `Completed` and allows transition to id 7, which DB likely means `on_hold`.

## 3. Transition Table

| From | To | Actor | Condition | Evidence | Status |
|---|---|---|---|---|---|
| Created | Assigned | Any actor with `task.update` and task access | `TaskWorkflowPolicy.CanTransition` allows 1 -> 2 | `TaskWorkflowPolicy`; `UpdateTaskStatusHandler`; `PostgresTaskCommands.UpdateStatusAsync` | PARTIAL: status ID mismatch risk |
| Created | Cancelled | Any actor with `task.update` and task access | 1 -> 8 allowed | Same as above | PARTIAL |
| Assigned | Accepted | Any actor with `task.update` and task access | 2 -> 3 allowed | Same as above | PARTIAL: DB id 3 is `in_progress`, not accepted |
| Assigned | Rejected | Any actor with `task.update` and task access | 2 -> 9 allowed | Same as above | PARTIAL: DB id 9 is `overdue`, not rejected |
| Accepted | InProgress | Any actor with `task.update` and task access | 3 -> 4 allowed | Same as above | PARTIAL: DB id 4 is `pending_review` |
| InProgress | Waiting | Any actor with `task.update` and task access | 4 -> 5 allowed | Same as above | PARTIAL: DB id 5 is `completed` |
| InProgress | Completed | Any actor with `task.update` and task access | 4 -> 6 allowed | Same as above | PARTIAL: DB id 6 is `closed` |
| Waiting | InProgress | Any actor with `task.update` and task access | 5 -> 4 allowed | Same as above | PARTIAL |
| Completed | Closed | Any actor with `task.update` and task access | 6 -> 7 allowed | Same as above | PARTIAL: DB id 7 is `on_hold` |
| Closed | InProgress | Intended admin/reopen path | `allowClosedReopen` exists but callers do not pass true | `TaskWorkflowPolicy.CanTransition`; callers in `PostgresTaskCommands` | MISSING |
| Any | Overdue | System job | No job/handler found | Repository search for hosted/background jobs | MISSING |

## 4. Validation Rules

| Rule | Status | Evidence | Gap |
|---|---|---|---|
| Title required | PARTIAL | `CreateTaskRequest.Title`; DB `tasks.title NOT NULL` | No API validation for empty/whitespace title; command trims but does not check empty |
| DueDate >= StartDate | IMPLEMENTED | DB `chk_task_date_range` | No friendly API validation |
| Progress 0..100 | PARTIAL | `TaskProgressPolicy.Normalize`; DB checks | Silently clamps instead of rejecting invalid input |
| Assigned requires task type | IMPLEMENTED at DB | `validate_task_status_rules` raises exception for assigned task without type | C# does not prevalidate |
| Assigned requires priority | IMPLEMENTED at DB | `validate_task_status_rules` | C# does not prevalidate |
| Assigned requires due date | IMPLEMENTED at DB | `validate_task_status_rules` | C# does not prevalidate |
| Assigned requires assignee | PARTIAL | DB trigger checks assignee count on update | Create inserts task before assignments, and trigger is only `BEFORE UPDATE`; C# does not enforce when setting status in create |
| Confidential access | IMPLEMENTED | Access SQL in query/access readers | No backend tests |
| Invalid transition | PARTIAL | `TaskWorkflowPolicy` returns false and controller maps to `403` | Response does not distinguish invalid transition from authorization failure |
| Closed content lock | PARTIAL | DB trigger checks `OLD.closed_at`; C# policy has no outgoing transitions from closed | Code does not set `closed_at`; hard-coded ID mismatch weakens this |
| Subtask due date <= parent due date | IMPLEMENTED at DB | `trg_subtasks_validate_due_date` | Depends on DB auth helper context |
| Parent progress sync | MISSING | No command updates parent progress from subtasks | Schema has `subtask_progress_auto_sync` but no implementation |
| Audit status/due/priority | PARTIAL | DB trigger writes `audit_logs` for task create/status/due/priority | No assignee audit; no backend audit tests |

## 5. End-to-End Use Cases

| Use case | Actual chain | Status |
|---|---|---|
| Create task | `TasksController.Create` -> `CreateTaskHandler.HandleAsync` -> `ITaskPolicyQueries.HasPermissionAsync/CanUseDepartmentAsync` -> `PostgresTaskCommands.CreateAsync` -> `tasks`, `task_assignments`, `tags`, `task_tags` | IMPLEMENTED |
| Assign task | `TasksController.TransferAssignee` -> `PostgresTaskCommands.TransferAssigneeAsync` -> `ITaskAccessReader.GetTaskAccessAsync` + `task.assign` -> `ReplaceSingleAssignmentAsync` -> `task_assignments` | IMPLEMENTED |
| Change status | `TasksController.UpdateStatus` -> `UpdateTaskStatusHandler.HandleAsync` -> `ITaskPolicyQueries.GetAccessAsync` -> `PostgresTaskCommands.UpdateStatusAsync` -> `TaskWorkflowPolicy.CanTransition` -> update `tasks.status_id` | PARTIAL: status ID mismatch |
| Update progress | `TasksController.Update` -> `PostgresTaskCommands.UpdateAsync` -> `TaskProgressPolicy.Normalize` -> update `tasks.progress` | PARTIAL: no optimistic locking; invalid values are clamped |
| Create/complete subtask | `TasksController.CreateSubTask`/`UpdateSubTask` -> `PostgresTaskCommands.CreateSubTaskAsync`/`UpdateSubTaskAsync` -> `TaskProgressPolicy.Normalize` -> insert/update `subtasks` | PARTIAL: parent progress/suggestion not updated |

## 6. API, DB and Test Gaps

- No `GET api/tasks/{id}` detail endpoint.
- No `DELETE api/tasks/{id}` endpoint.
- No task attachment upload/download endpoint.
- No server-side filter/search/sort/pagination contract for task list.
- No explicit task close/reopen endpoint.
- No system job for overdue status.
- No optimistic locking or concurrency guard.
- No backend tests for task creation, permissions, workflow transitions, confidential access, subtask due-date rule, or duplicate behavior.
- DB has status definitions that do not align with C# hard-coded status IDs.

## 7. Business Bugs That Can Occur

- Workflow transitions can move to the wrong DB status because C# status IDs do not match seeded DB status IDs.
- A closed task may still be editable if only `status_id` is changed to closed and `closed_at` remains null.
- Invalid transition currently returns `403`, which can be confused with permission failure.
- Multiple tasks created in the same second can generate duplicate `code`.
- Task list can grow without pagination and become slow or too large.
- Parent task progress can become stale after subtask completion.
- Assignment changes are not represented in `audit_logs` by the observed trigger.
- Attachment rows can exist in DB, but users cannot upload/download them through backend API.

## 8. Build and Test Result

| Command | Result | Notes |
|---|---|---|
| `dotnet build backend\EnterpriseTask\EnterpriseTask.slnx` | PASS | 0 warnings, 0 errors |
| `npm.cmd run build` in `enterprise-task-ms` | PASS | Angular production build succeeded |
| `npm.cmd test -- --watch=false` in `enterprise-task-ms` | FAIL | `src/app/app.spec.ts` test `should render title` fails because `querySelector('h1')?.textContent` is `undefined` |

## 9. Files Reviewed

- `backend/EnterpriseTask/EnterpriseTask.Api/Controllers/TasksController.cs`
- `backend/EnterpriseTask/EnterpriseTask.Application/Tasks/ITaskQueries.cs`
- `backend/EnterpriseTask/EnterpriseTask.Application/Tasks/TaskCommandDtos.cs`
- `backend/EnterpriseTask/EnterpriseTask.Application/Tasks/TaskDtos.cs`
- `backend/EnterpriseTask/EnterpriseTask.Application/Tasks/CreateTaskHandler.cs`
- `backend/EnterpriseTask/EnterpriseTask.Application/Tasks/UpdateTaskStatusHandler.cs`
- `backend/EnterpriseTask/EnterpriseTask.Domain/Tasks/TaskStatusIds.cs`
- `backend/EnterpriseTask/EnterpriseTask.Domain/Tasks/TaskWorkflowPolicy.cs`
- `backend/EnterpriseTask/EnterpriseTask.Domain/Tasks/TaskProgressPolicy.cs`
- `backend/EnterpriseTask/EnterpriseTask.Infrastructure/Tasks/PostgresTaskCommands.cs`
- `backend/EnterpriseTask/EnterpriseTask.Infrastructure/Tasks/PostgresTaskQueries.cs`
- `backend/EnterpriseTask/EnterpriseTask.Infrastructure/Tasks/PostgresTaskAccessReader.cs`
- `backend/EnterpriseTask/EnterpriseTask.Infrastructure/Tasks/PostgresTaskPolicyQueries.cs`
- `supabase_schema_v2_clean.sql`
