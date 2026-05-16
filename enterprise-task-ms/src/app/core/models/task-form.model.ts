export interface TaskFormSelectOption<T = string | number> {
  value: T;
  label: string;
  helper?: string;
}

export interface TaskMemberOption {
  id: number;
  label: string;
  role: string;
  departmentId: number;
}

export interface TaskDepartmentOption {
  id: number;
  label: string;
}

export interface TaskFormOptions {
  taskTypes: TaskFormSelectOption<string>[];
  departments: TaskDepartmentOption[];
  users: TaskMemberOption[];
  priorities: TaskFormSelectOption<number>[];
  urgencyLevels: TaskFormSelectOption<string>[];
  securityLevels: TaskFormSelectOption<string>[];
  sources: TaskFormSelectOption<string>[];
}

export interface CreateTaskInput {
  title: string;
  description?: string;
  taskType?: string;
  departmentId?: number;
  assigneeId?: number;
  collaboratorIds: number[];
  watcherIds: number[];
  startDate?: Date;
  dueDate?: Date;
  priorityId?: number;
  urgencyLevel?: string;
  securityLevel?: string;
  attachmentNames: string[];
  tags: string[];
  estimatedHours?: number;
  projectId?: number;
  parentTaskId?: number;
  source?: string;
}
