import { BigIntId, EntityId } from './common-id.model';
export interface Project { id: EntityId; code?: string; name: string; description?: string; departmentId?: BigIntId; ownerId?: EntityId; startDate?: Date; endDate?: Date; status?: 'planning' | 'active' | 'on_hold' | 'completed' | 'cancelled' | string; createdBy?: EntityId; createdAt: Date; updatedAt?: Date; }
export interface ProjectMember { projectId: EntityId; userId: EntityId; role?: string; joinedAt: Date; }
