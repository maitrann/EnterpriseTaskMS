import { EntityId } from './common-id.model';
export interface SubTask { id: EntityId; taskId: EntityId; title: string; assigneeId?: EntityId; dueDate?: Date; progress: number; done: boolean; createdAt: number; updatedAt?: number; completedAt?: number; order: number; }
export interface SubTaskInput { title: string; assigneeId?: EntityId; dueDate?: Date; progress?: number; }
