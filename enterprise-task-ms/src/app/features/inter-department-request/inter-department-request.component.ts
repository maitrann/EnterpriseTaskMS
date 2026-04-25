import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import {
  CreateInterDepartmentRequest,
  InterDepartmentRequest,
  RequestPriority,
  RequestStatus,
  RequestType
} from '../../core/models/inter-department-request.model';
import { MockAuthUser } from '../../core/mock-data/auth.mock';
import { AuthService } from '../../core/services/auth.service';
import { InterDepartmentRequestService } from '../../core/services/inter-department-request.service';
import {
  CustomSelectComponent,
  CustomSelectOption
} from '../../shared/ui/custom-select/custom-select.component';

type RequestFilters = {
  type: string;
  targetDepartmentId: string;
  status: string;
};

type RequestFieldTemplate = {
  key: string;
  label: string;
  placeholder: string;
};

const REQUEST_TYPE_TEMPLATES: Record<RequestType, RequestFieldTemplate[]> = {
  procurement: [
    { key: 'Hạng mục mua sắm', label: 'Hạng mục mua sắm', placeholder: 'Máy in, bàn ghế, vật tư...' },
    { key: 'Số lượng / ngân sách', label: 'Số lượng / ngân sách', placeholder: 'Nhập số lượng hoặc ngân sách' },
    { key: 'Lý do', label: 'Lý do', placeholder: 'Bối cảnh cần mua sắm' }
  ],
  asset: [
    { key: 'Loại thiết bị', label: 'Loại thiết bị', placeholder: 'Laptop, màn hình, thẻ ra vào...' },
    { key: 'Mục đích', label: 'Mục đích', placeholder: 'Onboarding, thay thế, cấp bổ sung...' },
    { key: 'Địa điểm giao', label: 'Địa điểm giao', placeholder: 'Tầng / khu vực bàn giao' }
  ],
  'it-support': [
    { key: 'Hệ thống / dịch vụ', label: 'Hệ thống / dịch vụ', placeholder: 'Email, VPN, phần mềm, wifi...' },
    { key: 'Mức độ ảnh hưởng', label: 'Mức độ ảnh hưởng', placeholder: '1 người dùng, 1 phòng ban...' },
    { key: 'Mô tả lỗi', label: 'Mô tả lỗi', placeholder: 'Mô tả ngắn gọn vấn đề gặp phải' }
  ],
  payment: [
    { key: 'Số tiền đề nghị', label: 'Số tiền đề nghị', placeholder: 'Nhập tổng giá trị' },
    { key: 'Hình thức', label: 'Hình thức', placeholder: 'Thanh toán, tạm ứng, hoàn ứng...' },
    { key: 'Mã tham chiếu', label: 'Mã tham chiếu', placeholder: 'Campaign, đề xuất, chứng từ...' }
  ],
  recruitment: [
    { key: 'Vị trí', label: 'Vị trí', placeholder: 'Tên vị trí cần tuyển' },
    { key: 'Số lượng', label: 'Số lượng', placeholder: 'Nhập số lượng cần tuyển' },
    { key: 'Thời điểm cần', label: 'Thời điểm cần', placeholder: 'Deadline onboard' }
  ],
  'communication-design': [
    { key: 'Định dạng', label: 'Định dạng', placeholder: 'Poster, social post, deck, video...' },
    { key: 'Key message', label: 'Key message', placeholder: 'Thông điệp chính cần truyền tải' },
    { key: 'Mốc phát hành', label: 'Mốc phát hành', placeholder: 'Ngày cần bàn giao sản phẩm' }
  ],
  legal: [
    { key: 'Loại hồ sơ', label: 'Loại hồ sơ', placeholder: 'Hợp đồng, phụ lục, văn bản...' },
    { key: 'Đối tác / đơn vị', label: 'Đối tác / đơn vị', placeholder: 'Nhập tên đối tác hoặc đơn vị' },
    { key: 'Mốc áp dụng', label: 'Mốc áp dụng', placeholder: 'Ngày cần có ý kiến pháp lý' }
  ]
};

