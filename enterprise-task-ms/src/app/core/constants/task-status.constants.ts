import { CustomSelectOption } from '../../shared/ui/custom-select/custom-select.component';

export type TaskStatusDefinition = {
  id: number;
  label: string;
  hint: string;
  bg: string;
  border: string;
};

// Keep these ids aligned with the initial PostgreSQL seed order in task_statuses.
export const TASK_STATUS_IDS = {
  MOI_TAO: 1,
  CHO_TIEP_NHAN: 2,
  DA_TIEP_NHAN: 3,
  DANG_XU_LY: 4,
  CHO_PHAN_HOI: 5,
  HOAN_THANH: 6,
  DONG: 7,
  HUY: 8,
  TU_CHOI: 9,
  TAM_DUNG: 5,
  CHO_PHE_DUYET: 6,
  QUA_HAN: 0
} as const;

export const TASK_STATUS_DEFINITIONS: TaskStatusDefinition[] = [
  {
    id: TASK_STATUS_IDS.MOI_TAO,
    label: 'Moi tao',
    hint: 'Cong viec vua duoc khoi tao',
    bg: 'linear-gradient(135deg, #f8fafc 0%, #eef2f7 100%)',
    border: 'rgba(148, 163, 184, 0.18)'
  },
  {
    id: TASK_STATUS_IDS.CHO_TIEP_NHAN,
    label: 'Da phan cong',
    hint: 'Dang cho nguoi phu trach tiep nhan',
    bg: 'linear-gradient(135deg, #f8fafc 0%, #e2e8f0 100%)',
    border: 'rgba(100, 116, 139, 0.2)'
  },
  {
    id: TASK_STATUS_IDS.DA_TIEP_NHAN,
    label: 'Da tiep nhan',
    hint: 'Nguoi phu trach da nhan viec',
    bg: 'linear-gradient(135deg, #eff6ff 0%, #dbeafe 100%)',
    border: 'rgba(96, 165, 250, 0.22)'
  },
  {
    id: TASK_STATUS_IDS.DANG_XU_LY,
    label: 'Dang xu ly',
    hint: 'Dang thuc hien va cap nhat tien do',
    bg: 'linear-gradient(135deg, #fff7ed 0%, #ffedd5 100%)',
    border: 'rgba(251, 191, 36, 0.24)'
  },
  {
    id: TASK_STATUS_IDS.CHO_PHAN_HOI,
    label: 'Dang cho',
    hint: 'Dang cho phan hoi hoac dieu kien tiep theo',
    bg: 'linear-gradient(135deg, #f5f3ff 0%, #ede9fe 100%)',
    border: 'rgba(167, 139, 250, 0.22)'
  },
  {
    id: TASK_STATUS_IDS.HOAN_THANH,
    label: 'Da hoan thanh',
    hint: 'Da hoan tat nghiep vu chinh',
    bg: 'linear-gradient(135deg, #ecfdf5 0%, #d1fae5 100%)',
    border: 'rgba(52, 211, 153, 0.22)'
  },
  {
    id: TASK_STATUS_IDS.DONG,
    label: 'Da xac nhan',
    hint: 'Da xac nhan va dong cong viec',
    bg: 'linear-gradient(135deg, #ecfeff 0%, #cffafe 100%)',
    border: 'rgba(34, 211, 238, 0.24)'
  },
  {
    id: TASK_STATUS_IDS.HUY,
    label: 'Da huy',
    hint: 'Cong viec khong con tiep tuc thuc hien',
    bg: 'linear-gradient(135deg, #fef2f2 0%, #fee2e2 100%)',
    border: 'rgba(248, 113, 113, 0.24)'
  },
  {
    id: TASK_STATUS_IDS.TU_CHOI,
    label: 'Tu choi',
    hint: 'Cong viec bi tu choi trong luong xu ly',
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
  TASK_STATUS_IDS.HUY,
  TASK_STATUS_IDS.TU_CHOI
];

export const TASK_STATUS_TRANSITIONS: Record<number, number[]> = {
  [TASK_STATUS_IDS.MOI_TAO]: [TASK_STATUS_IDS.CHO_TIEP_NHAN, TASK_STATUS_IDS.HUY],
  [TASK_STATUS_IDS.CHO_TIEP_NHAN]: [TASK_STATUS_IDS.DA_TIEP_NHAN, TASK_STATUS_IDS.TU_CHOI, TASK_STATUS_IDS.HUY],
  [TASK_STATUS_IDS.DA_TIEP_NHAN]: [TASK_STATUS_IDS.DANG_XU_LY, TASK_STATUS_IDS.HUY],
  [TASK_STATUS_IDS.DANG_XU_LY]: [
    TASK_STATUS_IDS.CHO_PHAN_HOI,
    TASK_STATUS_IDS.HOAN_THANH,
    TASK_STATUS_IDS.HUY
  ],
  [TASK_STATUS_IDS.CHO_PHAN_HOI]: [TASK_STATUS_IDS.DANG_XU_LY, TASK_STATUS_IDS.HUY],
  [TASK_STATUS_IDS.HOAN_THANH]: [TASK_STATUS_IDS.DONG, TASK_STATUS_IDS.HUY],
  [TASK_STATUS_IDS.DONG]: [],
  [TASK_STATUS_IDS.HUY]: [],
  [TASK_STATUS_IDS.TU_CHOI]: []
};

export function getTaskStatusLabel(statusId?: number) {
  return TASK_STATUS_DEFINITIONS.find((status) => status.id === statusId)?.label ?? 'Chua xac dinh';
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
