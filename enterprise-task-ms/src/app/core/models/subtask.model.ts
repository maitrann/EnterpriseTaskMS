export interface SubTask {
  id: number;
  taskId: number;
  title: string;
  assigneeId?: number;
  dueDate?: Date;
  progress: number;
  done: boolean;
  createdAt: number;
  updatedAt?: number;
  completedAt?: number;
  order: number;
}

export interface SubTaskInput {
  title: string;
  assigneeId?: number;
  dueDate?: Date;
  progress?: number;
}
