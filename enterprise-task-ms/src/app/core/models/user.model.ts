import { BigIntId, EntityId } from './common-id.model';

export interface User {
  id: EntityId;
  username: string;
  email?: string;
  fullName?: string;
  role?: string;
  avatarUrl?: string;
  departmentId?: BigIntId;
  isActive: boolean;
  createdAt: Date;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

export interface UserListQuery {
  page?: number;
  pageSize?: number;
  search?: string;
  departmentId?: BigIntId;
  isActive?: boolean;
}

export interface AdminUser {
  id: string;
  employeeCode?: string;
  email?: string;
  fullName?: string;
  avatarUrl?: string;
  departmentId?: BigIntId;
  departmentName?: string;
  managerId?: string;
  managerName?: string;
  jobTitle?: string;
  isActive: boolean;
  roleCodes: string[];
  roleNames: string[];
  scopedDepartmentIds: BigIntId[];
  scopedDepartmentNames: string[];
  createdAt: string;
  updatedAt?: string;
}
