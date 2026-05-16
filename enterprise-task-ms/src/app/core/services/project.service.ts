import { Injectable, computed, signal } from '@angular/core';

import { Project } from '../models/project.model';
import { Task } from '../models/task.model';
import { TaskService } from './task.service';

export interface ProjectOverview {
  project: Project;
  tasks: Task[];
  taskCount: number;
  completionRate: number;
  overdueCount: number;
  activeCount: number;
}

const date = (day: number) => new Date(2026, 3, day);

const PROJECT_MOCK: Project[] = [
  {
    id: 1,
    code: 'DA-001',
    name: 'Kế hoạch vận hành tháng 4',
    description: 'Tập hợp các đầu việc điều phối vận hành, báo cáo và chuẩn hóa quy trình nội bộ.',
    departmentId: 1,
    ownerId: 1,
    startDate: date(20),
    endDate: new Date(2026, 4, 5),
    status: 'active',
    createdBy: 1,
    createdAt: date(20),
    updatedAt: date(25)
  },
  {
    id: 2,
    code: 'DA-002',
    name: 'Onboarding và nhân sự tháng 5',
    description: 'Quản lý các công việc liên quan nhân sự mới, tài khoản, thiết bị và truyền thông nội bộ.',
    departmentId: 2,
    ownerId: 2,
    startDate: date(22),
    endDate: new Date(2026, 4, 8),
    status: 'active',
    createdBy: 1,
    createdAt: date(22),
    updatedAt: date(25)
  },
  {
    id: 3,
    code: 'DA-003',
    name: 'Kiểm soát tài chính và phê duyệt',
    description: 'Theo dõi chứng từ, ngân sách, hợp đồng và các đầu việc cần phê duyệt trong kỳ.',
    departmentId: 3,
    ownerId: 3,
    startDate: date(18),
    endDate: new Date(2026, 4, 3),
    status: 'at-risk',
    createdBy: 1,
    createdAt: date(18),
    updatedAt: date(25)
  },
  {
    id: 4,
    code: 'DA-004',
    name: 'Sẵn sàng hệ thống và hạ tầng nội bộ',
    description: 'Điều phối các công việc CNTT, backup, VPN, tài khoản và an toàn hệ thống.',
    departmentId: 5,
    ownerId: 5,
    startDate: date(21),
    endDate: new Date(2026, 4, 6),
    status: 'active',
    createdBy: 1,
    createdAt: date(21),
    updatedAt: date(25)
  }
];

@Injectable({ providedIn: 'root' })
export class ProjectService {
  readonly projects = signal<Project[]>(PROJECT_MOCK.map((project) => ({ ...project })));

  readonly overviews = computed(() =>
    this.projects().map((project) => this.createOverview(project, this.taskService.tasks()))
  );

  constructor(private readonly taskService: TaskService) {}

  getProjectOptions() {
    return this.projects().map((project) => ({
      value: project.id,
      label: `${project.code ?? `DA-${project.id}`} - ${project.name}`
    }));
  }

  getOverview(projectId: number) {
    const project = this.projects().find((item) => item.id === projectId);
    return project ? this.createOverview(project, this.taskService.tasks()) : null;
  }

  getTasksByProjectId(projectId: number) {
    return this.taskService.tasks().filter((task) => task.projectId === projectId);
  }

  private createOverview(project: Project, tasks: Task[]): ProjectOverview {
    const projectTasks = tasks.filter((task) => task.projectId === project.id);
    const completionRate = projectTasks.length
      ? Math.round(projectTasks.reduce((sum, task) => sum + task.progress, 0) / projectTasks.length)
      : 0;

    return {
      project,
      tasks: projectTasks,
      taskCount: projectTasks.length,
      completionRate,
      overdueCount: projectTasks.filter((task) => this.isOverdue(task)).length,
      activeCount: projectTasks.filter((task) => ![8, 9].includes(task.statusId ?? -1)).length
    };
  }

  private isOverdue(task: Task) {
    if (task.statusId === 10) {
      return true;
    }

    if (!task.dueDate) {
      return false;
    }

    return new Date(task.dueDate).getTime() < Date.now();
  }
}
