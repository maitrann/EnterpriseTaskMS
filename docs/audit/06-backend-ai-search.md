# 06 - Backend AI, AI Safety and Search Audit

## 1. AI Feature Matrix

| AI feature | Status | Endpoint | Service/provider | Authorization | Validation | Logging | UI integration evidence |
|---|---|---|---|---|---|---|---|
| AI-01 Smart Task Creator | MISSING | No AI/task-draft endpoint found | No AI provider/package/service found | N/A | N/A | `ai_request_logs` table exists but no runtime writes | No AI creator UI found; task create is manual |
| AI-02 Task Summarization | MISSING | No summary endpoint found | No AI provider/package/service found | N/A | N/A | `ai_request_logs` and `ai_task_insights.summary` exist but no runtime writes | No task summary AI UI found |
| AI-03 Priority and Risk Suggestion | MISSING | No risk endpoint found | No AI provider/package/service found | N/A | N/A | `ai_task_insights.risk_level`, `risk_reason`, `suggested_action` exist but no runtime writes | No AI risk suggestion UI found |
| AI-04 Smart Assignment | MISSING | No smart assignment endpoint found | No AI provider/package/service found | N/A | N/A | Feature setting seed exists | No UI evidence found |
| AI-05 Semantic Search/RAG | PARTIAL | No semantic search endpoint found | No embedding provider/indexing job found | DB RLS policy exists for accessible embeddings | No chunking/query validation code found | `embedding_index` table exists; no runtime writes | Frontend task board has client-side text search only |
| AI-06 Internal Chatbot | MISSING | No chatbot endpoint found | No AI provider/package/service found | N/A | N/A | No runtime log writes | No chatbot UI found |
| AI-07 Auto Classification | PARTIAL | No classification endpoint found | No AI provider/package/service found | N/A | N/A | Inter-request columns for AI classification exist | No runtime classification UI found |
| AI-08 Meeting/Email to Task | MISSING | No endpoint found | No AI/email ingestion provider found | N/A | N/A | No runtime log writes | No UI evidence found |
| AI feature toggles | PARTIAL | No settings API found | DB seed exists | DB RLS says admin manages settings | No backend validation service found | No runtime usage found | No UI evidence found |
| AI request metadata logging | PARTIAL | No API found | DB table exists | RLS permits own insert/admin read | No backend logging wrapper found | `ai_request_logs` table/indexes exist | No UI evidence found |

## 2. Data Flow for MVP Features

### AI-01 Smart Task Creator

Observed runtime flow: not implemented.

Expected flow from prompt would be natural-language input -> authorization -> feature toggle -> provider call -> structured draft output -> validation -> user confirmation -> normal task create. No backend controller, application service, provider abstraction, structured output DTO, validation, or draft-only endpoint was found.

### AI-02 Task Summarization

Observed runtime flow: not implemented.

The schema has `ai_task_insights.summary`, but there is no backend API that checks `CanAccessTask`, gathers task/comments/files/history, builds a prompt, calls a provider, or stores/returns a summary. No evidence was found that task data is sent to an AI provider.

### AI-03 Priority and Risk Suggestion

Observed runtime flow: not implemented.

The schema has `ai_task_insights.risk_level`, `risk_reason`, and `suggested_action`, but no code computes workload/deadline/progress/comment inputs, calls AI, or returns suggestion-only results. No code was found that automatically changes priority or deadline through AI.

## 3. Data Sent to Provider

No runtime AI provider integration was found. Therefore no actual task, comment, file metadata, status history, user workload, or inter-request data is currently sent to an AI provider by backend code.

Schema-level data that appears intended for AI/search:

- `ai_feature_settings`: feature toggles.
- `ai_request_logs`: user, feature code, reference type/id, input hash, status, error, token input/output.
- `ai_task_insights`: risk, reason, suggested action, summary, generated metadata.
- `embedding_index`: entity type/id, text chunk, 1536-dimensional vector, metadata.
- `inter_department_requests`: AI classification columns.

## 4. Security and Privacy Risks

| Risk | Evidence | Status |
|---|---|---|
| No authorization boundary for AI APIs | No AI endpoints exist | MISSING |
| No feature-toggle enforcement | `ai_feature_settings` exists but no backend code reads it | MISSING |
| No provider abstraction | No OpenAI/Azure/Semantic Kernel/provider package or service found | MISSING |
| No config/secrets model for AI provider | No AI provider config keys found in appsettings or code search | MISSING |
| No timeout/retry/cancellation policy | No provider calls or resilience package found | MISSING |
| No rate limit/quota | No rate limiter or quota service found | MISSING |
| No prompt size/token control | No prompt builder found | MISSING |
| No structured output validation | No AI output DTO/schema validation found | MISSING |
| No raw prompt minimization policy in code | No AI prompt logging/storage code found | CANNOT_VERIFY |
| AI failure isolation | No AI runtime exists, so no failure isolation is implemented | MISSING |
| AI suggestion labeling | No AI suggestion UI/backend response found | MISSING |
| Mock/test provider | No test/mock AI provider found | MISSING |

