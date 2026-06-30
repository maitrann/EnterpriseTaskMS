import { BigIntId, EntityId, Uuid } from './common-id.model';
import { SubTask } from './subtask.model';

export type TaskExtensionRequestStatus = 'pending' | 'approved' | 'rejected';
export type TaskAssignmentType = 'assignee' | 'co_assignee' | 'watcher';

export interface TaskAssignment {
  userId: Uuid;
  assignmentType: TaskAssignmentType;
  assignedBy?: Uuid;
  assignedAt: Date;
}

export interface TaskExtensionRequest {
  id: EntityId;
  requestedDueDate: Date;
  reason: string;
  status: TaskExtensionRequestStatus;
  requestedByUserId?: EntityId;
  requestedAt: Date;
  reviewedByUserId?: EntityId;
  reviewedAt?: Date;
  reviewNote?: string;
}

export interface Task {
  id: EntityId;
  code: string;
  projectId?: EntityId;
  parentTaskId?: EntityId;
  title: string;
  description?: string;
  taskType?: string;
  departmentId?: BigIntId;
  statusId?: BigIntId;
  priorityId?: BigIntId;
  urgencyLevel?: string;
  securityLevel?: string;
  reporterId?: EntityId;
  createdBy?: EntityId;
  // New schema shape
  assignments?: TaskAssignment[];
  // Transitional legacy shape
  assigneeId?: EntityId;
  collaboratorIds?: EntityId[];
  watcherIds?: EntityId[];
  startDate?: Date;
  dueDate?: Date;
  progress: number;
  source?: string;
  attachmentNames?: string[];
  tags?: string[];
  processingNotes?: string[];
  extensionRequests?: TaskExtensionRequest[];
  subtasks?: SubTask[];
  subtaskProgressAutoSync?: boolean;
  parentCompletionSuggested?: boolean;
  estimatedHours?: number;
  actualHours?: number;
  archivedAt?: Date;
  archivedBy?: EntityId;
  archiveReason?: string;
  createdAt: Date;
  updatedAt?: Date;
}
