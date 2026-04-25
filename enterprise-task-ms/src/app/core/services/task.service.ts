import { Inject, Injectable, computed, signal } from '@angular/core';

import { TASK_TERMINAL_STATUS_IDS } from '../constants/task-status.constants';
import { TASK_DATA_SOURCE, TaskDataSource } from '../data-sources/task.datasource';
import { CreateTaskInput, TaskFormOptions } from '../models/task-form.model';
import { TaskActivity } from '../models/task-activity.model';
import { Task } from '../models/task.model';
import { AuthService } from './auth.service';

@Injectable({ providedIn: 'root' })
export class TaskService {
  readonly tasks = signal<Task[]>([]);
  readonly activities = signal<TaskActivity[]>([]);
  readonly formOptions = signal<TaskFormOptions>(this.createEmptyFormOptions());

  readonly activeTasks = computed(() =>
    this.tasks().filter((task) => !TASK_TERMINAL_STATUS_IDS.includes(task.statusId ?? -1))
  );

  constructor(
    @Inject(TASK_DATA_SOURCE) private readonly taskDataSource: TaskDataSource,
    private readonly authService: AuthService
  ) {
    this.tasks.set(this.taskDataSource.getTasks());
    this.activities.set(this.taskDataSource.getTaskActivities());
    this.formOptions.set(this.taskDataSource.getTaskFormOptions());
  }

  getAll() {
    return this.tasks();
  }

  getById(id: number) {
    return this.tasks().find((task) => task.id === id) ?? null;
  }

  getActivitiesByTaskId(taskId: number) {
    return this.activities().filter((activity) => activity.taskId === taskId);
  }

  getParentTaskOptions(currentTaskId?: number) {
    return this.tasks()
      .filter((task) => task.id !== currentTaskId)
      .map((task) => ({
        value: task.id,
        label: `${task.code} - ${task.title}`
      }));
  }

  createTask(input: CreateTaskInput) {
    const nextId = this.tasks().length ? Math.max(...this.tasks().map((task) => task.id)) + 1 : 1;
    const now = new Date();
    const nextTask: Task = {
      id: nextId,
      code: `CV-${String(nextId).padStart(4, '0')}`,
      projectId: 1,
      parentTaskId: input.parentTaskId,
      title: input.title.trim(),
      description: input.description?.trim(),
      taskType: input.taskType,
      departmentId: input.departmentId,
      statusId: 1,
      priorityId: input.priorityId ?? 2,
      urgencyLevel: input.urgencyLevel,
      securityLevel: input.securityLevel,
      reporterId: 1,
      assigneeId: input.assigneeId,
      collaboratorIds: [...input.collaboratorIds],
      watcherIds: [...input.watcherIds],
      startDate: input.startDate,
      dueDate: input.dueDate,
      progress: 0,
      source: input.source,
      attachmentNames: [...input.attachmentNames],
      tags: [...input.tags],
      processingNotes: [],
      estimatedHours: input.estimatedHours ?? 0,
      actualHours: 0,
      createdAt: now,
      updatedAt: now
    };

    this.tasks.update((tasks) => [nextTask, ...tasks]);

    this.activities.update((activities) => [
      {
        id: Date.now(),
        taskId: nextTask.id,
        userId: 1,
        actionType: 'CREATE_TASK',
        newValue: nextTask.title,
        createdAt: now
      },
      ...activities
    ]);

    return nextTask;
  }

  updateTask(updatedTask: Task) {
    const currentTask = this.getById(updatedTask.id);

    if (!currentTask) {
      return {
        success: false,
        message: 'Không tìm thấy công việc để cập nhật.'
      };
    }

    if (!this.authService.canEditTask(currentTask)) {
      return {
        success: false,
        message: 'Bạn không có quyền sửa công việc của bộ phận khác.'
      };
    }

    this.tasks.update((tasks) =>
      tasks.map((task) =>
        task.id === updatedTask.id
          ? {
              ...updatedTask,
              collaboratorIds: [...(updatedTask.collaboratorIds ?? [])],
              watcherIds: [...(updatedTask.watcherIds ?? [])],
              attachmentNames: [...(updatedTask.attachmentNames ?? [])],
              tags: [...(updatedTask.tags ?? [])],
              processingNotes: [...(updatedTask.processingNotes ?? [])]
            }
          : task
      )
    );

    this.recordTaskUpdateActivities(currentTask, updatedTask);

    return {
      success: true
    };
  }

  replaceAll(tasks: Task[]) {
    this.tasks.set(tasks.map((task) => ({ ...task })));
  }

  private createEmptyFormOptions(): TaskFormOptions {
    return {
      taskTypes: [],
      departments: [],
      users: [],
      priorities: [],
      urgencyLevels: [],
      securityLevels: [],
      sources: []
    };
  }

  private recordTaskUpdateActivities(previousTask: Task, updatedTask: Task) {
    const userId = this.authService.user()?.id ?? 1;
    const now = new Date();
    const nextActivities: TaskActivity[] = [];

    if (previousTask.assigneeId !== updatedTask.assigneeId) {
      nextActivities.push({
        id: Date.now() + nextActivities.length,
        taskId: updatedTask.id,
        userId,
        actionType: 'assignee_change',
        oldValue: previousTask.assigneeId ? `User ${previousTask.assigneeId}` : 'Chưa phân công',
        newValue: updatedTask.assigneeId ? `User ${updatedTask.assigneeId}` : 'Chưa phân công',
        createdAt: now
      });
    }

    if (previousTask.statusId !== updatedTask.statusId) {
      nextActivities.push({
        id: Date.now() + nextActivities.length + 10,
        taskId: updatedTask.id,
        userId,
        actionType: 'status_change',
        oldValue: previousTask.statusId ? `Trạng thái ${previousTask.statusId}` : 'Chưa xác định',
        newValue: updatedTask.statusId ? `Trạng thái ${updatedTask.statusId}` : 'Chưa xác định',
        createdAt: now
      });
    }

    if (!nextActivities.length) {
      return;
    }

    this.activities.update((activities) => [...nextActivities, ...activities]);
  }
}
