import { DepartmentCard } from '../models/department-card.model';

export const DEPARTMENT_CARD_MOCK: DepartmentCard[] = [
  {
    name: 'Hành chính - Nhân sự',
    description: 'Xử lý đề xuất nhân sự, hành chính văn phòng và các workflow nội bộ.',
    members: 10,
    activeTasks: 18,
    completedTasks: 32,
    lead: 'Nguyen Minh An',
    sla: '92%',
    tone: 'blue'
  },
  {
    name: 'Ke toan - Tai chinh',
    description: 'Kiem soat thanh toan, doi soat chung tu va tong hop chi phi van hanh.',
    members: 8,
    activeTasks: 12,
    completedTasks: 26,
    lead: 'Tran Thu Ha',
    sla: '88%',
    tone: 'amber'
  },
  {
    name: 'Marketing',
    description: 'Van hanh campaign, san xuat noi dung va phoi hop truyen thong noi bo.',
    members: 14,
    activeTasks: 21,
    completedTasks: 19,
    lead: 'Le Hoang Phuc',
    sla: '85%',
    tone: 'emerald'
  },
  {
    name: 'IT noi bo',
    description: 'Hỗ trợ hệ thống, xử lý sự cố và triển khai nhu cầu công nghệ cho văn phòng.',
    members: 7,
    activeTasks: 9,
    completedTasks: 24,
    lead: 'Pham Duc Long',
    sla: '95%',
    tone: 'slate'
  }
];
