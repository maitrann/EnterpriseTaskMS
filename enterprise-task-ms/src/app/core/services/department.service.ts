import { HttpClient } from '@angular/common/http';
import { Inject, Injectable, computed, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { API_BASE_URL } from '../constants/app.constants';
import {
  DEPARTMENT_DATA_SOURCE,
  DepartmentDataSource
} from '../data-sources/department.datasource';
import { DepartmentCard } from '../models/department-card.model';
import { DepartmentOption } from '../models/department-card.model';

@Injectable({ providedIn: 'root' })
export class DepartmentService {
  readonly departmentCards = signal<DepartmentCard[]>([]);
  readonly departmentOptions = signal<DepartmentOption[]>([]);

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
}
