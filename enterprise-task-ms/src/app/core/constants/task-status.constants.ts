import { CustomSelectOption } from '../../shared/ui/custom-select/custom-select.component';

export type TaskStatusDefinition = {
  id: number;
  label: string;
  hint: string;
  bg: string;
  border: string;
};

// Keep these ids aligned with the PostgreSQL seed order in public.task_statuses.
export const TASK_STATUS_IDS = {
  MOI_TAO: 1,
  CHO_TIEP_NHAN: 2,
  DANG_XU_LY: 3,
  CHO_PHE_DUYET: 4,
  HOAN_THANH: 5,
  DONG: 6,
  TAM_DUNG: 7,
  HUY: 8,
  QUA_HAN: 9,
  // Backward-compatible alias for older mock records. The database code is `on_hold`.
  CHO_PHAN_HOI: 7
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
    label: 'Đã phân công',
    hint: 'Đang chờ người phụ trách tiếp nhận',
    bg: 'linear-gradient(135deg, #f8fafc 0%, #e2e8f0 100%)',
    border: 'rgba(100, 116, 139, 0.2)'
  },
  {
    id: TASK_STATUS_IDS.DANG_XU_LY,
    label: 'Đang xử lý',
    hint: 'Đang thực hiện và cập nhật tiến độ',
    bg: 'linear-gradient(135deg, #fff7ed 0%, #ffedd5 100%)',
    border: 'rgba(251, 191, 36, 0.24)'
  },
  {
    id: TASK_STATUS_IDS.CHO_PHE_DUYET,
    label: 'Chờ xác nhận',
    hint: 'Đang chờ người giao việc xác nhận kết quả',
    bg: 'linear-gradient(135deg, #f5f3ff 0%, #ede9fe 100%)',
    border: 'rgba(167, 139, 250, 0.22)'
  },
  {
    id: TASK_STATUS_IDS.HOAN_THANH,
    label: 'Đã hoàn thành',
    hint: 'Đã hoàn tất nghiệp vụ chính',
    bg: 'linear-gradient(135deg, #ecfdf5 0%, #d1fae5 100%)',
    border: 'rgba(52, 211, 153, 0.22)'
  },
  {
    id: TASK_STATUS_IDS.DONG,
    label: 'Đã đóng',
    hint: 'Đã xác nhận và đóng công việc',
    bg: 'linear-gradient(135deg, #ecfeff 0%, #cffafe 100%)',
    border: 'rgba(34, 211, 238, 0.24)'
  },
  {
    id: TASK_STATUS_IDS.TAM_DUNG,
    label: 'Tạm dừng',
    hint: 'Đang tạm dừng hoặc chờ điều phối lại',
    bg: 'linear-gradient(135deg, #f8fafc 0%, #e2e8f0 100%)',
    border: 'rgba(100, 116, 139, 0.2)'
  },
  {
    id: TASK_STATUS_IDS.HUY,
    label: 'Đã hủy',
    hint: 'Công việc không còn tiếp tục thực hiện',
    bg: 'linear-gradient(135deg, #fef2f2 0%, #fee2e2 100%)',
    border: 'rgba(248, 113, 113, 0.24)'
  },
  {
    id: TASK_STATUS_IDS.QUA_HAN,
    label: 'Quá hạn',
    hint: 'Công việc đã quá hạn xử lý',
    bg: 'linear-gradient(135deg, #fff1f2 0%, #ffe4e6 100%)',
    border: 'rgba(244, 63, 94, 0.24)'
  }
];

export const TASK_STATUS_OPTIONS: CustomSelectOption<number>[] = TASK_STATUS_DEFINITIONS.map((status) => ({
  value: status.id,
  label: status.label
}));

export const TASK_COMPLETED_STATUS_IDS: number[] = [TASK_STATUS_IDS.HOAN_THANH, TASK_STATUS_IDS.DONG];
export const TASK_TERMINAL_STATUS_IDS: number[] = [TASK_STATUS_IDS.DONG, TASK_STATUS_IDS.HUY];

export const TASK_STATUS_TRANSITIONS: Record<number, number[]> = {
  [TASK_STATUS_IDS.MOI_TAO]: [TASK_STATUS_IDS.CHO_TIEP_NHAN, TASK_STATUS_IDS.HUY],
  [TASK_STATUS_IDS.CHO_TIEP_NHAN]: [
    TASK_STATUS_IDS.DANG_XU_LY,
    TASK_STATUS_IDS.TAM_DUNG,
    TASK_STATUS_IDS.HUY
  ],
  [TASK_STATUS_IDS.DANG_XU_LY]: [
    TASK_STATUS_IDS.CHO_PHE_DUYET,
    TASK_STATUS_IDS.HOAN_THANH,
    TASK_STATUS_IDS.TAM_DUNG,
    TASK_STATUS_IDS.HUY
  ],
  [TASK_STATUS_IDS.CHO_PHE_DUYET]: [
    TASK_STATUS_IDS.DANG_XU_LY,
    TASK_STATUS_IDS.HOAN_THANH,
    TASK_STATUS_IDS.HUY
  ],
  [TASK_STATUS_IDS.HOAN_THANH]: [TASK_STATUS_IDS.DONG, TASK_STATUS_IDS.HUY],
  [TASK_STATUS_IDS.DONG]: [],
  [TASK_STATUS_IDS.TAM_DUNG]: [
    TASK_STATUS_IDS.CHO_TIEP_NHAN,
    TASK_STATUS_IDS.DANG_XU_LY,
    TASK_STATUS_IDS.HUY
  ],
  [TASK_STATUS_IDS.HUY]: [],
  [TASK_STATUS_IDS.QUA_HAN]: [
    TASK_STATUS_IDS.DANG_XU_LY,
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
