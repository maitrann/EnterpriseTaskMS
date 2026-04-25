import { Inject, Injectable, computed, signal } from '@angular/core';

import { AUTH_DATA_SOURCE, AuthDataSource } from '../data-sources/auth.datasource';
import {
  INTER_DEPARTMENT_REQUEST_DATA_SOURCE,
  InterDepartmentRequestDataSource
} from '../data-sources/inter-department-request.datasource';
import { MockAuthUser } from '../mock-data/auth.mock';
import {
  CreateInterDepartmentRequest,
  InterDepartmentRequest,
  RequestDepartmentRef,
  RequestMessage,
  RequestOwnerRef,
  RequestSlaPolicy,
  RequestStatus,
  RequestType
} from '../models/inter-department-request.model';

type AddRequestMessagePayload = {
  requestId: string;
  actor: MockAuthUser;
  body: string;
};

type RequestActionResult = {
  success: boolean;
  message?: string;
};

@Injectable({ providedIn: 'root' })
export class InterDepartmentRequestService {
  readonly departmentOptions: RequestDepartmentRef[];
  readonly ownerOptions: RequestOwnerRef[];
  readonly slaPolicies: RequestSlaPolicy[];
  readonly mockUsers: MockAuthUser[];
  readonly requests = signal<InterDepartmentRequest[]>([]);

  readonly requestTypeOptions = computed(() =>
    this.slaPolicies.map((policy) => ({
      value: policy.key,
      label: policy.label,
      description: `${policy.targetHours}h SLA`,
      groupLabel: 'Loại yêu cầu'
    }))
  );

  readonly statusOptions: Array<{ value: RequestStatus; label: string }> = [
    { value: 'new', label: 'Mới gửi' },
    { value: 'received', label: 'Đã tiếp nhận' },
    { value: 'processing', label: 'Đang xử lý' },
    { value: 'waiting-requester', label: 'Chờ bên gửi phản hồi' },
    { value: 'waiting-target', label: 'Chờ bộ phận xử lý cập nhật' },
    { value: 'done', label: 'Đã hoàn thành' },
    { value: 'closed', label: 'Đã đóng' },
    { value: 'rejected', label: 'Từ chối' }
  ];

  constructor(
    @Inject(AUTH_DATA_SOURCE)
    private readonly authDataSource: AuthDataSource,
    @Inject(INTER_DEPARTMENT_REQUEST_DATA_SOURCE)
    private readonly requestDataSource: InterDepartmentRequestDataSource
  ) {
    this.departmentOptions = this.requestDataSource.getDepartmentOptions();
    this.ownerOptions = this.requestDataSource.getOwnerOptions();
    this.slaPolicies = this.requestDataSource.getSlaPolicies();
    this.mockUsers = this.authDataSource.getMockUsers();
    this.requests.set(this.requestDataSource.getRequests());
  }

  createRequest(payload: CreateInterDepartmentRequest): RequestActionResult {
    if (!payload.targetDepartmentId.trim() || !payload.targetDepartment.trim()) {
      return {
        success: false,
        message: 'Cần chọn bộ phận tiếp nhận trước khi lưu phiếu yêu cầu.'
      };
    }

    const nextSequence = this.requests().length + 1;
    const now = new Date();
    const policy = this.getSlaPolicy(payload.type);
    const dueAt = this.getDueAtFromInput(payload.dueDate, policy.targetHours, now);

    const newRequest: InterDepartmentRequest = {
      id: String(nextSequence),
      code: `IR-${String(nextSequence).padStart(3, '0')}`,
      type: payload.type,
      title: payload.title.trim(),
      description: payload.description.trim(),
      requesterDepartment: payload.requesterDepartment.trim(),
      requesterDepartmentId: payload.requesterDepartmentId.trim(),
      requesterName: payload.requesterName.trim(),
      requesterUserId: payload.requesterUserId,
      targetDepartment: payload.targetDepartment.trim(),
      targetDepartmentId: payload.targetDepartmentId.trim(),
      owner: null,
      ownerId: null,
      priority: payload.priority,
      status: 'new',
      createdAt: now.toISOString(),
      updatedAt: now.toISOString(),
      receivedAt: null,
      closedAt: null,
      dueDate: this.formatDisplayDate(dueAt),
      sla: this.buildSlaSnapshot(policy, now, dueAt),
      formValues: this.normalizeFormValues(payload.formValues),
      latestMessage: payload.note.trim() || payload.description.trim(),
      note: payload.note.trim() || 'Phiếu yêu cầu mới được tạo từ workflow nội bộ.',
      messages: [
        {
          id: `msg-${Date.now()}`,
          authorName: payload.requesterName.trim(),
          authorRole: 'requester',
          authorDepartment: payload.requesterDepartment.trim(),
          createdAt: this.formatMessageTime(now),
          body: payload.note.trim() || payload.description.trim()
        }
      ]
    };

    this.requests.update((requests) => [newRequest, ...requests]);
    return { success: true };
  }

