export interface DepartmentCard {
  name: string;
  description: string;
  members: number;
  activeTasks: number;
  completedTasks: number;
  lead: string;
  sla: string;
  tone: 'blue' | 'amber' | 'emerald' | 'slate';
}

export interface DepartmentOption {
  id: number;
  name: string;
}

export interface DepartmentListItem {
  id: number;
  companyId: number;
  code: string | null;
  name: string;
  description: string | null;
  parentDepartmentId: number | null;
  parentDepartmentName: string | null;
  managerId: string | null;
  managerName: string | null;
  memberCount: number;
  activeTaskCount: number;
  isActive: boolean;
  createdAt: string;
  updatedAt: string | null;
}

export interface DepartmentTreeNode extends DepartmentListItem {
  children: DepartmentTreeNode[];
}

export interface DepartmentCreateRequest {
  companyId: number;
  code: string | null;
  name: string;
  description: string | null;
  parentDepartmentId: number | null;
  managerId: string | null;
}

export interface DepartmentUpdateRequest {
  code: string | null;
  name: string;
  description: string | null;
  parentDepartmentId: number | null;
}

export interface DepartmentManagerAssignmentRequest {
  managerId: string | null;
}
