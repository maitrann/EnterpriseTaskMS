import { TASK_STATUS_DEFINITIONS } from '../constants/task-status.constants';
import { TaskStatus } from '../models/task-status.model';

export const TASK_STATUS_MOCK: TaskStatus[] = TASK_STATUS_DEFINITIONS.map((status, index) => ({
  id: status.id,
  name: status.label,
  orderIndex: index + 1,
  color: status.border
}));
