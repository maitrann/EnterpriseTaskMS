import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import {
  CreateInterDepartmentRequest,
  InterDepartmentRequest,
  RequestPriority,
  RequestStatus
} from '../../core/models/inter-department-request.model';
import { InterDepartmentRequestService } from '../../core/services/inter-department-request.service';
import {
  CustomSelectComponent,
  CustomSelectOption
} from '../../shared/ui/custom-select/custom-select.component';

type RequestFilters = {
  requesterDepartment: string;
  targetDepartment: string;
  status: string;
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

  readonly departmentOptions = this.requestService.departmentOptions;
  readonly ownerOptions = this.requestService.ownerOptions;
  readonly statusOptions = this.requestService.statusOptions;
  readonly requests = this.requestService.requests;
  readonly departmentSelectOptions = computed<CustomSelectOption[]>(() =>
    [
      { value: '', label: 'Tất cả' },
      ...this.departmentOptions.map((department) => ({ value: department, label: department }))
    ]
  );
  readonly ownerSelectOptions = computed<CustomSelectOption[]>(() =>
    this.ownerOptions.map((owner) => ({ value: owner, label: owner }))
  );
  readonly statusSelectOptions = computed<CustomSelectOption[]>(() => [
    { value: '', label: 'Tất cả' },
    ...this.statusOptions
  ]);
  readonly priorityOptions: CustomSelectOption<RequestPriority>[] = [
    { value: 'Low', label: 'Thấp' },
    { value: 'Medium', label: 'Trung bình' },
    { value: 'High', label: 'Cao' },
    { value: 'Critical', label: 'Khẩn cấp' }
  ];

  readonly filters = signal<RequestFilters>({
    requesterDepartment: '',
    targetDepartment: '',
    status: ''
  });

  readonly selectedRequest = signal<InterDepartmentRequest | null>(null);
  readonly editingRequest = signal<InterDepartmentRequest | null>(null);
  readonly editingStatus = signal<RequestStatus>('new');

  readonly isCreateModalOpen = signal(false);
  readonly createForm = signal<CreateInterDepartmentRequest>(this.getInitialForm());

  readonly filteredRequests = computed(() => {
    const activeFilters = this.filters();

    return this.requests().filter((request) => {
      const matchesRequester =
        !activeFilters.requesterDepartment ||
        request.requesterDepartment === activeFilters.requesterDepartment;
      const matchesTarget =
        !activeFilters.targetDepartment || request.targetDepartment === activeFilters.targetDepartment;
      const matchesStatus = !activeFilters.status || request.status === activeFilters.status;

      return matchesRequester && matchesTarget && matchesStatus;
    });
  });

  readonly summaryCards = computed(() => this.requestService.summaryFactory(this.filteredRequests()));
  readonly statusBuckets = computed(() => this.requestService.statusBucketFactory(this.filteredRequests()));

  openCreateModal() {
    this.createForm.set(this.getInitialForm());
    this.isCreateModalOpen.set(true);
  }

  closeCreateModal() {
    this.isCreateModalOpen.set(false);
  }

  updateField<K extends keyof CreateInterDepartmentRequest>(
    field: K,
    value: CreateInterDepartmentRequest[K]
  ) {
    this.createForm.update((form) => ({
      ...form,
      [field]: value
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
      requesterDepartment: '',
      targetDepartment: '',
      status: ''
    });
  }

  createMockRequest() {
    const form = this.createForm();
    if (!form.title.trim()) {
      return;
    }

    this.requestService.createRequest(form);
    this.closeCreateModal();
  }

  openDetail(request: InterDepartmentRequest) {
    this.selectedRequest.set(request);
  }

  closeDetail() {
    this.selectedRequest.set(null);
  }

  openStatusModal(request: InterDepartmentRequest) {
    this.editingRequest.set(request);
    this.editingStatus.set(request.status);
  }

  closeStatusModal() {
    this.editingRequest.set(null);
  }

  saveStatusUpdate() {
    const request = this.editingRequest();
    if (!request) {
      return;
    }

    this.requestService.updateStatus(request.id, this.editingStatus());
    this.closeStatusModal();
  }

  getStatusLabel(status: RequestStatus) {
    return this.requestService.getStatusLabel(status);
  }

  private getInitialForm(): CreateInterDepartmentRequest {
    return {
      title: '',
      requesterDepartment: this.departmentOptions[0],
      targetDepartment: this.departmentOptions[1],
      owner: this.ownerOptions[0],
      priority: 'High' as RequestPriority,
      dueDate: '2026-04-22',
      note: ''
    };
  }
}