  acknowledgeRequest(requestId: string, actor: MockAuthUser): RequestActionResult {
    const request = this.requests().find((item) => item.id === requestId);

    if (!request) {
      return { success: false, message: 'Không tìm thấy phiếu yêu cầu.' };
    }

    if (!this.hasCoordinatorAccess(actor) && actor.departmentCode !== request.targetDepartmentId) {
      return { success: false, message: 'Chỉ bộ phận tiếp nhận mới được tiếp nhận phiếu này.' };
    }

    if (request.status !== 'new') {
      return { success: false, message: 'Phiếu đã được tiếp nhận hoặc đang xử lý.' };
    }

    const now = new Date();
    this.requests.update((requests) =>
      requests.map((item) =>
        item.id === requestId
          ? {
              ...item,
              status: 'received',
              receivedAt: now.toISOString(),
              updatedAt: now.toISOString(),
              latestMessage: `${actor.departmentName} đã tiếp nhận phiếu, đang chờ phân công xử lý.`,
              messages: [
                ...item.messages,
                {
                  id: `msg-${Date.now()}`,
                  authorName: actor.fullName ?? actor.username,
                  authorRole: 'processor',
                  authorDepartment: actor.departmentName ?? '',
                  createdAt: this.formatMessageTime(now),
                  body: 'Bộ phận tiếp nhận đã nhận phiếu và sẽ phân công người xử lý.'
                }
              ],
              sla: this.recalculateSla(item.sla, now)
            }
          : item
      )
    );

    return { success: true };
  }

  assignOwner(requestId: string, ownerId: string, actor: MockAuthUser): RequestActionResult {
    const request = this.requests().find((item) => item.id === requestId);
    const owner = this.ownerOptions.find((item) => item.id === ownerId);

    if (!request || !owner) {
      return { success: false, message: 'Không tìm thấy phiếu hoặc người xử lý.' };
    }

    if (!this.hasCoordinatorAccess(actor) && actor.departmentCode !== request.targetDepartmentId) {
      return { success: false, message: 'Chỉ bộ phận tiếp nhận mới được phân công người xử lý.' };
    }

    if (owner.departmentId !== request.targetDepartmentId) {
      return { success: false, message: 'Người xử lý phải thuộc cùng bộ phận tiếp nhận.' };
    }

    if (!['received', 'processing', 'waiting-target'].includes(request.status)) {
      return { success: false, message: 'Chỉ phân công sau khi phiếu đã được tiếp nhận.' };
    }

    const now = new Date();
    this.requests.update((requests) =>
      requests.map((item) =>
        item.id === requestId
          ? {
              ...item,
              owner: owner.name,
              ownerId: owner.id,
              status: 'processing',
              updatedAt: now.toISOString(),
              latestMessage: `${owner.name} đã được phân công xử lý phiếu.`,
              messages: [
                ...item.messages,
                {
                  id: `msg-${Date.now()}`,
                  authorName: actor.fullName ?? actor.username,
                  authorRole: 'processor',
                  authorDepartment: actor.departmentName ?? '',
                  createdAt: this.formatMessageTime(now),
                  body: `Đã phân công ${owner.name} xử lý yêu cầu.`
                }
              ],
              sla: this.recalculateSla(item.sla, now)
            }
          : item
      )
    );

    return { success: true };
  }

  updateStatus(requestId: string, status: RequestStatus, actor: MockAuthUser): RequestActionResult {
    const request = this.requests().find((item) => item.id === requestId);

    if (!request) {
      return { success: false, message: 'Không tìm thấy phiếu yêu cầu.' };
    }

    if (!this.hasCoordinatorAccess(actor) && actor.departmentCode !== request.targetDepartmentId) {
      return { success: false, message: 'Chỉ bộ phận xử lý mới được cập nhật trạng thái.' };
    }

    if (!request.ownerId && status !== 'received') {
      return {
        success: false,
        message: 'Cần tiếp nhận và phân công người xử lý trước khi cập nhật tiến độ.'
      };
    }

    if (status === 'closed') {
      return { success: false, message: 'Trạng thái đóng phiếu chỉ do bên yêu cầu xác nhận.' };
    }

    const now = new Date();
    this.requests.update((requests) =>
      requests.map((item) =>
        item.id === requestId
          ? {
              ...item,
              status,
              updatedAt: now.toISOString(),
              latestMessage: `Trạng thái được cập nhật sang ${this.getStatusLabel(status).toLowerCase()}.`,
              sla: this.recalculateSla(item.sla, now)
            }
          : item
      )
    );

    return { success: true };
  }

