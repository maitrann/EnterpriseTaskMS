export type RequestStatus = 'new' | 'processing' | 'waiting' | 'done';
export type RequestPriority = 'Low' | 'Medium' | 'High' | 'Critical';

export interface InterDepartmentRequest {
  id: string;
  title: string;
  requesterDepartment: string;
  targetDepartment: string;
  owner: string;
  priority: RequestPriority;
  status: RequestStatus;
  sla: string;
  dueDate: string;
  note: string;
}

export interface CreateInterDepartmentRequest {
  title: string;
  requesterDepartment: string;
  targetDepartment: string;
  owner: string;
  priority: RequestPriority;
  dueDate: string;
  note: string;
}
