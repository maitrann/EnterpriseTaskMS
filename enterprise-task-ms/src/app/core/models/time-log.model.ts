export interface TimeLog {
  id: number;
  taskId: number;
  userId: number;
  hours?: number;
  description?: string;
  logDate?: Date;
  createdAt: Date;
}