## 5. Search Architecture

| Search capability | Status | Evidence | Gap |
|---|---|---|---|
| Task board text search | PARTIAL | `TaskBoardComponent.filteredTasks` filters title/description client-side | Not backend search; only currently loaded tasks |
| Backend task search | MISSING | `TasksController.Get` has no search parameter; `ITaskQueries.GetTasksAsync` accepts only actor user id | No server-side search/filter/pagination |
| Full-text search | MISSING | No `to_tsvector`, `ts_rank`, GIN text index, or full-text query found | Not implemented |
| Semantic search table | PARTIAL | `embedding_index` table with vector column exists | No ingestion/query endpoint |
| Vector index | PARTIAL | HNSW vector index is commented out in SQL | Not active |
| Embedding generation | MISSING | No provider/job writes embeddings | No chunking/sync pipeline |
| RAG retrieval | MISSING | No retrieval, reranking, prompt assembly, or citation path found | Not implemented |
| Authorization filter for embeddings | PARTIAL | RLS policy reads embeddings for admin/director, accessible task, or accessible inter-request | Only DB policy; no backend query uses it |

## 6. Gaps to Meet Acceptance Criteria

- Add authenticated AI endpoints for task draft, summary, and risk suggestion.
- Add provider abstraction and config via secrets/environment.
- Enforce `ai_feature_settings` before each AI call.
- Validate structured AI output and return draft/suggestion only.
- Require user confirmation before persisting task changes.
- Authorize task/request access before building prompts.
- Avoid sending inaccessible comments/files/history to provider.
- Add timeout, retry, cancellation, rate limit and quota controls.
- Write `ai_request_logs` without storing raw sensitive prompt unless required.
- Add mock provider and tests for success/failure/blocked scenarios.
- Implement server-side search with filters and pagination.
- Implement embedding chunking, sync job, vector index, and authorized semantic search query.
- Add UI/backend labels that returned content is AI-generated suggestion.

## 7. Code, Config, DB and Test Evidence

| Area | Evidence |
|---|---|
| Backend controllers | Only auth, tasks, departments, projects, inter-department requests, health, dev controllers were found; no AI/search controller |
| Backend packages | No OpenAI, Azure OpenAI, Semantic Kernel, vector/search provider, resilience or AI SDK package found |
| Backend runtime search | `TasksController.Get` and `ITaskQueries.GetTasksAsync` do not accept search/filter parameters |
| Frontend search | `TaskBoardComponent.filteredTasks` performs client-side title/description search |
| DB AI schema | `ai_feature_settings`, `ai_request_logs`, `ai_task_insights`, `embedding_index` |
| DB vector extension | `CREATE EXTENSION IF NOT EXISTS vector WITH SCHEMA extensions` |
| DB vector index | HNSW index exists only as commented SQL |
| DB policies | AI settings/logs/insights/embeddings have RLS policies |
| Tests | No backend AI/search tests found; frontend test suite unrelated and currently failing title test |

## 8. Build and Test Result

| Command | Result | Notes |
|---|---|---|
| `dotnet build backend\EnterpriseTask\EnterpriseTask.slnx` | PASS | 0 warnings, 0 errors |
| `npm.cmd run build` in `enterprise-task-ms` | PASS | Angular production build succeeded |
| `npm.cmd test -- --watch=false` in `enterprise-task-ms` | FAIL | `src/app/app.spec.ts` test `should render title` fails because `querySelector('h1')?.textContent` is `undefined` |

## 9. Files Reviewed

- `backend/EnterpriseTask/EnterpriseTask.Api/Controllers/*`
- `backend/EnterpriseTask/EnterpriseTask.Api/Program.cs`
- `backend/EnterpriseTask/EnterpriseTask.Api/appsettings.json`
- `backend/EnterpriseTask/EnterpriseTask.Api/appsettings.Development.json`
- `backend/EnterpriseTask/EnterpriseTask.Api/EnterpriseTask.Api.csproj`
- `backend/EnterpriseTask/EnterpriseTask.Application/EnterpriseTask.Application.csproj`
- `backend/EnterpriseTask/EnterpriseTask.Infrastructure/EnterpriseTask.Infrastructure.csproj`
- `backend/EnterpriseTask/EnterpriseTask.Application/Tasks/ITaskQueries.cs`
- `backend/EnterpriseTask/EnterpriseTask.Infrastructure/Tasks/PostgresTaskQueries.cs`
- `enterprise-task-ms/src/app/features/task/components/task-board/task-board.component.ts`
- `enterprise-task-ms/src/app/shared/ui/custom-select/custom-select.component.ts`
- `supabase_schema_v2_clean.sql`
