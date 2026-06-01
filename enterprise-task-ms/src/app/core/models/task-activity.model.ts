import { EntityId } from './common-id.model';

export interface TaskActivity {
  id: number;
  taskId: EntityId;
  userId: EntityId;
  actionType?: string;
  oldValue?: string;
  newValue?: string;
  createdAt: Date;
}
