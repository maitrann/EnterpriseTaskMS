export interface Project {
  id: number;
  code?: string;
  name: string;
  description?: string;
  departmentId?: number;
  ownerId?: number;
  startDate?: Date;
  endDate?: Date;
  status?: string;
  createdBy?: number;
  createdAt: Date;
  updatedAt?: Date;
}
export interface ProjectMember {
  id: number;
  projectId: number;
  userId: number;
  role?: string;
  joinedAt: Date;
}
