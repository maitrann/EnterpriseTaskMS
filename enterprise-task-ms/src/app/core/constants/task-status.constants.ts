import { CustomSelectOption } from '../../shared/ui/custom-select/custom-select.component';

export type TaskStatusDefinition = {
  id: number;
  label: string;
  hint: string;
  bg: string;
  border: string;
};

export const TASK_STATUS_DEFINITIONS: TaskStatusDefinition[] = [
  {
    id: 1,
    label: 'Mới tạo',
    hint: 'Công việc vừa được khởi tạo',
    bg: 'linear-gradient(135deg, #f8fafc 0%, #eef2f7 100%)',
    border: 'rgba(148, 163, 184, 0.18)'
  },
  {
    id: 2,
    label: 'Chờ tiếp nhận',
    hint: 'Đang chờ người phụ trách tiếp nhận',
    bg: 'linear-gradient(135deg, #f8fafc 0%, #e2e8f0 100%)',
    border: 'rgba(100, 116, 139, 0.2)'
  },
  {
    id: 3,
    label: 'Đang xử lý',
    hint: 'Đang thực hiện và cập nhật tiến độ',
    bg: 'linear-gradient(135deg, #eff6ff 0%, #dbeafe 100%)',
    border: 'rgba(96, 165, 250, 0.22)'
  },
  {
    id: 4,
    label: 'Tạm dừng',
    hint: 'Tạm thời dừng xử lý chờ điều kiện tiếp theo',
    bg: 'linear-gradient(135deg, #fff7ed 0%, #ffedd5 100%)',
    border: 'rgba(251, 191, 36, 0.24)'
  },
  {
    id: 5,
    label: 'Chờ phản hồi',
    hint: 'Đang chờ phản hồi từ bên liên quan',
    bg: 'linear-gradient(135deg, #f5f3ff 0%, #ede9fe 100%)',
    border: 'rgba(167, 139, 250, 0.22)'
  },
  {
    id: 6,
    label: 'Chờ phê duyệt',
    hint: 'Đã xử lý xong và đang chờ phê duyệt',
    bg: 'linear-gradient(135deg, #fefce8 0%, #fef3c7 100%)',
    border: 'rgba(250, 204, 21, 0.24)'
  },
  {
    id: 7,
    label: 'Hoàn thành',
    hint: 'Đã hoàn tất nghiệp vụ chính',
    bg: 'linear-gradient(135deg, #ecfdf5 0%, #d1fae5 100%)',
    border: 'rgba(52, 211, 153, 0.22)'
  },
  {
    id: 8,
    label: 'Đóng',
    hint: 'Đã chốt và đóng công việc',
    bg: 'linear-gradient(135deg, #ecfeff 0%, #cffafe 100%)',
    border: 'rgba(34, 211, 238, 0.24)'
  },
  {
    id: 9,
    label: 'Hủy',
    hint: 'Công việc không còn tiếp tục thực hiện',
    bg: 'linear-gradient(135deg, #fef2f2 0%, #fee2e2 100%)',
    border: 'rgba(248, 113, 113, 0.24)'
  },
  {
    id: 10,
    label: 'Quá hạn',
    hint: 'Đã vượt hạn xử lý và cần ưu tiên',
    bg: 'linear-gradient(135deg, #fff1f2 0%, #ffe4e6 100%)',
    border: 'rgba(244, 63, 94, 0.24)'
  }
];

export const TASK_STATUS_OPTIONS: CustomSelectOption<number>[] = TASK_STATUS_DEFINITIONS.map((status) => ({
  value: status.id,
  label: status.label
}));

export const TASK_COMPLETED_STATUS_IDS = [7, 8];
export const TASK_TERMINAL_STATUS_IDS = [7, 8, 9];

export function getTaskStatusLabel(statusId?: number) {
  return TASK_STATUS_DEFINITIONS.find((status) => status.id === statusId)?.label ?? 'Chưa xác định';
}