  closeRequest(requestId: string, actor: MockAuthUser): RequestActionResult {
    const request = this.requests().find((item) => item.id === requestId);

    if (!request) {
      return { success: false, message: 'Không tìm thấy phiếu yêu cầu.' };
    }

    if (!this.hasCoordinatorAccess(actor) && actor.id !== request.requesterUserId) {
      return { success: false, message: 'Chỉ bên yêu cầu tạo phiếu mới được xác nhận đóng phiếu.' };
    }

    if (request.status !== 'done') {
      return { success: false, message: 'Chỉ đóng phiếu sau khi bộ phận xử lý đã hoàn thành.' };
    }

    const now = new Date();
    this.requests.update((requests) =>
      requests.map((item) =>
        item.id === requestId
          ? {
              ...item,
              status: 'closed',
              updatedAt: now.toISOString(),
              closedAt: now.toISOString(),
              latestMessage: `${actor.fullName ?? actor.username} đã xác nhận kết quả và đóng phiếu.`,
              messages: [
                ...item.messages,
                {
                  id: `msg-${Date.now()}`,
                  authorName: actor.fullName ?? actor.username,
                  authorRole: 'requester',
                  authorDepartment: actor.departmentName ?? '',
                  createdAt: this.formatMessageTime(now),
                  body: 'Bên yêu cầu đã xác nhận kết quả và đóng phiếu.'
                }
              ],
              sla: this.recalculateSla(item.sla, now)
            }
          : item
      )
    );

    return { success: true };
  }

  addMessage(payload: AddRequestMessagePayload): RequestActionResult {
    const now = new Date();
    const request = this.requests().find((item) => item.id === payload.requestId);

    if (!request) {
      return { success: false, message: 'Không tìm thấy phiếu yêu cầu.' };
    }

    const isRequester = payload.actor.id === request.requesterUserId;
    const isTargetDepartment = payload.actor.departmentCode === request.targetDepartmentId;
    const isCoordinator = this.hasCoordinatorAccess(payload.actor);

    if (!isRequester && !isTargetDepartment && !isCoordinator) {
      return { success: false, message: 'Tài khoản hiện tại không thuộc luồng xử lý của phiếu.' };
    }

    this.requests.update((requests) =>
      requests.map((item) => {
        if (item.id !== payload.requestId) {
          return item;
        }

        const nextMessage: RequestMessage = {
          id: `msg-${Date.now()}`,
          authorName: payload.actor.fullName ?? payload.actor.username,
          authorRole: isRequester ? 'requester' : 'processor',
          authorDepartment: payload.actor.departmentName ?? '',
          createdAt: this.formatMessageTime(now),
          body: payload.body.trim()
        };

        return {
          ...item,
          updatedAt: now.toISOString(),
          latestMessage: payload.body.trim(),
          status: this.getConversationStatus(item.status, isRequester),
          messages: [...item.messages, nextMessage],
          sla: this.recalculateSla(item.sla, now)
        };
      })
    );

    return { success: true };
  }

  getVisibleRequests(actor: MockAuthUser | null) {
    if (!actor) {
      return this.requests();
    }

    if (this.hasCoordinatorAccess(actor)) {
      return this.requests();
    }

    return this.requests().filter((request) => {
      if (actor.requestAccessRole === 'requester') {
        return request.requesterUserId === actor.id || request.requesterDepartmentId === actor.departmentCode;
      }

      return request.targetDepartmentId === actor.departmentCode;
    });
  }

  getOwnersByDepartment(departmentId: string) {
    return this.ownerOptions.filter((owner) => owner.departmentId === departmentId);
  }

  getStatusLabel(status: RequestStatus) {
    return this.statusOptions.find((option) => option.value === status)?.label ?? status;
  }

  getTypeLabel(type: RequestType) {
    return this.slaPolicies.find((policy) => policy.key === type)?.label ?? type;
  }

  readonly summaryFactory = (items: InterDepartmentRequest[]) => [
    { label: 'Tổng yêu cầu', value: items.length, helper: 'Số phiếu sau khi áp dụng phạm vi xem hiện tại' },
    {
      label: 'Mới tiếp nhận',
      value: items.filter((request) => request.status === 'new').length,
      helper: 'Danh sách chờ bộ phận tiếp nhận xử lý'
    },
    {
      label: 'Đang mở',
      value: items.filter((request) => !['closed', 'rejected'].includes(request.status)).length,
      helper: 'Bao gồm tiếp nhận, xử lý, chờ phản hồi và đã hoàn thành'
    },
    {
      label: 'Quá SLA',
      value: items.filter((request) => request.sla.breached).length,
      helper: 'Cần xử lý sát do đã vượt ngưỡng cam kết'
    }
  ];

