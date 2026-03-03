export interface Task {
  id: number;
  title: string;
  description: string;
  status: 'todo' | 'inprogress' | 'done';
  priority: 'low' | 'medium' | 'high';
  departmentId: number;
  assignedUserId: number;
  deadline: Date;
}