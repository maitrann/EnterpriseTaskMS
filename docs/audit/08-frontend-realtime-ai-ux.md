# Prompt 08 - Frontend Realtime, Notification, Collaboration, and AI UX Audit

## Scope

- Source reviewed only; no application code changed.
- Focus areas: realtime/SignalR, notification UI, comment/mention/attachment collaboration, AI UX, and UX/security risks.
- Status values used: `IMPLEMENTED`, `PARTIAL`, `MISSING`, `CANNOT_VERIFY`, `NOT_APPLICABLE`.

## Realtime Event Mapping FE <-> BE

| Event / flow | Backend support found | Frontend support found | Status | Evidence |
| --- | --- | --- | --- | --- |
| SignalR/WebSocket infrastructure | No `AddSignalR`, `MapHub`, hub class, `IHubContext`, WebSocket, or SSE setup found | No `@microsoft/signalr` dependency and no `HubConnectionBuilder`/WebSocket/EventSource client found | MISSING | `backend/EnterpriseTask/EnterpriseTask.Api/Program.cs`, `enterprise-task-ms/package.json`, source search for SignalR/WebSocket/EventSource |
| Client connection after auth | None found | None found | MISSING | `enterprise-task-ms/src/app/core/services/auth.service.ts`, no realtime service files |
| Access token for realtime | None found | HTTP interceptor exists only for normal API calls | MISSING | `enterprise-task-ms/src/app/core/interceptors/auth.interceptor.ts` |
| Subscribe by user/task/group | None found | None found | MISSING | No hub/topic subscription code found |
| Reconnect/backoff | None found | None found | MISSING | No realtime connection lifecycle code found |
| Cleanup on destroy/logout | None found | `AuthService.logout()` clears localStorage and routes to `/login`; no realtime cleanup exists | MISSING | `enterprise-task-ms/src/app/core/services/auth.service.ts` |
| Task status/comment events | Backend has REST endpoints for task status/comments/subtasks/extensions | FE updates local state optimistically and reloads via REST after some actions | PARTIAL | `backend/.../TasksController.cs`, `enterprise-task-ms/src/app/core/services/task.service.ts`, `task-api.client.ts` |
| Inter-department message/status events | Backend has REST endpoints for request messages/status/assign/close | FE updates local state optimistically and reloads via REST after actions | PARTIAL | `backend/.../InterDepartmentRequestsController.cs`, `inter-department-request.service.ts` |
| Notification events | No notification controller/hub/job found | Notification model exists, service is empty, header badge is hard-coded | MISSING | `enterprise-task-ms/src/app/core/models/notification.model.ts`, `core/services/notification.service.ts`, `layout/header/header.component.html` |
| Fallback refresh after reconnect | No reconnect flow found | Services have manual/API reload methods, but not tied to reconnect | MISSING | `task.service.ts`, `inter-department-request.service.ts` |
| Connection error UI | None found | None found | MISSING | No realtime error state UI found |

## Notification UI Checklist

| Requirement | Status | Evidence | Notes |
| --- | --- | --- | --- |
| Bell/unread count | PARTIAL | `enterprise-task-ms/src/app/layout/header/header.component.html` | Header renders a notification button and badge `3`, but count is hard-coded and not bound to service/API state. |
| Notification list | MISSING | `enterprise-task-ms/src/app/core/services/notification.service.ts` length 0 | No dropdown/list component or route found. |
| Mark read / read all | MISSING | `notification.service.ts` length 0 | No method/API/UI found. |
| Click to reference | MISSING | Header notification button has no click handler | No reference navigation found. |
| New notification realtime | MISSING | No SignalR/WebSocket client | No event source. |
| Pagination/load more | MISSING | No notification list implementation | Not implemented. |
| Empty/error state | MISSING | No notification list implementation | Not implemented. |

## Collaboration Checklist

| Feature | Status | Evidence | Notes |
| --- | --- | --- | --- |
| Task feedback/comment composer | PARTIAL | `enterprise-task-ms/src/app/features/task/components/task-detail-drawer/task-detail-drawer.component.ts/html`, `task.service.ts`, `TasksController.cs` | Drawer has feedback textarea and calls `TaskService.addTaskFeedback()`, which maps to `POST /api/tasks/{id}/comments`. It is not a full threaded comment UI. |
| Mention autocomplete | MISSING | Task and request composer code | No `@mention` parsing/autocomplete found. User select controls exist for assignee/co-assignee/watcher, not mentions. |
| Internal/public marker | MISSING | Task/request comment UI | No internal/public visibility toggle found. |
| Timeline update | PARTIAL | `task-activity-timeline.component.ts/html`, `TaskApiClient.loadSnapshot()` | Timeline displays activity from task activities/local updates. It updates by local state and reload, not realtime. |
| Inter-department request messages | PARTIAL | `inter-department-request.component.ts/html`, `inter-department-request.service.ts`, `InterDepartmentRequestsController.cs` | Message thread and composer exist, with REST `POST /inter-department-requests/{id}/messages`. No realtime delivery. |
| Upload progress | MISSING | `task-create-modal.component.html`, `task-edit-modal.component.html` | Attachment UI is a text token list; no `<input type="file">`, upload progress, or upload API found. |
| Allowed type/size validation | MISSING | Attachment UI | No file upload validation found. |
| Download authorization error | MISSING | Attachment UI/API | No download link/API/error handling found. |
| XSS-safe rendering | PARTIAL | Angular templates use interpolation for task notes/messages/activity values | No `[innerHTML]`/`DomSanitizer` usage found in inspected UI. Angular interpolation reduces HTML injection risk, but user-generated text still needs backend sanitization/encoding policy. |

