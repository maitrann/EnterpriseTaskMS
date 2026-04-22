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
      { label: 'Tong phong ban', value: departments.length, helper: 'Khoi van phong dang duoc mock' },
      { label: 'Nhan su', value: totalMembers, helper: 'Tong thanh vien dang phan bo' },
      { label: 'Task active', value: activeTasks, helper: 'Cong viec dang xu ly theo phong ban' },
      { label: 'SLA trung binh', value: `${averageSla}%`, helper: 'Muc dap ung tiep nhan va xu ly' },
      { label: 'Da hoan tat', value: completedTasks, helper: 'Dau viec da dong trong ky' }
    ];
  });

  constructor(
    @Inject(DEPARTMENT_DATA_SOURCE) private readonly departmentDataSource: DepartmentDataSource
  ) {
    this.departmentCards.set(this.departmentDataSource.getDepartmentCards());
  }
}
