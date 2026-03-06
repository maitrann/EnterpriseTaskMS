export interface TaskComment {
  id: number;
  taskId: number;
  userId: number;
  content?: string;
  createdAt: Date;
  updatedAt?: Date;
}