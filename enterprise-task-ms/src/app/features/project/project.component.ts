import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { RouterModule } from '@angular/router';

import { getTaskStatusLabel, TASK_STATUS_IDS } from '../../core/constants/task-status.constants';
import { EntityId } from '../../core/models/common-id.model';
import { ProjectOverview, ProjectService } from '../../core/services/project.service';
import { Task } from '../../core/models/task.model';
import { TaskService } from '../../core/services/task.service';
import { TaskDetailDrawerComponent } from '../task/components/task-detail-drawer/task-detail-drawer.component';

@Component({
  selector: 'app-project',
  standalone: true,
  imports: [CommonModule, RouterModule, TaskDetailDrawerComponent],
  templateUrl: './project.component.html',
  styleUrl: './project.component.scss'
})
export class ProjectComponent {
  private readonly projectService = inject(ProjectService);
  private readonly taskService = inject(TaskService);

  readonly overviews = this.projectService.overviews;
  readonly formOptions = this.taskService.formOptions;
  readonly selectedProjectId = signal<EntityId>(1);
  readonly selectedTask = signal<Task | null>(null);

  readonly selectedOverview = computed<ProjectOverview | null>(
    () => this.overviews().find((overview) => overview.project.id === this.selectedProjectId()) ?? this.overviews()[0] ?? null
  );

  readonly summaryCards = computed(() => {
    const overviews = this.overviews();
    const taskCount = overviews.reduce((sum, overview) => sum + overview.taskCount, 0);
    const completionRate = overviews.length
      ? Math.round(overviews.reduce((sum, overview) => sum + overview.completionRate, 0) / overviews.length)
      : 0;

    return [
      { label: 'Dự án', value: overviews.length, helper: 'Đang quản lý riêng theo projectId' },
      { label: 'Công việc', value: taskCount, helper: 'Tổng số công việc trong các dự án' },
      { label: 'Tiến độ TB', value: `${completionRate}%`, helper: 'Tổng hợp từ tiến độ công việc' }
    ];
  });

  selectProject(projectId: EntityId) {
    this.selectedProjectId.set(projectId);
    this.selectedTask.set(null);
  }

  openTask(task: Task) {
    this.selectedTask.set(task);
  }

  closeTask() {
    this.selectedTask.set(null);
  }

  handleTaskChanged(task: Task) {
    this.selectedTask.set(task);
  }

  getStatusLabel(statusId?: number) {
    return getTaskStatusLabel(statusId);
  }

  getDepartmentLabel(departmentId?: number) {
    if (!departmentId) {
      return 'Chưa chọn';
    }

    return this.formOptions().departments.find((department) => department.id === departmentId)?.label ?? `PB-${departmentId}`;
  }

  getUserLabel(userId?: EntityId) {
    if (!userId) {
      return 'Chưa chọn';
    }

    return this.formOptions().users.find((user) => user.id === userId)?.label ?? `NV-${String(userId).slice(0, 8)}`;
  }

  getDueClass(task: Task) {
    if (task.statusId === TASK_STATUS_IDS.QUA_HAN) {
      return 'overdue';
    }

    if (!task.dueDate) {
      return 'normal';
    }

    return new Date(task.dueDate).getTime() < Date.now() ? 'overdue' : 'normal';
  }
}
