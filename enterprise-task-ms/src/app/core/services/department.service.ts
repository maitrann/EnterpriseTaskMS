import { Inject, Injectable, computed, signal } from '@angular/core';

import {
  DEPARTMENT_DATA_SOURCE,
  DepartmentDataSource
} from '../data-sources/department.datasource';
import { DepartmentCard } from '../models/department-card.model';

@Injectable({ providedIn: 'root' })
export class DepartmentService {
  readonly departmentCards = signal<DepartmentCard[]>([]);

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
    @Inject(DEPARTMENT_DATA_SOURCE) private readonly departmentDataSource: DepartmentDataSource
  ) {
    this.departmentCards.set(this.departmentDataSource.getDepartmentCards());
  }
}
