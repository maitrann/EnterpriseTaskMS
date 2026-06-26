export interface DepartmentCard {
  name: string;
  description: string;
  members: number;
  activeTasks: number;
  completedTasks: number;
  lead: string;
  sla: string;
  tone: 'blue' | 'amber' | 'emerald' | 'slate';
}

export interface DepartmentOption {
  id: number;
  name: string;
}
