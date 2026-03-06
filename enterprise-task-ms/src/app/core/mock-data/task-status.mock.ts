import { TaskStatus } from '../models/task-status.model';

export const TASK_STATUS_MOCK: TaskStatus[] = [
  {
    id: 1,
    name: 'Todo',
    orderIndex: 1,
    color: '#6B7280'
  },
  {
    id: 2,
    name: 'In Progress',
    orderIndex: 2,
    color: '#3B82F6'
  },
  {
    id: 3,
    name: 'Review',
    orderIndex: 3,
    color: '#F59E0B'
  },
  {
    id: 4,
    name: 'Done',
    orderIndex: 4,
    color: '#10B981'
  }
];