import { Task } from '../../core/models/task.model';
const statuses = [1, 2, 3, 4]; // Todo, InProgress, Review, Done
const priorities = [1, 2, 3, 4];

function randomItem<T>(arr: T[]): T {
  return arr[Math.floor(Math.random() * arr.length)];
}

function randomDate(start: Date, end: Date) {
  return new Date(
    start.getTime() + Math.random() * (end.getTime() - start.getTime())
  );
}

const now = new Date();

export const TASK_MOCK: Task[] = Array.from({ length: 50 }).map((_, i) => {

  const start = randomDate(new Date(2026, 2, 1), new Date(2026, 2, 20));
  const due = randomDate(new Date(2026, 2, 20), new Date(2026, 3, 5));

  return {
    id: i + 1,

    projectId: 1,

    parentTaskId: undefined,

    title: `Task #${i + 1}`,
    description: `This is mock task number ${i + 1}`,

    statusId: randomItem(statuses),

    priorityId: randomItem(priorities),

    reporterId: 1,
    assigneeId: Math.floor(Math.random() * 5) + 1,

    startDate: start,
    dueDate: due,

    progress: Math.floor(Math.random() * 100),

    estimatedHours: Math.floor(Math.random() * 40) + 4,
    actualHours: Math.floor(Math.random() * 40),

    createdAt: now,
    updatedAt: now
  };
});