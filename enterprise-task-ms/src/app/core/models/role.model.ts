export interface Role {
  id: number;
  name: string;
  description?: string;
}
export interface UserRole {
  id: number;
  userId: number;
  roleId: number;
}