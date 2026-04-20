import { TaskActivity } from '../../core/models/task-activity.model';
import { TASK_MOCK } from './task.mock';

const actions = [
  'status_change',
  'priority_change',
  'assignee_change',
  'progress_change',
  'comment_added',
  'attachment_added'
];

function randomItem<T>(arr: T[]): T {
  return arr[Math.floor(Math.random() * arr.length)];
}

function randomDate(start: Date, end: Date) {
  return new Date(
    start.getTime() + Math.random() * (end.getTime() - start.getTime())
  );
}

export const TASK_ACTIVITY_MOCK: TaskActivity[] = [];

let id = 1;

for (const task of TASK_MOCK) {

  const activityCount = Math.floor(Math.random() * 4) + 2;

  for (let i = 0; i < activityCount; i++) {

    TASK_ACTIVITY_MOCK.push({
      id: id++,

      taskId: task.id,

      userId: Math.floor(Math.random() * 5) + 1,

      actionType: randomItem(actions),

      oldValue: Math.random() > 0.5 ? 'Old Value' : undefined,
      newValue: Math.random() > 0.5 ? 'New Value' : undefined,

      createdAt: randomDate(
        new Date(2026, 2, 1),
        new Date()
      )
    });

  }

}