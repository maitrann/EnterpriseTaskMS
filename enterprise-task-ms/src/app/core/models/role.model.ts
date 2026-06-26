export interface Role {
  id: number;
  code: string;
  name: string;
  description?: string;
  permissions: Permission[];
  createdAt: string;
}

export interface Permission {
  id: number;
  code: string;
  name: string;
  module: string;
  description?: string;
  createdAt: string;
}

export interface UserRole {
  id: number;
  userId: string;
  roleId: number;
}
