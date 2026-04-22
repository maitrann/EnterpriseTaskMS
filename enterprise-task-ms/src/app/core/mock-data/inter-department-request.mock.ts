import { InterDepartmentRequest } from '../models/inter-department-request.model';

export const INTER_DEPARTMENT_REQUEST_MOCK: InterDepartmentRequest[] = [
  {
    id: 'IR-001',
    title: 'De nghi cap tai khoan email cho nhan su moi',
    requesterDepartment: 'Hanh chinh - Nhan su',
    targetDepartment: 'IT noi bo',
    owner: 'Pham Duc Long',
    priority: 'High',
    status: 'processing',
    sla: 'Con 4h',
    dueDate: '22/04/2026',
    note: 'Can cap truoc 17:00 de hoan tat onboarding.'
  },
  {
    id: 'IR-002',
    title: 'Doi soat chi phi campaign thang 04',
    requesterDepartment: 'Marketing',
    targetDepartment: 'Ke toan - Tai chinh',
    owner: 'Tran Thu Ha',
    priority: 'Medium',
    status: 'waiting',
    sla: 'Cho xac nhan',
    dueDate: '23/04/2026',
    note: 'Dang cho bo sung bang ke chi tiet tu team marketing.'
  },
  {
    id: 'IR-003',
    title: 'Cap phat laptop cho nhan vien thu viec',
    requesterDepartment: 'Hanh chinh - Nhan su',
    targetDepartment: 'IT noi bo',
    owner: 'Pham Duc Long',
    priority: 'Critical',
    status: 'new',
    sla: 'Moi tiep nhan',
    dueDate: '22/04/2026',
    note: 'Yeu cau phuc vu dot nhan su vao thu 2 dau tuan.'
  },
  {
    id: 'IR-004',
    title: 'Xac nhan ngan sach workshop noi bo',
    requesterDepartment: 'Hanh chinh - Nhan su',
    targetDepartment: 'Ke toan - Tai chinh',
    owner: 'Tran Thu Ha',
    priority: 'Low',
    status: 'done',
    sla: 'Dat SLA',
    dueDate: '21/04/2026',
    note: 'Da duoc phe duyet va chuyen lai ben yeu cau.'
  }
];

export const INTER_DEPARTMENT_OPTION_MOCK = {
  departments: ['Hanh chinh - Nhan su', 'IT noi bo', 'Ke toan - Tai chinh', 'Marketing'],
  owners: ['Pham Duc Long', 'Tran Thu Ha', 'Nguyen Minh An', 'Le Hoang Phuc']
};
