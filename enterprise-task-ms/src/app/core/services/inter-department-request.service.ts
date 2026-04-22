import { Inject, Injectable, computed, signal } from '@angular/core';

import {
  INTER_DEPARTMENT_REQUEST_DATA_SOURCE,
  InterDepartmentRequestDataSource
} from '../data-sources/inter-department-request.datasource';
import {
  CreateInterDepartmentRequest,
  InterDepartmentRequest,
  RequestStatus
} from '../models/inter-department-request.model';

@Injectable({ providedIn: 'root' })
export class InterDepartmentRequestService {
  readonly departmentOptions: string[];
  readonly ownerOptions: string[];
  readonly requests = signal<InterDepartmentRequest[]>([]);

  readonly statusOptions: Array<{ value: RequestStatus; label: string }> = [
    { value: 'new', label: 'Moi tiep nhan' },
    { value: 'processing', label: 'Dang xu ly' },
    { value: 'waiting', label: 'Cho xac nhan' },
    { value: 'done', label: 'Hoan tat' }
  ];

  readonly requestsByStatus = computed(() => this.statusBucketFactory(this.requests()));

  constructor(
    @Inject(INTER_DEPARTMENT_REQUEST_DATA_SOURCE)
    private readonly requestDataSource: InterDepartmentRequestDataSource
  ) {
    this.departmentOptions = this.requestDataSource.getDepartmentOptions();
    this.ownerOptions = this.requestDataSource.getOwnerOptions();
    this.requests.set(this.requestDataSource.getRequests());
  }

  createRequest(payload: CreateInterDepartmentRequest) {
    const nextId = `IR-${String(this.requests().length + 1).padStart(3, '0')}`;
    const newRequest: InterDepartmentRequest = {
      id: nextId,
      title: payload.title.trim(),
      requesterDepartment: payload.requesterDepartment.trim(),
      targetDepartment: payload.targetDepartment.trim(),
      owner: payload.owner.trim(),
      priority: payload.priority,
      status: 'new',
      sla: 'Moi tiep nhan',
      dueDate: this.formatDate(payload.dueDate),
      note: payload.note.trim() || 'Yeu cau moi duoc tao tu workflow mock.'
    };

    this.requests.update((requests) => [newRequest, ...requests]);
  }

  updateStatus(requestId: string, status: RequestStatus) {
    this.requests.update((requests) =>
      requests.map((request) =>
        request.id === requestId
          ? {
              ...request,
              status,
              sla: this.getSlaLabel(status)
            }
          : request
      )
    );
  }

  readonly summaryFactory = (items: InterDepartmentRequest[]) => [
    { label: 'Tong yeu cau', value: items.length, helper: 'Du lieu sau khi ap dung bo loc hien tai' },
    {
      label: 'Dang xu ly',
      value: items.filter((request) => request.status !== 'done').length,
      helper: 'Dang o trang thai tiep nhan, xu ly hoac cho xac nhan'
    },
    {
      label: 'Cho xac nhan',
      value: items.filter((request) => request.status === 'waiting').length,
      helper: 'Can ben yeu cau phan hoi ket qua'
    },
    {
      label: 'Muc cao',
      value: items.filter((request) => request.priority === 'Critical').length,
      helper: 'Can theo doi sat vi anh huong tien do'
    }
  ];

  readonly statusBucketFactory = (items: InterDepartmentRequest[]) => [
    { label: 'Moi tiep nhan', count: items.filter((request) => request.status === 'new').length, tone: 'blue' },
    { label: 'Dang xu ly', count: items.filter((request) => request.status === 'processing').length, tone: 'amber' },
    { label: 'Cho xac nhan', count: items.filter((request) => request.status === 'waiting').length, tone: 'slate' },
    { label: 'Hoan tat', count: items.filter((request) => request.status === 'done').length, tone: 'emerald' }
  ];

  getStatusLabel(status: RequestStatus) {
    return this.statusOptions.find((option) => option.value === status)?.label ?? status;
  }

  private formatDate(value: string) {
    if (!value) {
      return 'Chua chot';
    }

    const [year, month, day] = value.split('-');
    return `${day}/${month}/${year}`;
  }

  private getSlaLabel(status: RequestStatus) {
    switch (status) {
      case 'new':
        return 'Moi tiep nhan';
      case 'processing':
        return 'Dang xu ly';
      case 'waiting':
        return 'Cho xac nhan';
      case 'done':
        return 'Dat SLA';
    }
  }
}
