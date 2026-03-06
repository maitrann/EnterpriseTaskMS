export interface TaskActivity {
  id: number;
  taskId: number;
  userId: number;
  actionType?: string;
  oldValue?: string;
  newValue?: string;
  createdAt: Date;
}