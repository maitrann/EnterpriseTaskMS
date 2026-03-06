export interface Project {
  id: number;
  name: string;
  description?: string;
  departmentId?: number;
  startDate?: Date;
  endDate?: Date;
  status?: string;
  createdBy?: number;
  createdAt: Date;
}
export interface ProjectMember {
  id: number;
  projectId: number;
  userId: number;
  role?: string;
  joinedAt: Date;
}