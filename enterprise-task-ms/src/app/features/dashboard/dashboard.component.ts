import { CommonModule, DatePipe } from '@angular/common';
import { Component, computed, inject } from '@angular/core';
import { RouterModule } from '@angular/router';

import {
  TASK_COMPLETED_STATUS_IDS,
  TASK_TERMINAL_STATUS_IDS
} from '../../core/constants/task-status.constants';
import { DepartmentService } from '../../core/services/department.service';
import { TaskService } from '../../core/services/task.service';

@Component({
  standalone: true,
  selector: 'app-dashboard',
  imports: [CommonModule, RouterModule, DatePipe],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent {
  private readonly taskService = inject(TaskService);
  private readonly departmentService = inject(DepartmentService);

  private readonly tasks = this.taskService.tasks;

  readonly kpis = computed(() => {
    const tasks = this.tasks();
    const total = tasks.length;
    const done = tasks.filter((task) => TASK_COMPLETED_STATUS_IDS.includes(task.statusId ?? -1)).length;
    const inProgress = tasks.filter((task) => !TASK_TERMINAL_STATUS_IDS.includes(task.statusId ?? -1)).length;
    const overdue = tasks.filter((task) => this.getDueState(task.dueDate) === 'overdue').length;

    return [
      {
        label: 'Tổng công việc',
        value: total,
        helper: 'Toàn bộ đầu việc đang được quản lý',
        tone: 'blue'
      },
      {
        label: 'Đang xử lý',
        value: inProgress,
        helper: 'Cần theo dõi sát trong ngày',
        tone: 'amber'
      },
      {
        label: 'Đã hoàn tất',
        value: done,
        helper: 'Đã chốt và sẵn sàng đối chiếu',
        tone: 'emerald'
      },
      {
        label: 'Trễ hạn',
        value: overdue,
        helper: 'Cần ưu tiên can thiệp',
        tone: 'red'
      }
    ];
  });

  readonly departmentLoad = this.departmentService.departmentCards;

  readonly spotlightTasks = computed(() =>
    [...this.tasks()]
      .filter((task) => !TASK_TERMINAL_STATUS_IDS.includes(task.statusId ?? -1))
      .sort((first, second) => (second.priorityId ?? 0) - (first.priorityId ?? 0))
      .slice(0, 5)
  );

  readonly deadlineBuckets = computed(() => {
    const tasks = this.tasks();
    const overdue = tasks.filter((task) => this.getDueState(task.dueDate) === 'overdue').length;
    const warning = tasks.filter((task) => this.getDueState(task.dueDate) === 'warning').length;
    const normal = tasks.filter((task) => this.getDueState(task.dueDate) === 'normal').length;

    return [
      { label: 'Trễ hạn', value: overdue, tone: 'overdue' },
      { label: 'Sắp đến hạn', value: warning, tone: 'warning' },
      { label: 'Đúng tiến độ', value: normal, tone: 'normal' }
    ];
  });

  getDueState(dueDate?: Date) {
    if (!dueDate) {
      return 'normal';
    }

    const today = new Date();
    const due = new Date(dueDate);
    const diff = (due.getTime() - today.getTime()) / (1000 * 60 * 60 * 24);

    if (diff < 0) {
      return 'overdue';
    }

    if (diff <= 2) {
      return 'warning';
    }

    return 'normal';
  }

  getPriorityLabel(priorityId?: number) {
    switch (priorityId) {
      case 1:
        return 'Thấp';
      case 2:
        return 'Trung bình';
      case 3:
        return 'Cao';
      case 4:
        return 'Khẩn cấp';
      default:
        return 'Chưa gán';
    }
  }
}
