# Prompt 07 - Frontend Core Screens Audit

## Scope

- Source reviewed only; no application code changed.
- Verification follows route -> component -> service -> backend endpoint where available.
- Status values used: `IMPLEMENTED`, `PARTIAL`, `MISSING`, `CANNOT_VERIFY`, `NOT_APPLICABLE`.

## Frontend Architecture Snapshot

| Area | Status | Evidence | Notes |
| --- | --- | --- | --- |
| Routing | PARTIAL | `enterprise-task-ms/src/app/app.routes.ts` | Routes exist for login, forgot password, dashboard, tasks, projects, departments, and inter-department requests. No routes for user management, role matrix, reports, settings, notifications, or AI screens. |
| Route guards | PARTIAL | `enterprise-task-ms/src/app/core/guards/auth.guard.ts`, `enterprise-task-ms/src/app/core/guards/role.guard.ts` | Auth guard is active on the main layout. `role.guard.ts` exists but is empty and unused. |
| Auth state/session | PARTIAL | `enterprise-task-ms/src/app/core/services/auth.service.ts` | Login calls `POST /api/auth/login`, stores token/user in localStorage, restores user by localStorage. No token expiry handling or `/auth/me` refresh flow on app start. |
| API client/interceptor | PARTIAL | `enterprise-task-ms/src/app/core/interceptors/auth.interceptor.ts`, `enterprise-task-ms/src/app/core/services/task-api.client.ts` | Bearer token is attached only for URLs starting with `API_BASE_URL`. Task API client covers many task endpoints but uses `unknown` payloads, so FE DTOs are not strongly enforced at API boundary. |
| State management | PARTIAL | `enterprise-task-ms/src/app/app.config.ts`, `enterprise-task-ms/src/app/core/services/task-state.store.ts`, `enterprise-task-ms/src/app/features/task/store/task.effects.ts` | NgRx task reducer is registered, but effects are commented out and main runtime state uses signal-based services/stores. |
| Mock/fallback data | PARTIAL | `enterprise-task-ms/src/app/app.config.ts`, `enterprise-task-ms/src/app/core/services/task.service.ts`, `department.service.ts`, `project.service.ts`, `inter-department-request.service.ts` | Mock data sources are still registered. Services keep mock/local state when API fails, which helps demos but can hide backend/API failures. |
| Error handling/loading states | PARTIAL | `enterprise-task-ms/src/app/features/auth/login.component.ts`, `enterprise-task-ms/src/app/core/services/task.service.ts` | Login has submit/error state. Core task/request services frequently use `.catch(() => undefined)` after optimistic updates, so mutation failures are silent. No global error/toast/interceptor handling found. |
| Permission UI | PARTIAL | `enterprise-task-ms/src/app/core/services/auth.service.ts`, `task-board.component.ts`, `inter-department-request.component.ts` | FE checks role strings and department id/code to show/disable actions. This is UI behavior only and must not be treated as backend authorization. |

## Screen / Feature Matrix

