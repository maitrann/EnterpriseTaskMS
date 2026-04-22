import { User } from '../models/user.model';

export type MockAuthUser = User & {
  password: string;
};

export const MOCK_ADMIN_USER: MockAuthUser = {
  id: 1,
  username: 'admin',
  email: 'admin@etms.local',
  fullName: 'Tran Thi Thao',
  role: 'System Admin',
  isActive: true,
  createdAt: new Date('2026-04-01T08:00:00'),
  password: 'Admin@123'
};