@Component({
  standalone: true,
  selector: 'app-inter-department-request',
  imports: [CommonModule, FormsModule, CustomSelectComponent],
  templateUrl: './inter-department-request.component.html',
  styleUrl: './inter-department-request.component.scss'
})
export class InterDepartmentRequestComponent {
  private readonly requestService = inject(InterDepartmentRequestService);
  private readonly authService = inject(AuthService);

  readonly currentUser = computed(() => this.authService.user() as MockAuthUser | null);
  readonly departments = this.requestService.departmentOptions;
  readonly statusOptions = this.requestService.statusOptions;
  readonly requestTypeOptions = this.requestService.requestTypeOptions;

  readonly departmentSelectOptions = computed<CustomSelectOption[]>(() => [
    { value: '', label: 'Tất cả' },
    ...this.departments.map((department) => ({
      value: department.id,
      label: department.name
    }))
  ]);

  readonly requestTypeSelectOptions = computed<CustomSelectOption[]>(() => [
    { value: '', label: 'Tất cả' },
    ...this.requestTypeOptions()
  ]);

  readonly filterStatusOptions = computed<CustomSelectOption[]>(() => [
    { value: '', label: 'Tất cả' },
    ...this.statusOptions
  ]);

  readonly workflowStatusOptions = computed<CustomSelectOption[]>(() =>
    this.statusOptions
      .filter((status) => status.value !== 'closed')
      .map((status) => ({ ...status, groupLabel: 'Trạng thái' }))
  );

  readonly priorityOptions: CustomSelectOption<RequestPriority>[] = [
    { value: 'Low', label: 'Thấp' },
    { value: 'Medium', label: 'Trung bình' },
    { value: 'High', label: 'Cao' },
    { value: 'Critical', label: 'Khẩn cấp' }
  ];

  readonly filters = signal<RequestFilters>({
    type: '',
    targetDepartmentId: '',
    status: ''
  });

  readonly feedbackMessage = signal('');
  readonly selectedRequest = signal<InterDepartmentRequest | null>(null);
  readonly editingRequest = signal<InterDepartmentRequest | null>(null);
  readonly assigningRequest = signal<InterDepartmentRequest | null>(null);
  readonly editingStatus = signal<RequestStatus>('processing');
  readonly messageDraft = signal('');
  readonly selectedOwnerId = signal('');

  readonly isCreateModalOpen = signal(false);
  readonly createForm = signal<CreateInterDepartmentRequest>(this.getInitialForm());

  readonly availableRequests = computed(() => this.requestService.getVisibleRequests(this.currentUser()));
  readonly filteredRequests = computed(() => {
    const activeFilters = this.filters();

    return this.availableRequests().filter((request) => {
      const matchesType = !activeFilters.type || request.type === activeFilters.type;
      const matchesTarget =
        !activeFilters.targetDepartmentId || request.targetDepartmentId === activeFilters.targetDepartmentId;
      const matchesStatus = !activeFilters.status || request.status === activeFilters.status;

      return matchesType && matchesTarget && matchesStatus;
    });
  });

  readonly summaryCards = computed(() => this.requestService.summaryFactory(this.filteredRequests()));
  readonly statusBuckets = computed(() => this.requestService.statusBucketFactory(this.filteredRequests()));
  readonly departmentStats = computed(() => this.requestService.departmentSummaryFactory(this.filteredRequests()));
  readonly templateFields = computed(() => REQUEST_TYPE_TEMPLATES[this.createForm().type] ?? []);

  readonly ownerSelectOptions = computed<CustomSelectOption[]>(() => {
    const departmentId =
      this.assigningRequest()?.targetDepartmentId ?? this.createForm().targetDepartmentId;

    return this.requestService.getOwnersByDepartment(departmentId).map((owner) => ({
      value: owner.id,
      label: owner.name,
      description: owner.departmentName
    }));
  });

  openCreateModal() {
    const user = this.currentUser();
    if (!user || (!user.canCreateRequest && !this.hasCoordinatorAccess(user))) {
      this.feedbackMessage.set('Tài khoản hiện tại không có quyền tạo yêu cầu.');
      return;
    }

    this.createForm.set(this.getInitialForm());
    this.feedbackMessage.set('');
    this.isCreateModalOpen.set(true);
  }

