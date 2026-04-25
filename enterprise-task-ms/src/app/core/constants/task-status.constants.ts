import { CustomSelectOption } from '../../shared/ui/custom-select/custom-select.component';

export type TaskStatusDefinition = {
  id: number;
  label: string;
  hint: string;
  bg: string;
  border: string;
};

export const TASK_STATUS_IDS = {
  MOI_TAO: 1,
  CHO_TIEP_NHAN: 2,
  DANG_XU_LY: 3,
  TAM_DUNG: 4,
  CHO_PHAN_HOI: 5,
  CHO_PHE_DUYET: 6,
  HOAN_THANH: 7,
  DONG: 8,
  HUY: 9,
  QUA_HAN: 10
} as const;

export const TASK_STATUS_DEFINITIONS: TaskStatusDefinition[] = [
  {
    id: TASK_STATUS_IDS.MOI_TAO,
    label: 'Mới tạo',
    hint: 'Công việc vừa được khởi tạo',
    bg: 'linear-gradient(135deg, #f8fafc 0%, #eef2f7 100%)',
    border: 'rgba(148, 163, 184, 0.18)'
  },
  {
    id: TASK_STATUS_IDS.CHO_TIEP_NHAN,
    label: 'Chờ tiếp nhận',
    hint: 'Đang chờ người phụ trách tiếp nhận',
    bg: 'linear-gradient(135deg, #f8fafc 0%, #e2e8f0 100%)',
    border: 'rgba(100, 116, 139, 0.2)'
  },
  {
    id: TASK_STATUS_IDS.DANG_XU_LY,
    label: 'Đang xử lý',
    hint: 'Đang thực hiện và cập nhật tiến độ',
    bg: 'linear-gradient(135deg, #eff6ff 0%, #dbeafe 100%)',
    border: 'rgba(96, 165, 250, 0.22)'
  },
  {
    id: TASK_STATUS_IDS.TAM_DUNG,
    label: 'Tạm dừng',
    hint: 'Tạm thời dừng xử lý chờ điều kiện tiếp theo',
    bg: 'linear-gradient(135deg, #fff7ed 0%, #ffedd5 100%)',
    border: 'rgba(251, 191, 36, 0.24)'
  },
  {
    id: TASK_STATUS_IDS.CHO_PHAN_HOI,
    label: 'Chờ phản hồi',
    hint: 'Đang chờ phản hồi từ bên liên quan',
    bg: 'linear-gradient(135deg, #f5f3ff 0%, #ede9fe 100%)',
    border: 'rgba(167, 139, 250, 0.22)'
  },
  {
    id: TASK_STATUS_IDS.CHO_PHE_DUYET,
    label: 'Chờ phê duyệt',
    hint: 'Đã xử lý xong và đang chờ phê duyệt',
    bg: 'linear-gradient(135deg, #fefce8 0%, #fef3c7 100%)',
    border: 'rgba(250, 204, 21, 0.24)'
  },
  {
    id: TASK_STATUS_IDS.HOAN_THANH,
    label: 'Hoàn thành',
    hint: 'Đã hoàn tất nghiệp vụ chính',
    bg: 'linear-gradient(135deg, #ecfdf5 0%, #d1fae5 100%)',
    border: 'rgba(52, 211, 153, 0.22)'
  },
  {
    id: TASK_STATUS_IDS.DONG,
    label: 'Đóng',
    hint: 'Đã chốt và đóng công việc',
    bg: 'linear-gradient(135deg, #ecfeff 0%, #cffafe 100%)',
    border: 'rgba(34, 211, 238, 0.24)'
  },
  {
    id: TASK_STATUS_IDS.HUY,
    label: 'Hủy',
    hint: 'Công việc không còn tiếp tục thực hiện',
    bg: 'linear-gradient(135deg, #fef2f2 0%, #fee2e2 100%)',
    border: 'rgba(248, 113, 113, 0.24)'
  },
  {
    id: TASK_STATUS_IDS.QUA_HAN,
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

export const TASK_COMPLETED_STATUS_IDS: number[] = [TASK_STATUS_IDS.HOAN_THANH, TASK_STATUS_IDS.DONG];
export const TASK_TERMINAL_STATUS_IDS: number[] = [
  TASK_STATUS_IDS.HOAN_THANH,
  TASK_STATUS_IDS.DONG,
  TASK_STATUS_IDS.HUY
];

// Spec 8.4.5 defines the core path.
// The branches for Tạm dừng / Chờ phản hồi / Chờ phê duyệt / Quá hạn are inferred so the UI stays operable.
export const TASK_STATUS_TRANSITIONS: Record<number, number[]> = {
  [TASK_STATUS_IDS.MOI_TAO]: [TASK_STATUS_IDS.CHO_TIEP_NHAN, TASK_STATUS_IDS.HUY],
  [TASK_STATUS_IDS.CHO_TIEP_NHAN]: [TASK_STATUS_IDS.DANG_XU_LY, TASK_STATUS_IDS.HUY],
  [TASK_STATUS_IDS.DANG_XU_LY]: [
    TASK_STATUS_IDS.CHO_PHAN_HOI,
    TASK_STATUS_IDS.TAM_DUNG,
    TASK_STATUS_IDS.CHO_PHE_DUYET,
    TASK_STATUS_IDS.HOAN_THANH,
    TASK_STATUS_IDS.HUY
  ],
  [TASK_STATUS_IDS.TAM_DUNG]: [TASK_STATUS_IDS.DANG_XU_LY, TASK_STATUS_IDS.HUY],
  [TASK_STATUS_IDS.CHO_PHAN_HOI]: [TASK_STATUS_IDS.DANG_XU_LY, TASK_STATUS_IDS.HUY],
  [TASK_STATUS_IDS.CHO_PHE_DUYET]: [TASK_STATUS_IDS.HOAN_THANH, TASK_STATUS_IDS.HUY],
  [TASK_STATUS_IDS.HOAN_THANH]: [TASK_STATUS_IDS.DONG, TASK_STATUS_IDS.HUY],
  [TASK_STATUS_IDS.DONG]: [],
  [TASK_STATUS_IDS.HUY]: [],
  [TASK_STATUS_IDS.QUA_HAN]: [
    TASK_STATUS_IDS.DANG_XU_LY,
    TASK_STATUS_IDS.TAM_DUNG,
    TASK_STATUS_IDS.CHO_PHAN_HOI,
    TASK_STATUS_IDS.CHO_PHE_DUYET,
    TASK_STATUS_IDS.HOAN_THANH,
    TASK_STATUS_IDS.HUY
  ]
};

export function getTaskStatusLabel(statusId?: number) {
  return TASK_STATUS_DEFINITIONS.find((status) => status.id === statusId)?.label ?? 'Chưa xác định';
}

export function getTaskStatusDefinition(statusId?: number) {
  return TASK_STATUS_DEFINITIONS.find((status) => status.id === statusId) ?? null;
}

export function getAllowedNextStatusIds(statusId?: number, allowSpecialReopen = false) {
  if (!statusId) {
    return [];
  }

  if (allowSpecialReopen && statusId === TASK_STATUS_IDS.DONG) {
    return [TASK_STATUS_IDS.DANG_XU_LY];
  }

  return TASK_STATUS_TRANSITIONS[statusId] ?? [];
}

export function canTransitionTaskStatus(
  fromStatusId: number | undefined,
  toStatusId: number,
  allowSpecialReopen = false
) {
  if (!fromStatusId || fromStatusId === toStatusId) {
    return false;
  }

  return getAllowedNextStatusIds(fromStatusId, allowSpecialReopen).includes(toStatusId);
}

export function getAllowedStatusOptions(statusId?: number, allowSpecialReopen = false) {
  const allowedIds = getAllowedNextStatusIds(statusId, allowSpecialReopen);
  return TASK_STATUS_OPTIONS.filter((status) => allowedIds.includes(status.value));
}