| Route / Feature | Component | Service / API | State | Permission UI | Status | Evidence |
| --- | --- | --- | --- | --- | --- | --- |
| `/login` | `LoginComponent` | `AuthService.login()` -> `POST /api/auth/login` | Signal + localStorage token/user | None by role menu | PARTIAL | `enterprise-task-ms/src/app/features/auth/login.component.ts`, `enterprise-task-ms/src/app/core/services/auth.service.ts`, `backend/EnterpriseTask/EnterpriseTask.Api/Controllers/AuthController.cs` |
| `/forgot-password` | `ForgotPasswordComponent` | `AuthService.forgotPassword()` local response only | Local component state | None | PARTIAL | `enterprise-task-ms/src/app/core/services/auth.service.ts`, `enterprise-task-ms/src/app/features/auth/forgot-password.component.ts` |
| `/dashboard` | `DashboardComponent` | Uses `TaskService` (`GET /api/tasks`) and `DepartmentService` (`GET /api/departments/cards`) | Computed client-side KPI/deadline/dept cards | None | PARTIAL | `enterprise-task-ms/src/app/features/dashboard/dashboard.component.ts`, `enterprise-task-ms/src/app/core/services/task.service.ts`, `department.service.ts` |
| `/tasks` board | `TaskBoardComponent` | `TaskService` + `TaskApiClient`: `GET /api/tasks`, `GET /api/tasks/activities`, `GET /api/tasks/form-options`, task mutations | Signal store with optimistic updates | `AuthService.canEditTask()` and `hasSpecialTaskPermission()` | PARTIAL | `enterprise-task-ms/src/app/features/task/components/task-board/task-board.component.ts`, `enterprise-task-ms/src/app/core/services/task-api.client.ts`, `backend/.../TasksController.cs` |
| Task list search/filter/sort | `TaskBoardComponent` | No server query endpoint used | Client-side `search`, `filterPriority`, `statusView` signals | Edit actions gated client-side | PARTIAL | `task-board.component.ts`, `task-board.component.html` |
| Task list pagination | None found | None found | None | None | MISSING | No pagination query/state found in `task-board.component.ts/html` or `TaskApiClient`. |
| Create task | `TaskCreateModalComponent` | `TaskService.createTask()` -> `POST /api/tasks` | Draft signal, optimistic local task, reload API after success | Caller controls modal; assignee required client-side | PARTIAL | `task-create-modal.component.ts/html`, `task.service.ts`, `task-api.client.ts` |
| Edit task | `TaskEditModalComponent` | `TaskService.updateTask()` updates local state only; status changes through action helpers can call `/status` | Local copied task model | `AuthService.canEditTask()` | PARTIAL | `task-edit-modal.component.ts/html`, `task.service.ts` |
| Task detail | `TaskDetailDrawerComponent` | Status/comment/subtask/extension helpers call task endpoints; timeline from local/API activities | Signal subtasks/comments/action feedback | Button enablement based on status + FE permission | PARTIAL | `task-detail-drawer.component.ts/html`, `task.service.ts`, `task-api.client.ts` |
| Task attachment | Create/edit token fields only | No upload/download API call found | `attachmentNames` local model field | Same as task edit | PARTIAL | `task-create-modal.component.html`, `task-edit-modal.component.html`, `task.service.ts` |
| Task AI summary/risk/generate | None found in task screens | Backend AI/search was not exposed to these task UI flows | None | None | MISSING | No AI entry point in `task-create-modal`, `task-edit-modal`, or `task-detail-drawer`. |
| `/projects` | `ProjectComponent` | `ProjectService` -> `GET /api/projects`; task detail drawer reused | Signal projects + task overviews computed client-side | Detail actions inherit task drawer checks | PARTIAL | `enterprise-task-ms/src/app/features/project/project.component.ts/html`, `project.service.ts`, `backend/.../ProjectsController.cs` |
| `/departments` | `DepartmentComponent` | `DepartmentService` -> `GET /api/departments/cards` | Signal cards + computed summary | None | PARTIAL | `enterprise-task-ms/src/app/features/department/department.component.ts/html`, `department.service.ts`, `backend/.../DepartmentsController.cs` |
| `/inter-department-requests` | `InterDepartmentRequestComponent` | `InterDepartmentRequestService` -> GET/POST/status/assign/message/close endpoints | Signal list + client-side filters/summary | Uses `MockAuthUser`, role string, department code | PARTIAL | `inter-department-request.component.ts/html`, `inter-department-request.service.ts`, `backend/.../InterDepartmentRequestsController.cs` |
| User management | No routed component | `user.service.ts` is empty | None | None | MISSING | `enterprise-task-ms/src/app/core/services/user.service.ts` length 0; no route/menu item. |
| Role/permission matrix | No routed component | `role.service.ts` and `role.guard.ts` are empty | None | None | MISSING | `enterprise-task-ms/src/app/core/services/role.service.ts`, `core/guards/role.guard.ts` length 0. |
| Reports | Menu disabled item only | `report.service.ts` is empty; no backend report controller found | None | None | MISSING | `layout/sidebar/sidebar.component.ts/html`, `core/services/report.service.ts` |
| Settings / AI toggles | No routed component | No settings/AI toggle service or endpoint found | None | None | MISSING | `app.routes.ts`, `layout/sidebar/sidebar.component.ts/html` |

## Dead / Unreachable / Placeholder Code

- `enterprise-task-ms/src/app/core/guards/role.guard.ts` exists but is an empty file and is not registered in `app.routes.ts`.
- `enterprise-task-ms/src/app/core/services/user.service.ts`, `role.service.ts`, `report.service.ts`, and `notification.service.ts` are empty files and have no reachable route usage.
- `enterprise-task-ms/src/app/features/task/store/task.effects.ts` is entirely commented out; NgRx actions/reducer exist but the runtime task workflow primarily uses `TaskService` + `TaskStateStore`.
- Sidebar has a disabled `Báo cáo` item with `route: null`; there is no reports route.
- There are no routed screens for users, roles, permission matrix, settings, notifications, or AI controls.

## DTO / Enum / Status Mismatches

- Task status constants are inconsistent in FE:
  - `TASK_STATUS_IDS.TAM_DUNG` reuses id `5` (`CHO_PHAN_HOI`).
  - `TASK_STATUS_IDS.CHO_PHE_DUYET` reuses id `6` (`HOAN_THANH`).
  - `TASK_STATUS_IDS.QUA_HAN` is `0`, but it is not in `TASK_STATUS_DEFINITIONS`.
  - Evidence: `enterprise-task-ms/src/app/core/constants/task-status.constants.ts`.
