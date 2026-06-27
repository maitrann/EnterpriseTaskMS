import { HttpClient } from '@angular/common/http';
import { Inject, Injectable, computed, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { API_BASE_URL } from '../constants/app.constants';
import {
  DEPARTMENT_DATA_SOURCE,
  DepartmentDataSource
} from '../data-sources/department.datasource';
import {
  DepartmentCard,
  DepartmentCreateRequest,
  DepartmentListItem,
  DepartmentManagerAssignmentRequest,
  DepartmentOption,
  DepartmentTreeNode,
  DepartmentUpdateRequest
} from '../models/department-card.model';

@Injectable({ providedIn: 'root' })
export class DepartmentService {
  readonly departmentCards = signal<DepartmentCard[]>([]);
  readonly departmentOptions = signal<DepartmentOption[]>([]);
  readonly departmentList = signal<DepartmentListItem[]>([]);
  readonly departmentTree = signal<DepartmentTreeNode[]>([]);

  readonly summaryCards = computed(() => {
    const departments = this.departmentCards();
    const totalMembers = departments.reduce((sum, department) => sum + department.members, 0);
    const activeTasks = departments.reduce((sum, department) => sum + department.activeTasks, 0);
    const completedTasks = departments.reduce((sum, department) => sum + department.completedTasks, 0);
    const averageSla = Math.round(
      departments.reduce((sum, department) => sum + Number.parseInt(department.sla, 10), 0) / departments.length
    );

    return [
      { label: 'Tổng phòng ban', value: departments.length, helper: 'Khối văn phòng đang dùng dữ liệu mẫu' },
      { label: 'Nhân sự', value: totalMembers, helper: 'Tổng thành viên đang phân bổ' },
      { label: 'Task đang xử lý', value: activeTasks, helper: 'Công việc đang xử lý theo phòng ban' },
      { label: 'SLA trung bình', value: `${averageSla}%`, helper: 'Mức đáp ứng tiếp nhận và xử lý' },
      { label: 'Đã hoàn tất', value: completedTasks, helper: 'Đầu việc đã đóng trong kỳ' }
    ];
  });

  constructor(
    @Inject(DEPARTMENT_DATA_SOURCE) private readonly departmentDataSource: DepartmentDataSource,
    private readonly http: HttpClient
  ) {
    this.departmentCards.set(this.departmentDataSource.getDepartmentCards());
    void this.loadFromApi();
    void this.loadOptions();
  }

  async loadFromApi() {
    try {
      this.departmentCards.set(
        await firstValueFrom(this.http.get<DepartmentCard[]>(`${API_BASE_URL}/departments/cards`))
      );
    } catch {
      // Keep mock cards while the API is offline.
    }
  }

  async loadOptions() {
    try {
      this.departmentOptions.set(
        await firstValueFrom(this.http.get<DepartmentOption[]>(`${API_BASE_URL}/departments/options`))
      );
    } catch {
      this.departmentOptions.set([]);
    }
  }

  async loadAdminList(includeInactive = true) {
    try {
      this.departmentList.set(
        await firstValueFrom(
          this.http.get<DepartmentListItem[]>(`${API_BASE_URL}/departments`, {
            params: { includeInactive }
          })
        )
      );
    } catch {
      this.departmentList.set([]);
    }
  }

  async loadAdminTree(includeInactive = true) {
    try {
      this.departmentTree.set(
        await firstValueFrom(
          this.http.get<DepartmentTreeNode[]>(`${API_BASE_URL}/departments/tree`, {
            params: { includeInactive }
          })
        )
      );
    } catch {
      this.departmentTree.set([]);
    }
  }

  async createDepartment(request: DepartmentCreateRequest) {
    await firstValueFrom(this.http.post(`${API_BASE_URL}/departments`, request));
    await this.reloadAdminContracts();
  }

  async updateDepartment(departmentId: number, request: DepartmentUpdateRequest) {
    await firstValueFrom(this.http.put(`${API_BASE_URL}/departments/${departmentId}`, request));
    await this.reloadAdminContracts();
  }

  async assignDepartmentManager(departmentId: number, request: DepartmentManagerAssignmentRequest) {
    await firstValueFrom(this.http.put(`${API_BASE_URL}/departments/${departmentId}/manager`, request));
    await this.reloadAdminContracts();
  }

  async deactivateDepartment(departmentId: number) {
    await firstValueFrom(this.http.post(`${API_BASE_URL}/departments/${departmentId}/deactivate`, {}));
    await this.reloadAdminContracts();
  }

  private async reloadAdminContracts() {
    await Promise.all([
      this.loadOptions(),
      this.loadAdminList(),
      this.loadAdminTree()
    ]);
  }
}
