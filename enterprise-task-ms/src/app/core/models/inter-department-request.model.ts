import { BigIntId, EntityId, Uuid } from './common-id.model';

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

export type RequestPriority = 'low' | 'medium' | 'high' | 'critical' | 'Low' | 'Medium' | 'High' | 'Critical';

export interface RequestDepartmentRef { id: BigIntId | string; name: string; }
export interface RequestOwnerRef { id: Uuid | string; name: string; departmentId: BigIntId | string; departmentName: string; }
export interface RequestSlaPolicy { key: RequestType; label: string; targetHours: number; warnHours: number; }
export interface RequestSlaSnapshot { policyKey: RequestType; policyLabel: string; targetHours: number; warnHours: number; startedAt: string; dueAt: string; remainingHours: number; breached: boolean; }
export interface RequestMessage { id: Uuid | string; authorName: string; authorRole: 'requester' | 'processor' | 'coordinator'; authorDepartment: string; createdAt: string; body: string; }

export interface InterDepartmentRequest {
  id: Uuid | string;
  code: string;
  type: RequestType;
  title: string;
  description: string;
  requesterDepartment: string;
  requesterDepartmentId: string;
  requesterName: string;
  requesterUserId: EntityId;
  targetDepartment: string;
  targetDepartmentId: string;
  owner: string | null;
  ownerId: Uuid | string | null;
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
  requesterUserId: EntityId;
  targetDepartment: string;
  targetDepartmentId: string;
  priority: RequestPriority;
  dueDate: string;
  formValues: Record<string, string>;
  note: string;
}
