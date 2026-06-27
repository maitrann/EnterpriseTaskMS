import { DepartmentCard } from '../models/department-card.model';

export const DEPARTMENT_CARD_MOCK: DepartmentCard[] = [
  {
    name: 'Hành chính - Nhân sự',
    description: 'Xử lý đề xuất nhân sự, hành chính văn phòng và các workflow nội bộ.',
    members: 10,
    activeTasks: 18,
    completedTasks: 32,
    lead: 'Nguyễn Minh An',
    sla: '92%',
    tone: 'blue'
  },
  {
    name: 'Kế toán - Tài chính',
    description: 'Kiểm soát thanh toán, đối soát chứng từ và tổng hợp chi phí vận hành.',
    members: 8,
    activeTasks: 12,
    completedTasks: 26,
    lead: 'Trần Thu Hà',
    sla: '88%',
    tone: 'amber'
  },
  {
    name: 'Marketing',
    description: 'Vận hành campaign, sản xuất nội dung và phối hợp truyền thông nội bộ.',
    members: 14,
    activeTasks: 21,
    completedTasks: 19,
    lead: 'Lê Hoàng Phúc',
    sla: '85%',
    tone: 'emerald'
  },
  {
    name: 'IT nội bộ',
    description: 'Hỗ trợ hệ thống, xử lý sự cố và triển khai nhu cầu công nghệ cho văn phòng.',
    members: 7,
    activeTasks: 9,
    completedTasks: 24,
    lead: 'Phạm Đức Long',
    sla: '95%',
    tone: 'slate'
  }
];