  closeCreateModal() {
    this.isCreateModalOpen.set(false);
  }

  updateTextField<K extends keyof CreateInterDepartmentRequest>(field: K, value: CreateInterDepartmentRequest[K]) {
    this.createForm.update((form) => ({
      ...form,
      [field]: value
    }));
  }

  updateRequestType(type: RequestType) {
    this.createForm.update((form) => ({
      ...form,
      type,
      formValues: this.buildTemplateValues(type)
    }));
  }

  updateTargetDepartment(departmentId: string) {
    const department = this.departments.find((item) => item.id === departmentId);

    this.createForm.update((form) => ({
      ...form,
      targetDepartmentId: department?.id ?? '',
      targetDepartment: department?.name ?? ''
    }));
  }

  updateTemplateValue(key: string, value: string) {
    this.createForm.update((form) => ({
      ...form,
      formValues: {
        ...form.formValues,
        [key]: value
      }
    }));
  }

  updateFilter<K extends keyof RequestFilters>(field: K, value: RequestFilters[K]) {
    this.filters.update((filters) => ({
      ...filters,
      [field]: value
    }));
  }

  clearFilters() {
    this.filters.set({
      type: '',
      targetDepartmentId: '',
      status: ''
    });
  }

  createRequest() {
    const user = this.currentUser();
    const form = this.createForm();

    if (!user) {
      this.feedbackMessage.set('Không xác định được người dùng đăng nhập.');
      return;
    }

    if (!form.title.trim() || !form.description.trim()) {
      this.feedbackMessage.set('Cần nhập tiêu đề và mô tả trước khi lưu phiếu.');
      return;
    }

    const result = this.requestService.createRequest(form);
    this.feedbackMessage.set(
      result.success ? 'Phiếu yêu cầu được tạo thành công.' : result.message ?? 'Không thể tạo phiếu.'
    );

    if (result.success) {
      this.closeCreateModal();
      this.createForm.set(this.getInitialForm());
    }
  }

  openDetail(request: InterDepartmentRequest) {
    this.selectedRequest.set(request);
    this.messageDraft.set('');
  }

  closeDetail() {
    this.selectedRequest.set(null);
    this.messageDraft.set('');
  }

  openStatusModal(request: InterDepartmentRequest) {
    this.editingRequest.set(request);
    this.editingStatus.set(request.status === 'new' ? 'received' : request.status);
  }

  closeStatusModal() {
    this.editingRequest.set(null);
  }

  openAssignModal(request: InterDepartmentRequest) {
    this.assigningRequest.set(request);
    this.selectedOwnerId.set('');
  }

  closeAssignModal() {
    this.assigningRequest.set(null);
    this.selectedOwnerId.set('');
  }

  acknowledgeRequest(request: InterDepartmentRequest) {
    const user = this.currentUser();
    if (!user) {
      return;
    }

    const result = this.requestService.acknowledgeRequest(request.id, user);
    this.handleResult(result, request.id);
  }

  assignOwner() {
    const user = this.currentUser();
    const request = this.assigningRequest();

    if (!user || !request) {
      return;
    }

    if (!this.selectedOwnerId()) {
      this.feedbackMessage.set('Cần chọn người xử lý trước khi lưu phân công.');
      return;
    }

    const result = this.requestService.assignOwner(request.id, this.selectedOwnerId(), user);
    this.handleResult(result, request.id);

    if (result.success) {
      this.closeAssignModal();
    }
  }

  saveStatusUpdate() {
    const user = this.currentUser();
    const request = this.editingRequest();

    if (!user || !request) {
      return;
    }

    const result = this.requestService.updateStatus(request.id, this.editingStatus(), user);
    this.handleResult(result, request.id);

    if (result.success) {
      this.closeStatusModal();
    }
  }

  addMessageToSelectedRequest() {
    const user = this.currentUser();
    const request = this.selectedRequest();
    const draft = this.messageDraft().trim();

    if (!user || !request || !draft) {
      return;
    }

    const result = this.requestService.addMessage({
      requestId: request.id,
      actor: user,
      body: draft
    });

    this.handleResult(result, request.id);

    if (result.success) {
      this.messageDraft.set('');
    }
  }

