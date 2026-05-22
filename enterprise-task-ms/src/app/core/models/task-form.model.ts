import { BigIntId, EntityId, Uuid } from './common-id.model';

export interface TaskFormSelectOption<T = string | number> { value: T; label: string; helper?: string; }
export interface TaskMemberOption { id: EntityId; label: string; role: string; departmentId: BigIntId; }
export interface TaskDepartmentOption { id: BigIntId; label: string; }
export interface TaskFormOptions {
  taskTypes: TaskFormSelectOption<string>[];
  departments: TaskDepartmentOption[];
  users: TaskMemberOption[];
  priorities: TaskFormSelectOption<BigIntId>[];
  urgencyLevels: TaskFormSelectOption<string>[];
  securityLevels: TaskFormSelectOption<string>[];
  sources: TaskFormSelectOption<string>[];
}

export interface CreateTaskInput {
  title: string;
  description?: string;
  taskType?: string;
  departmentId?: BigIntId;
  assigneeId?: EntityId;
  collaboratorIds?: EntityId[];
  coAssigneeIds?: Uuid[];
  watcherIds: EntityId[];
  startDate?: Date;
  dueDate?: Date;
  priorityId?: BigIntId;
  urgencyLevel?: string;
  securityLevel?: string;
  attachmentNames: string[];
  tags: string[];
  estimatedHours?: number;
  projectId?: EntityId;
  parentTaskId?: EntityId;
  source?: string;
}
