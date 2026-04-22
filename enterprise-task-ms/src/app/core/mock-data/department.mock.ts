import { DepartmentCard } from '../models/department-card.model';

export const DEPARTMENT_CARD_MOCK: DepartmentCard[] = [
  {
    name: 'Hanh chinh - Nhan su',
    description: 'Xu ly de xuat nhan su, hanh chinh van phong va cac workflow noi bo.',
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
    description: 'Ho tro he thong, xu ly su co va trien khai nhu cau cong nghe cho van phong.',
    members: 7,
    activeTasks: 9,
    completedTasks: 24,
    lead: 'Pham Duc Long',
    sla: '95%',
    tone: 'slate'
  }
];
