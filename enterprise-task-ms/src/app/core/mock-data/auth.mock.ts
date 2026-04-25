import { User } from '../models/user.model';

export type MockAuthUser = User & {
  password: string;
  departmentCode?: string;
  departmentName?: string;
  requestAccessRole?: 'requester' | 'receiver' | 'coordinator';
  canCreateRequest?: boolean;
};

export const MOCK_ADMIN_USER: MockAuthUser = {
  id: 1,
  username: 'admin',
  email: 'admin@etms.local',
  fullName: 'Truong Tran',
  role: 'System Admin',
  departmentId: 1,
  isActive: true,
  createdAt: new Date('2026-04-01T08:00:00'),
  password: 'Admin@123'
};

export const MOCK_AUTH_USERS: MockAuthUser[] = [
  MOCK_ADMIN_USER,
  {
    id: 2,
    username: 'chau.hr',
    email: 'chau.hr@etms.local',
    fullName: 'Nguyen Minh Chau',
    role: 'HR Executive',
    departmentId: 101,
    departmentCode: 'hr-admin',
    departmentName: 'Hanh chinh - Nhan su',
    requestAccessRole: 'requester',
    canCreateRequest: true,
    isActive: true,
    createdAt: new Date('2026-04-02T08:00:00'),
    password: 'Mock@123'
  },
  {
    id: 3,
    username: 'linh.mk',
    email: 'linh.mk@etms.local',
    fullName: 'Do Khanh Linh',
    role: 'Marketing Executive',
    departmentId: 104,
    departmentCode: 'marketing',
    departmentName: 'Marketing',
    requestAccessRole: 'requester',
    canCreateRequest: true,
    isActive: true,
    createdAt: new Date('2026-04-02T08:15:00'),
    password: 'Mock@123'
  },
  {
    id: 4,
    username: 'long.it',
    email: 'long.it@etms.local',
    fullName: 'Pham Duc Long',
    role: 'IT Support Lead',
    departmentId: 102,
    departmentCode: 'it',
    departmentName: 'IT noi bo',
    requestAccessRole: 'receiver',
    canCreateRequest: false,
    isActive: true,
    createdAt: new Date('2026-04-02T08:20:00'),
    password: 'Mock@123'
  },
  {
    id: 5,
    username: 'ha.finance',
    email: 'ha.finance@etms.local',
    fullName: 'Tran Thu Ha',
    role: 'Finance Lead',
    departmentId: 103,
    departmentCode: 'finance',
    departmentName: 'Ke toan - Tai chinh',
    requestAccessRole: 'receiver',
    canCreateRequest: false,
    isActive: true,
    createdAt: new Date('2026-04-02T08:25:00'),
    password: 'Mock@123'
  },
  {
    id: 6,
    username: 'phuc.mk',
    email: 'phuc.mk@etms.local',
    fullName: 'Le Hoang Phuc',
    role: 'Marketing Lead',
    departmentId: 104,
    departmentCode: 'marketing',
    departmentName: 'Marketing',
    requestAccessRole: 'receiver',
    canCreateRequest: false,
    isActive: true,
    createdAt: new Date('2026-04-02T08:30:00'),
    password: 'Mock@123'
  },
  {
    id: 7,
    username: 'dang.legal',
    email: 'dang.legal@etms.local',
    fullName: 'Nguyen Hai Dang',
    role: 'Legal Specialist',
    departmentId: 105,
    departmentCode: 'legal',
    departmentName: 'Phap che',
    requestAccessRole: 'receiver',
    canCreateRequest: false,
    isActive: true,
    createdAt: new Date('2026-04-02T08:35:00'),
    password: 'Mock@123'
  },
  {
    id: 8,
    username: 'kiet.finance',
    email: 'kiet.finance@etms.local',
    fullName: 'Pham Tuan Kiet',
    role: 'Finance Executive',
    departmentId: 103,
    departmentCode: 'finance',
    departmentName: 'Ke toan - Tai chinh',
    requestAccessRole: 'requester',
    canCreateRequest: true,
    isActive: true,
    createdAt: new Date('2026-04-02T08:40:00'),
    password: 'Mock@123'
  }
];
