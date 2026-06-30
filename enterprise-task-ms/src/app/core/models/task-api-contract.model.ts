import { BigIntId, EntityId } from './common-id.model';
import { Task } from './task.model';

export type ApiDate = string | null;

export interface TaskCreateResponse {
  id: EntityId;
}

export interface TaskDuplicateResponse extends TaskCreateResponse {
  task: Task;
}

export type TaskMutationResponse = void;

export interface CreateTaskRequest {
  title: string;
  description?: string;
  taskType?: string;
  projectId?: EntityId;
  parentTaskId?: EntityId;
  departmentId?: BigIntId;
  assigneeId?: EntityId;
  priorityId?: BigIntId;
  startDate?: ApiDate;
  dueDate?: ApiDate;
  estimatedHours?: number;
  source?: string;
  urgencyLevel?: string;
  securityLevel?: string;
  collaboratorIds?: EntityId[];
  watcherIds?: EntityId[];
  tags?: string[];
}

export interface UpdateTaskRequest extends CreateTaskRequest {
  statusId?: BigIntId;
  progress: number;
  actualHours?: number;
}

export interface UpdateTaskStatusRequest {
  statusId: BigIntId;
  note?: string;
}

export interface AddTaskCommentRequest {
  content: string;
}

export interface TransferTaskAssigneeRequest {
  assigneeId: EntityId;
  reason?: string;
}

export interface DuplicateTaskRequest {
  title?: string;
  resetPeople: boolean;
  resetAttachments: boolean;
}

export interface ArchiveTaskRequest {
  reason?: string;
}

export interface CreateTaskExtensionRequest {
  requestedDueDate: ApiDate;
  reason: string;
}

export interface ReviewTaskExtensionRequest {
  approved: boolean;
  reviewNote?: string;
}

export interface CreateSubTaskRequest {
  title: string;
  assigneeId?: EntityId;
  dueDate?: ApiDate;
  progress?: number;
}

export interface UpdateSubTaskRequest {
  title?: string;
  assigneeId?: EntityId;
  dueDate?: ApiDate;
  progress?: number;
  done?: boolean;
}
