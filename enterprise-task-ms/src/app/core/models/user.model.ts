export interface User {
  id: number;
  username: string;
  email?: string;
  fullName?: string;
  avatarUrl?: string;
  departmentId?: number;
  isActive: boolean;
  createdAt: Date;
}