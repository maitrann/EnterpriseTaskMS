export type RequestType =
  | 'procurement'
  | 'asset'
  | 'it-support'
  | 'payment'
  | 'recruitment'
  | 'communication-design'
  | 'legal';

export type RequestStatus =
  | 'new'
  | 'received'
  | 'processing'
  | 'waiting-requester'
  | 'waiting-target'
  | 'done'
  | 'closed'
  | 'rejected';
export type RequestPriority = 'Low' | 'Medium' | 'High' | 'Critical';

export interface RequestDepartmentRef {
  id: string;
  name: string;
}

export interface RequestOwnerRef {
  id: string;
  name: string;
  departmentId: string;
  departmentName: string;
}

export interface RequestSlaPolicy {
  key: RequestType;
  label: string;
  targetHours: number;
  warnHours: number;
}

export interface RequestSlaSnapshot {
  policyKey: RequestType;
  policyLabel: string;
  targetHours: number;
  warnHours: number;
  startedAt: string;
  dueAt: string;
  remainingHours: number;
  breached: boolean;
}

export interface RequestMessage {
  id: string;
  authorName: string;
  authorRole: 'requester' | 'processor' | 'coordinator';
  authorDepartment: string;
  createdAt: string;
  body: string;
}

export interface InterDepartmentRequest {
  id: string;
  code: string;
  type: RequestType;
  title: string;
  description: string;
  requesterDepartment: string;
  requesterDepartmentId: string;
  requesterName: string;
  requesterUserId: number;
  targetDepartment: string;
  targetDepartmentId: string;
  owner: string | null;
  ownerId: string | null;
  priority: RequestPriority;
  status: RequestStatus;
  createdAt: string;
  updatedAt: string;
  receivedAt: string | null;
  closedAt: string | null;
  dueDate: string;
  sla: RequestSlaSnapshot;
  formValues: Record<string, string>;
  latestMessage: string;
  note: string;
  messages: RequestMessage[];
}

export interface CreateInterDepartmentRequest {
  type: RequestType;
  title: string;
  description: string;
  requesterDepartment: string;
  requesterDepartmentId: string;
  requesterName: string;
  requesterUserId: number;
  targetDepartment: string;
  targetDepartmentId: string;
  priority: RequestPriority;
  dueDate: string;
  formValues: Record<string, string>;
  note: string;
}