- FE mock task data references statuses such as `CHO_PHE_DUYET`, `TAM_DUNG`, and `QUA_HAN`, but the board definitions only render 9 defined statuses. Evidence: `enterprise-task-ms/src/app/core/mock-data/task.mock.ts`, `task-status.constants.ts`.
- FE task models allow mixed `EntityId`/`BigIntId` IDs and local numeric IDs, while backend task routes use GUID route constraints (`{id:guid}`) for task mutations. Evidence: `enterprise-task-ms/src/app/core/models/task.model.ts`, `backend/EnterpriseTask/EnterpriseTask.Api/Controllers/TasksController.cs`.
- Create task form collects `attachmentNames`, but `TaskService.createTask()` does not send attachments to `POST /api/tasks`; it only keeps them in local optimistic state. Evidence: `task-create-modal.component.ts/html`, `task.service.ts`.
- Edit task can mutate `attachmentNames`, `processingNotes`, subtasks, and local fields on the copied task; `TaskService.updateTask()` records local activity but does not call `TaskApiClient.updateTask()`. Status/subtask/comment/extension actions have separate API calls. Evidence: `task-edit-modal.component.ts/html`, `task.service.ts`, `task-api.client.ts`.
- Inter-department request UI uses `MockAuthUser` and compares `actor.departmentCode` with request `targetDepartmentId`; create payload casts department IDs to `Number(...)`. This mixed string/number approach can drift from backend DTO expectations. Evidence: `inter-department-request.component.ts`, `inter-department-request.service.ts`.

## UX Gaps Affecting Acceptance Criteria

- Dashboard has KPI cards and deadline/department buckets, but no time/department filters, no personal/manager/admin view switch, and no explicit loading/error state.
- Task list has search, priority filter, status presets, badges and drag/drop, but no server-side pagination, no server-side sort/filter, no URL/query state, and no explicit API loading/error indicator.
- Create/edit task validates title, assignee, and start/due date ordering, but attachment is a text token list, not upload. AI generate entry point is missing.
- Task detail covers status actions, progress, subtasks, extension requests, and activity timeline, but comment/mention support is not a complete interactive thread in the drawer UI and AI Summary/Risk Insight are missing.
- Permission behavior is mostly UI-side role/department checks. It is useful for UX but not proof of backend authorization.
- Several services use optimistic local updates and then `.catch(() => undefined)`, so API failures can be invisible to users.
- UI copy still says sample/mock mode in sidebar and dashboard, even though several services now call backend APIs. This can confuse acceptance testing.

## Backend Endpoint Coverage From Current FE

| Backend area | FE usage | Status | Evidence |
| --- | --- | --- | --- |
| Auth login | Called by login screen | PARTIAL | `AuthService.login()`, `AuthController.Login` |
| Auth me | Endpoint exists but not called by FE startup/session restore | PARTIAL | `AuthController.Me`, `AuthService.restoreUser()` |
| Tasks | List/options/activity and many actions called | PARTIAL | `TaskApiClient`, `TasksController` |
| Projects | GET projects called | PARTIAL | `ProjectService`, `ProjectsController` |
| Departments | GET cards called | PARTIAL | `DepartmentService`, `DepartmentsController` |
| Inter-department requests | Main request workflow endpoints called | PARTIAL | `InterDepartmentRequestService`, `InterDepartmentRequestsController` |
| Health/dev | Not user-facing FE features | NOT_APPLICABLE | `DatabaseHealthController`, `DevelopmentController` |
| Users/Roles/Reports/Settings | No backend controllers and no routed FE screens found | MISSING | Controller search under `backend/EnterpriseTask/EnterpriseTask.Api/Controllers` |

## Verification

- `dotnet build backend\EnterpriseTask\EnterpriseTask.slnx` - passed.
- `npm.cmd run build` in `enterprise-task-ms` - passed.
- `npm.cmd test -- --watch=false` in `enterprise-task-ms` - failed: `src/app/app.spec.ts:21` still expects an `h1` containing `Hello, enterprise-task-ms`, but rendered app no longer has that element/text.

## Conclusion

Frontend core screens are usable for the main demo workflows, but Prompt 07 is **PARTIAL overall**. The strongest coverage is task board/detail and inter-department request workflow. The main blockers for SRS-level acceptance are missing management/report/settings screens, incomplete role guard/permission matrix, client-side-only filtering/pagination, silent optimistic API failures, and DTO/status drift between FE state and backend contracts.
