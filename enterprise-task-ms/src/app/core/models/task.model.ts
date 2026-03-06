export interface Task {
  id: number;
  projectId: number;
  parentTaskId?: number;

  title: string;
  description?: string;

  statusId?: number;
  priorityId?: number;

  reporterId?: number;
  assigneeId?: number;

  startDate?: Date;
  dueDate?: Date;

  progress: number;

  estimatedHours?: number;
  actualHours?: number;

  createdAt: Date;
  updatedAt?: Date;
}