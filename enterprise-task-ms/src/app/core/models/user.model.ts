import { BigIntId, EntityId } from './common-id.model';
export interface User { id: EntityId; username: string; email?: string; fullName?: string; role?: string; avatarUrl?: string; departmentId?: BigIntId; isActive: boolean; createdAt: Date; }