  readonly statusBucketFactory = (items: InterDepartmentRequest[]) => [
    { label: 'Mới gửi', count: items.filter((request) => request.status === 'new').length, tone: 'blue' },
    {
      label: 'Đã tiếp nhận',
      count: items.filter((request) => request.status === 'received').length,
      tone: 'slate'
    },
    {
      label: 'Đang xử lý',
      count: items.filter((request) => ['processing', 'waiting-requester', 'waiting-target'].includes(request.status)).length,
      tone: 'amber'
    },
    {
      label: 'Hoàn tất / Đóng',
      count: items.filter((request) => ['done', 'closed'].includes(request.status)).length,
      tone: 'emerald'
    }
  ];

  readonly departmentSummaryFactory = (items: InterDepartmentRequest[]) =>
    this.departmentOptions
      .map((department) => {
        const matching = items.filter((request) => request.targetDepartmentId === department.id);

        return {
          id: department.id,
          label: department.name,
          total: matching.length,
          open: matching.filter((request) => !['closed', 'rejected'].includes(request.status)).length,
          breached: matching.filter((request) => request.sla.breached).length
        };
      })
      .filter((department) => department.total > 0)
      .sort((left, right) => right.total - left.total);

  private normalizeFormValues(values: Record<string, string>) {
    return Object.entries(values).reduce<Record<string, string>>((result, [key, value]) => {
      const normalizedKey = key.trim();
      const normalizedValue = value.trim();

      if (normalizedKey && normalizedValue) {
        result[normalizedKey] = normalizedValue;
      }

      return result;
    }, {});
  }

  private getConversationStatus(status: RequestStatus, isRequester: boolean): RequestStatus {
    if (['new', 'received', 'done', 'closed', 'rejected'].includes(status)) {
      return status;
    }

    return isRequester ? 'waiting-target' : 'waiting-requester';
  }

  private hasCoordinatorAccess(actor: MockAuthUser) {
    const role = actor.role?.toLowerCase() ?? '';
    return (
      actor.requestAccessRole === 'coordinator' ||
      role.includes('admin') ||
      role.includes('lanh dao') ||
      role.includes('lãnh đạo')
    );
  }

  private getSlaPolicy(type: RequestType) {
    return (
      this.slaPolicies.find((policy) => policy.key === type) ?? {
        key: type,
        label: type,
        targetHours: 24,
        warnHours: 4
      }
    );
  }

  private buildSlaSnapshot(policy: RequestSlaPolicy, startedAt: Date, dueAt: Date) {
    const remainingHours = this.getRemainingHours(dueAt, startedAt);

    return {
      policyKey: policy.key,
      policyLabel: policy.label,
      targetHours: policy.targetHours,
      warnHours: policy.warnHours,
      startedAt: startedAt.toISOString(),
      dueAt: dueAt.toISOString(),
      remainingHours,
      breached: remainingHours < 0
    };
  }

  private recalculateSla(
    sla: InterDepartmentRequest['sla'],
    referenceDate: Date
  ): InterDepartmentRequest['sla'] {
    const remainingHours = this.getRemainingHours(new Date(sla.dueAt), referenceDate);

    return {
      ...sla,
      remainingHours,
      breached: remainingHours < 0
    };
  }

  private getRemainingHours(dueAt: Date, referenceDate: Date) {
    const diffHours = (dueAt.getTime() - referenceDate.getTime()) / (1000 * 60 * 60);
    return Math.round(diffHours);
  }

  private getDueAtFromInput(value: string, fallbackHours: number, referenceDate: Date) {
    if (!value) {
      return new Date(referenceDate.getTime() + fallbackHours * 60 * 60 * 1000);
    }

    return new Date(`${value}T17:00:00`);
  }

  private formatDisplayDate(value: Date | string) {
    const date = typeof value === 'string' ? new Date(value) : value;
    const day = String(date.getDate()).padStart(2, '0');
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const year = date.getFullYear();

    return `${day}/${month}/${year}`;
  }

  private formatMessageTime(value: Date) {
    const day = String(value.getDate()).padStart(2, '0');
    const month = String(value.getMonth() + 1).padStart(2, '0');
    const year = value.getFullYear();
    const hour = String(value.getHours()).padStart(2, '0');
    const minute = String(value.getMinutes()).padStart(2, '0');

    return `${day}/${month}/${year} ${hour}:${minute}`;
  }
}