  closeSelectedRequest() {
    const user = this.currentUser();
    const request = this.selectedRequest();

    if (!user || !request) {
      return;
    }

    const result = this.requestService.closeRequest(request.id, user);
    this.handleResult(result, request.id);
  }

  canAcknowledge(request: InterDepartmentRequest) {
    const user = this.currentUser();
    return (
      !!user &&
      (this.hasCoordinatorAccess(user) || user.departmentCode === request.targetDepartmentId) &&
      request.status === 'new'
    );
  }

  canAssign(request: InterDepartmentRequest) {
    const user = this.currentUser();
    return (
      !!user &&
      (this.hasCoordinatorAccess(user) || user.departmentCode === request.targetDepartmentId) &&
      request.status === 'received'
    );
  }

  canUpdateStatus(request: InterDepartmentRequest) {
    const user = this.currentUser();
    return (
      !!user &&
      (this.hasCoordinatorAccess(user) || user.departmentCode === request.targetDepartmentId) &&
      !!request.ownerId
    );
  }

  canClose(request: InterDepartmentRequest) {
    const user = this.currentUser();
    return (
      !!user &&
      (this.hasCoordinatorAccess(user) || user.id === request.requesterUserId) &&
      request.status === 'done'
    );
  }

  getSessionRoleLabel(user: MockAuthUser) {
    if (this.hasCoordinatorAccess(user)) {
      return 'Điều phối';
    }

    return user.requestAccessRole === 'requester' ? 'Bên gửi' : 'Bên nhận';
  }

  getStatusLabel(status: RequestStatus) {
    return this.requestService.getStatusLabel(status);
  }

  getTypeLabel(type: RequestType) {
    return this.requestService.getTypeLabel(type);
  }

  getSlaChipLabel(request: InterDepartmentRequest) {
    if (request.sla.breached) {
      return `Quá ${Math.abs(request.sla.remainingHours)}h`;
    }

    return `Còn ${request.sla.remainingHours}h`;
  }

  getTemplateEntries(request: InterDepartmentRequest) {
    return Object.entries(request.formValues);
  }

  trackByKey(index: number, entry: [string, string]) {
    return `${index}-${entry[0]}`;
  }

  private handleResult(result: { success: boolean; message?: string }, requestId: string) {
    this.feedbackMessage.set(result.message ?? (result.success ? 'Cập nhật thành công.' : 'Không thể thực hiện thao tác.'));

    if (!result.success) {
      return;
    }

    const refreshed = this.availableRequests().find((item) => item.id === requestId)
      ?? this.requestService.requests().find((item) => item.id === requestId)
      ?? null;

    if (this.selectedRequest()?.id === requestId && refreshed) {
      this.selectedRequest.set(refreshed);
    }

    if (this.editingRequest()?.id === requestId && refreshed) {
      this.editingRequest.set(refreshed);
    }

    if (this.assigningRequest()?.id === requestId && refreshed) {
      this.assigningRequest.set(refreshed);
    }
  }

  private getInitialForm(): CreateInterDepartmentRequest {
    const user = this.currentUser();
    const type: RequestType = 'it-support';

    return {
      type,
      title: '',
      description: '',
      requesterDepartment: user?.departmentName ?? '',
      requesterDepartmentId: user?.departmentCode ?? '',
      requesterName: user?.fullName ?? '',
      requesterUserId: user?.id ?? 0,
      targetDepartment: '',
      targetDepartmentId: '',
      priority: 'High',
      dueDate: '2026-04-28',
      formValues: this.buildTemplateValues(type),
      note: ''
    };
  }

  private buildTemplateValues(type: RequestType) {
    return REQUEST_TYPE_TEMPLATES[type].reduce<Record<string, string>>((result, field) => {
      result[field.key] = '';
      return result;
    }, {});
  }

  private hasCoordinatorAccess(user: MockAuthUser) {
    const role = user.role?.toLowerCase() ?? '';
    return (
      user.requestAccessRole === 'coordinator' ||
      role.includes('admin') ||
      role.includes('lanh dao') ||
      role.includes('lãnh đạo')
    );
  }
}
