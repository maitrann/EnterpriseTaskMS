export type TaskExtensionRequestStatus = 'pending' | 'approved' | 'rejected';

export interface TaskExtensionRequest {
  id: number;
  requestedDueDate: Date;
  reason: string;
  status: TaskExtensionRequestStatus;
  requestedByUserId: number;
  requestedAt: Date;
  reviewedByUserId?: number;
  reviewedAt?: Date;
  reviewNote?: string;
}

export interface Task {
  id: number;
  code: string;
  projectId: number;
  parentTaskId?: number;

  title: string;
  description?: string;
  taskType?: string;
  departmentId?: number;

  statusId?: number;
  priorityId?: number;
  urgencyLevel?: string;
  securityLevel?: string;

  reporterId?: number;
  assigneeId?: number;
  collaboratorIds?: number[];
  watcherIds?: number[];

  startDate?: Date;
  dueDate?: Date;

  progress: number;
  source?: string;
  attachmentNames?: string[];
  tags?: string[];
  processingNotes?: string[];
  extensionRequests?: TaskExtensionRequest[];

  estimatedHours?: number;
  actualHours?: number;

  createdAt: Date;
  updatedAt?: Date;
}
