export interface Department {
  id: number;
  companyId: number;
  name: string;
  parentDepartmentId?: number;
  createdAt: Date;
}