## AI Interaction Matrix

| AI interaction | Frontend UI | Backend/API found | Status | Evidence |
| --- | --- | --- | --- | --- |
| Smart Task Creator natural language input | None found | No AI/generate controller found | MISSING | `task-create-modal.component.ts/html`, controller list under `backend/.../Controllers` |
| AI Generate button | None found | No endpoint found | MISSING | Search for AI/generate/semantic terms in FE/BE |
| Loading/cancel/error for AI generation | None found | None found | MISSING | No AI state/service found |
| Draft fields filled by AI and editable before save | No AI draft fill found; manual task draft exists | None found | MISSING | `task-create-modal.component.ts` |
| AI suggestion label | No AI label found | None found | MISSING | Source search for AI/suggestion |
| Task detail AI Summary button | None found | No summary endpoint/controller found | MISSING | `task-detail-drawer.component.ts/html`, controller list |
| AI Summary permission-aware display | None found | None found | MISSING | No task detail AI UI/service |
| AI Summary loading/error/retry | None found | None found | MISSING | No AI UI state |
| Risk Insight badge/reason/action | None found | No risk insight endpoint/controller found | MISSING | `task-detail-drawer.component.ts/html`, source search |
| Advanced semantic search/chatbot/smart assignment/classification/email-to-task | Header has plain search input and task board has client-side search only | No AI/search controller found | MISSING | `layout/header.component.html`, `task-board.component.ts`, backend controller list |

## Accessibility and UX Notes

| Area | Status | Evidence | Notes |
| --- | --- | --- | --- |
| Dialog/form labels and errors | PARTIAL | Task create/edit/detail and auth forms | Many fields have labels/placeholders and validation messages. Some custom dialogs/drawers do not show explicit focus-trap/ARIA dialog handling. |
| Keyboard/focus | PARTIAL | `task-detail-drawer.component.html`, `custom-select.component.ts/html` | Some keyboard handlers exist for inline subtask edit (`enter`/`escape`), but drawer/modal focus management is not evident. |
| Responsive minimum | CANNOT_VERIFY | SCSS exists for screens, build passes | Static code review cannot fully verify responsive behavior without visual test pass. |
| Date/time timezone | PARTIAL | `toDateInputValue()`, `fromDateInputValue()`, Angular date pipe | Date-only values are converted with local `T00:00:00`; no explicit timezone policy found. |
| Status/priority not only color | PARTIAL | Task cards/detail display labels plus classes | Labels exist, but several badges still rely heavily on color classes for quick distinction. |

## Security / UX Risks

- **Stale state risk:** task/request services often update local state optimistically and then call API with `.catch(() => undefined)`. If backend rejects or network fails, UI can remain stale without user-visible error. Evidence: `enterprise-task-ms/src/app/core/services/task.service.ts`, `inter-department-request.service.ts`.
- **No realtime consistency:** there is no FE/BE realtime transport, so concurrent edits/comments/messages from other users do not appear until explicit reload/API refresh.
- **Hard-coded notification count:** header shows unread badge `3` without service/API binding, which can mislead users during acceptance testing. Evidence: `layout/header/header.component.html`.
- **Unauthorized UI call risk:** FE permission checks are role/department string checks and are useful for UX only. Backend authorization still matters for REST calls. Evidence: `auth.service.ts`, `task-detail-drawer.component.ts`, backend controllers with `[Authorize]`.
- **Attachment gap:** text-only attachment names create a false affordance; there is no upload/download, allowed-type/size validation, progress, or authorization error handling.
- **No duplicate connection risk yet:** duplicate SignalR connection is not currently a runtime risk because no realtime connection exists, but it becomes a design risk when realtime is added.
- **XSS posture is mostly safe by template interpolation:** no `[innerHTML]`/`DomSanitizer` usage found in inspected frontend. Continue avoiding raw HTML rendering for comments/messages/notes.
- **AI trust boundary missing:** because no AI UI exists, there is no AI output labeling, retry/error handling, or distinction between suggestion and official task data.

## Verification

- `dotnet build backend\EnterpriseTask\EnterpriseTask.slnx` - passed.
- `npm.cmd run build` in `enterprise-task-ms` - passed.
- `npm.cmd test -- --watch=false` in `enterprise-task-ms` - failed: `src/app/app.spec.ts:21` still expects an `h1` containing `Hello, enterprise-task-ms`, but the rendered app does not provide that element/text.

## Conclusion

Prompt 08 is **MISSING/PARTIAL overall**:

- Realtime/SignalR: **MISSING**.
- Notification UI: **MISSING**, with only a hard-coded header badge.
- Collaboration: **PARTIAL**, because REST-based task feedback, activity timeline, subtasks, extension requests, and inter-department request messages exist, but there is no realtime delivery, mention support, upload/download, or robust error handling.
- AI UX: **MISSING**, with no Smart Task Creator, AI Summary, Risk Insight, semantic search/chatbot, or AI suggestion labeling found in frontend or backend controllers.
