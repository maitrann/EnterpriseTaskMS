import { Inject, Injectable, computed, signal } from '@angular/core';

import {
  TASK_STATUS_IDS,
  TASK_TERMINAL_STATUS_IDS,
  getTaskStatusLabel
} from '../constants/task-status.constants';
import { TASK_DATA_SOURCE, TaskDataSource } from '../data-sources/task.datasource';
import { CreateTaskInput, TaskFormOptions } from '../models/task-form.model';
import { TaskActivity } from '../models/task-activity.model';
import { SubTask, SubTaskInput } from '../models/subtask.model';
import { Task, TaskExtensionRequest } from '../models/task.model';
import { AuthService } from './auth.service';

export type TaskActionResult = {
  success: boolean;
  message?: string;
  task?: Task;
};

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

  getSubtasksByTaskId(taskId: number) {
    const task = this.getById(taskId);
    return task ? this.cloneSubtasks(this.getTaskSubtasks(task)) : [];
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
      projectId: input.projectId ?? 1,
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
      collaboratorIds: [...(input.collaboratorIds ?? [])],
      watcherIds: [...input.watcherIds],
      startDate: input.startDate,
      dueDate: input.dueDate,
      progress: 0,
      source: input.source,
      attachmentNames: [...input.attachmentNames],
      tags: [...input.tags],
      processingNotes: [],
      extensionRequests: [],
      subtasks: [],
      subtaskProgressAutoSync: true,
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

  acceptTask(taskId: number) {
    const task = this.getById(taskId);

    if (!task) {
      return this.notFoundResult();
    }

    if (task.statusId !== TASK_STATUS_IDS.CHO_TIEP_NHAN) {
      return {
        success: false,
        message: 'Chỉ nhận việc đang ở trạng thái Chờ tiếp nhận.'
      };
    }

    return this.applyTaskAction(
      task,
      (current) => ({
        ...current,
        statusId: TASK_STATUS_IDS.DANG_XU_LY,
        progress: Math.max(current.progress, 5)
      }),
      'accept_task',
      'Chờ tiếp nhận',
      'Đang xử lý'
    );
  }

  rejectAcceptance(taskId: number, reason: string) {
    const task = this.getById(taskId);

    if (!task) {
      return this.notFoundResult();
    }

    if (task.statusId !== TASK_STATUS_IDS.CHO_TIEP_NHAN) {
      return {
        success: false,
        message: 'Chỉ từ chối tiếp nhận khi công việc đang chờ tiếp nhận.'
      };
    }

    const note = reason.trim() || 'Từ chối tiếp nhận công việc.';
    return this.applyTaskAction(
      task,
      (current) => ({
        ...current,
        statusId: TASK_STATUS_IDS.TAM_DUNG,
        processingNotes: [`Từ chối tiếp nhận: ${note}`, ...(current.processingNotes ?? [])]
      }),
      'reject_acceptance',
      'Chờ tiếp nhận',
      'Tạm dừng chờ điều phối lại'
    );
  }

  requestExtension(taskId: number, dueDate: Date, reason: string) {
    const task = this.getById(taskId);

    if (!task) {
      return this.notFoundResult();
    }

    const note = reason.trim() || 'Xin gia hạn thời hạn xử lý.';
    const pendingRequest = (task.extensionRequests ?? []).find((request) => request.status === 'pending');

    if (pendingRequest) {
      return {
        success: false,
        message: 'Công việc đang có một yêu cầu gia hạn chờ duyệt.'
      };
    }

    const extensionRequest: TaskExtensionRequest = {
      id: Date.now(),
      requestedDueDate: dueDate,
      reason: note,
      status: 'pending',
      requestedByUserId: this.authService.user()?.id ?? 1,
      requestedAt: new Date()
    };

    return this.applyTaskAction(
      task,
      (current) => ({
        ...current,
        extensionRequests: [extensionRequest, ...(current.extensionRequests ?? [])],
        processingNotes: [`Xin gia hạn đến ${this.formatDate(dueDate)}: ${note}`, ...(current.processingNotes ?? [])]
      }),
      'request_extension',
      task.dueDate ? this.formatDate(task.dueDate) : 'Chưa có hạn',
      this.formatDate(dueDate)
    );
  }

  approveExtensionRequest(taskId: number, requestId: number, note: string) {
    const task = this.getById(taskId);

    if (!task) {
      return this.notFoundResult();
    }

    if (!this.canReviewTask(task)) {
      return {
        success: false,
        message: 'Chỉ người giao việc hoặc tài khoản có quyền đặc biệt mới được duyệt gia hạn.'
      };
    }

    const extensionRequest = (task.extensionRequests ?? []).find((request) => request.id === requestId);

    if (!extensionRequest || extensionRequest.status !== 'pending') {
      return {
        success: false,
        message: 'Không tìm thấy yêu cầu gia hạn đang chờ duyệt.'
      };
    }

    const reviewNote = note.trim();
    return this.applyTaskAction(
      task,
      (current) => ({
        ...current,
        dueDate: extensionRequest.requestedDueDate,
        extensionRequests: (current.extensionRequests ?? []).map((request) =>
          request.id === requestId
            ? {
                ...request,
                status: 'approved',
                reviewedByUserId: this.authService.user()?.id ?? 1,
                reviewedAt: new Date(),
                reviewNote
              }
            : request
        ),
        processingNotes: [
          `Duyệt gia hạn đến ${this.formatDate(extensionRequest.requestedDueDate)}${reviewNote ? `: ${reviewNote}` : ''}`,
          ...(current.processingNotes ?? [])
        ]
      }),
      'approve_extension',
      task.dueDate ? this.formatDate(task.dueDate) : 'Chưa có hạn',
      this.formatDate(extensionRequest.requestedDueDate)
    );
  }

  rejectExtensionRequest(taskId: number, requestId: number, note: string) {
    const task = this.getById(taskId);

    if (!task) {
      return this.notFoundResult();
    }

    if (!this.canReviewTask(task)) {
      return {
        success: false,
        message: 'Chỉ người giao việc hoặc tài khoản có quyền đặc biệt mới được từ chối gia hạn.'
      };
    }

    const extensionRequest = (task.extensionRequests ?? []).find((request) => request.id === requestId);

    if (!extensionRequest || extensionRequest.status !== 'pending') {
      return {
        success: false,
        message: 'Không tìm thấy yêu cầu gia hạn đang chờ duyệt.'
      };
    }

    const reviewNote = note.trim() || 'Không duyệt yêu cầu gia hạn.';
    return this.applyTaskAction(
      task,
      (current) => ({
        ...current,
        extensionRequests: (current.extensionRequests ?? []).map((request) =>
          request.id === requestId
            ? {
                ...request,
                status: 'rejected',
                reviewedByUserId: this.authService.user()?.id ?? 1,
                reviewedAt: new Date(),
                reviewNote
              }
            : request
        ),
        processingNotes: [`Từ chối gia hạn: ${reviewNote}`, ...(current.processingNotes ?? [])]
      }),
      'reject_extension',
      this.formatDate(extensionRequest.requestedDueDate),
      reviewNote
    );
  }

  transferAssignee(taskId: number, assigneeId: number, reason: string) {
    const task = this.getById(taskId);

    if (!task) {
      return this.notFoundResult();
    }

    if (!assigneeId) {
      return {
        success: false,
        message: 'Cần chọn người xử lý mới.'
      };
    }

    const note = reason.trim();
    return this.applyTaskAction(
      task,
      (current) => ({
        ...current,
        assigneeId,
        statusId: current.statusId === TASK_STATUS_IDS.TAM_DUNG ? TASK_STATUS_IDS.CHO_TIEP_NHAN : current.statusId,
        processingNotes: note ? [`Chuyển người xử lý: ${note}`, ...(current.processingNotes ?? [])] : current.processingNotes
      }),
      'transfer_assignee',
      task.assigneeId ? `User ${task.assigneeId}` : 'Chưa chọn',
      `User ${assigneeId}`
    );
  }

  createSubtask(taskId: number, input: SubTaskInput) {
    const task = this.getById(taskId);

    if (!task) {
      return this.notFoundResult();
    }

    const title = input.title.trim();
    if (!title) {
      return {
        success: false,
        message: 'Cần nhập tên nhiệm vụ con.'
      };
    }

    const deadlineCheck = this.validateSubtaskDeadline(task, input.dueDate);
    if (!deadlineCheck.success) {
      return deadlineCheck;
    }

    const subtasks = this.getTaskSubtasks(task);
    const now = Date.now();
    const progress = this.normalizeProgress(input.progress ?? 0);
    const nextSubtask: SubTask = {
      id: now,
      taskId,
      title,
      assigneeId: input.assigneeId,
      dueDate: input.dueDate,
      progress,
      done: progress === 100,
      createdAt: now,
      updatedAt: now,
      completedAt: progress === 100 ? now : undefined,
      order: subtasks.length + 1
    };

    return this.applyTaskAction(
      task,
      (current) =>
        this.withSubtaskRollup({
          ...current,
          subtasks: [...subtasks, nextSubtask]
        }),
      'subtask_create',
      undefined,
      title
    );
  }

  updateSubtask(taskId: number, subtaskId: number, changes: Partial<SubTaskInput>) {
    const task = this.getById(taskId);

    if (!task) {
      return this.notFoundResult();
    }

    const subtasks = this.getTaskSubtasks(task);
    const subtask = subtasks.find((item) => item.id === subtaskId);
    if (!subtask) {
      return {
        success: false,
        message: 'Không tìm thấy nhiệm vụ con.'
      };
    }

    const title = changes.title?.trim() ?? subtask.title;
    if (!title) {
      return {
        success: false,
        message: 'Cần nhập tên nhiệm vụ con.'
      };
    }

    const dueDate = changes.dueDate === undefined ? subtask.dueDate : changes.dueDate;
    const deadlineCheck = this.validateSubtaskDeadline(task, dueDate);
    if (!deadlineCheck.success) {
      return deadlineCheck;
    }

    const progress = this.normalizeProgress(changes.progress ?? subtask.progress);
    const now = Date.now();

    return this.applyTaskAction(
      task,
      (current) =>
        this.withSubtaskRollup({
          ...current,
          subtasks: subtasks.map((item) =>
            item.id === subtaskId
              ? {
                  ...item,
                  title,
                  assigneeId: changes.assigneeId === undefined ? item.assigneeId : changes.assigneeId,
                  dueDate,
                  progress,
                  done: progress === 100,
                  updatedAt: now,
                  completedAt: progress === 100 ? item.completedAt ?? now : undefined
                }
              : item
          )
        }),
      'subtask_update',
      subtask.title,
      title
    );
  }

  toggleSubtaskDone(taskId: number, subtaskId: number) {
    const task = this.getById(taskId);

    if (!task) {
      return this.notFoundResult();
    }

    const subtasks = this.getTaskSubtasks(task);
    const subtask = subtasks.find((item) => item.id === subtaskId);
    if (!subtask) {
      return {
        success: false,
        message: 'Không tìm thấy nhiệm vụ con.'
      };
    }

    const nextDone = !subtask.done;
    const now = Date.now();

    return this.applyTaskAction(
      task,
      (current) =>
        this.withSubtaskRollup({
          ...current,
          subtasks: subtasks.map((item) =>
            item.id === subtaskId
              ? {
                  ...item,
                  done: nextDone,
                  progress: nextDone ? 100 : 0,
                  updatedAt: now,
                  completedAt: nextDone ? now : undefined
                }
              : item
          )
        }),
      'subtask_toggle',
      subtask.done ? 'Done' : 'Open',
      nextDone ? 'Done' : 'Open'
    );
  }

  deleteSubtask(taskId: number, subtaskId: number) {
    const task = this.getById(taskId);

    if (!task) {
      return this.notFoundResult();
    }

    const subtasks = this.getTaskSubtasks(task);
    const subtask = subtasks.find((item) => item.id === subtaskId);
    if (!subtask) {
      return {
        success: false,
        message: 'Không tìm thấy nhiệm vụ con.'
      };
    }

    return this.applyTaskAction(
      task,
      (current) =>
        this.withSubtaskRollup({
          ...current,
          subtasks: subtasks
            .filter((item) => item.id !== subtaskId)
            .map((item, index) => ({ ...item, order: index + 1 }))
        }),
      'subtask_delete',
      subtask.title,
      undefined
    );
  }

  completeTask(taskId: number, note: string) {
    const task = this.getById(taskId);

    if (!task) {
      return this.notFoundResult();
    }

    return this.applyTaskAction(
      task,
      (current) => ({
        ...current,
        statusId: TASK_STATUS_IDS.HOAN_THANH,
        progress: 100,
        processingNotes: note.trim() ? [`Hoàn thành: ${note.trim()}`, ...(current.processingNotes ?? [])] : current.processingNotes
      }),
      'complete_task',
      getTaskStatusLabel(task.statusId),
      getTaskStatusLabel(TASK_STATUS_IDS.HOAN_THANH)
    );
  }

  confirmCompletion(taskId: number, note: string) {
    const task = this.getById(taskId);

    if (!task) {
      return this.notFoundResult();
    }

    if (task.statusId !== TASK_STATUS_IDS.HOAN_THANH) {
      return {
        success: false,
        message: 'Chỉ xác nhận hoàn thành khi công việc đã ở trạng thái Hoàn thành.'
      };
    }

    if (!this.canReviewTask(task)) {
      return {
        success: false,
        message: 'Chỉ người giao việc hoặc tài khoản có quyền đặc biệt mới được xác nhận hoàn thành.'
      };
    }

    return this.applyTaskAction(
      task,
      (current) => ({
        ...current,
        statusId: TASK_STATUS_IDS.DONG,
        progress: 100,
        processingNotes: note.trim() ? [`Xác nhận hoàn thành: ${note.trim()}`, ...(current.processingNotes ?? [])] : current.processingNotes
      }),
      'confirm_completion',
      getTaskStatusLabel(task.statusId),
      getTaskStatusLabel(TASK_STATUS_IDS.DONG)
    );
  }

  cancelTask(taskId: number, reason: string) {
    const task = this.getById(taskId);

    if (!task) {
      return this.notFoundResult();
    }

    const note = reason.trim() || 'Hủy công việc.';
    return this.applyTaskAction(
      task,
      (current) => ({
        ...current,
        statusId: TASK_STATUS_IDS.HUY,
        processingNotes: [`Hủy: ${note}`, ...(current.processingNotes ?? [])]
      }),
      'cancel_task',
      getTaskStatusLabel(task.statusId),
      note
    );
  }

  addTaskFeedback(taskId: number, body: string) {
    const task = this.getById(taskId);

    if (!task) {
      return this.notFoundResult();
    }

    const note = body.trim();
    if (!note) {
      return {
        success: false,
        message: 'Cần nhập nội dung phản hồi.'
      };
    }

    return this.applyTaskAction(
      task,
      (current) => ({
        ...current,
        processingNotes: [note, ...(current.processingNotes ?? [])]
      }),
      'comment_added',
      undefined,
      note
    );
  }

  duplicateTask(taskId: number) {
    const task = this.getById(taskId);

    if (!task) {
      return this.notFoundResult();
    }

    return this.cloneTask(task, {
      title: `${task.title} (bản sao)`,
      actionType: 'duplicate_task'
    });
  }

  createSimilarTask(taskId: number) {
    const task = this.getById(taskId);

    if (!task) {
      return this.notFoundResult();
    }

    return this.cloneTask(task, {
      title: `Tương tự - ${task.title}`,
      actionType: 'create_similar_task',
      resetPeople: true,
      resetAttachments: true
    });
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
              processingNotes: [...(updatedTask.processingNotes ?? [])],
              extensionRequests: [...(updatedTask.extensionRequests ?? [])],
              subtasks: this.cloneSubtasks(updatedTask.subtasks),
              subtaskProgressAutoSync: updatedTask.subtaskProgressAutoSync ?? true,
              parentCompletionSuggested: updatedTask.parentCompletionSuggested
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

  private applyTaskAction(
    task: Task,
    updater: (task: Task) => Task,
    actionType: string,
    oldValue?: string,
    newValue?: string
  ): TaskActionResult {
    if (!this.authService.canEditTask(task)) {
      return {
        success: false,
        message: 'Bạn không có quyền thao tác công việc của bộ phận khác.'
      };
    }

    const updatedTask = {
      ...updater({
        ...task,
        collaboratorIds: [...(task.collaboratorIds ?? [])],
        watcherIds: [...(task.watcherIds ?? [])],
        attachmentNames: [...(task.attachmentNames ?? [])],
        tags: [...(task.tags ?? [])],
        processingNotes: [...(task.processingNotes ?? [])],
        extensionRequests: [...(task.extensionRequests ?? [])],
        subtasks: this.cloneSubtasks(task.subtasks)
      }),
      updatedAt: new Date()
    };

    this.tasks.update((tasks) => tasks.map((item) => (item.id === task.id ? updatedTask : item)));
    this.recordActivity(task.id, actionType, oldValue, newValue);

    return {
      success: true,
      task: updatedTask
    };
  }

  private cloneTask(
    task: Task,
    options: {
      title: string;
      actionType: string;
      resetPeople?: boolean;
      resetAttachments?: boolean;
    }
  ): TaskActionResult {
    if (!this.authService.canEditTask(task)) {
      return {
        success: false,
        message: 'Bạn không có quyền sao chép công việc của bộ phận khác.'
      };
    }

    const nextId = this.tasks().length ? Math.max(...this.tasks().map((item) => item.id)) + 1 : 1;
    const now = new Date();
    const clonedTask: Task = {
      ...task,
      id: nextId,
      code: `CV-${String(nextId).padStart(4, '0')}`,
      title: options.title,
      statusId: TASK_STATUS_IDS.MOI_TAO,
      progress: 0,
      assigneeId: options.resetPeople ? undefined : task.assigneeId,
      collaboratorIds: options.resetPeople ? [] : [...(task.collaboratorIds ?? [])],
      watcherIds: [...(task.watcherIds ?? [])],
      attachmentNames: options.resetAttachments ? [] : [...(task.attachmentNames ?? [])],
      tags: [...(task.tags ?? [])],
      processingNotes: [],
      extensionRequests: [],
      subtasks: options.resetPeople ? [] : this.cloneSubtasksForTask(task.subtasks, nextId),
      subtaskProgressAutoSync: task.subtaskProgressAutoSync ?? true,
      parentCompletionSuggested: false,
      actualHours: 0,
      createdAt: now,
      updatedAt: now
    };

    this.tasks.update((tasks) => [clonedTask, ...tasks]);
    this.recordActivity(task.id, options.actionType, task.code, clonedTask.code);
    this.recordActivity(clonedTask.id, 'CREATE_TASK', undefined, clonedTask.title);

    return {
      success: true,
      task: clonedTask
    };
  }

  private withSubtaskRollup(task: Task): Task {
    const subtasks = task.subtasks ?? [];
    const allSubtasksDone = !!subtasks.length && subtasks.every((subtask) => subtask.done);
    const shouldSyncProgress = task.subtaskProgressAutoSync ?? true;
    const progress = shouldSyncProgress && subtasks.length ? this.calculateSubtaskProgress(subtasks) : task.progress;
    const suggestionNote = 'Tất cả nhiệm vụ con đã hoàn thành. Hệ thống gợi ý hoàn thành công việc hiện tại.';
    const shouldSuggestParentCompletion =
      allSubtasksDone &&
      task.statusId !== TASK_STATUS_IDS.HOAN_THANH &&
      task.statusId !== TASK_STATUS_IDS.DONG &&
      task.statusId !== TASK_STATUS_IDS.HUY;

    return {
      ...task,
      progress,
      parentCompletionSuggested: shouldSuggestParentCompletion,
      processingNotes:
        shouldSuggestParentCompletion && !(task.processingNotes ?? []).includes(suggestionNote)
          ? [suggestionNote, ...(task.processingNotes ?? [])]
          : task.processingNotes
    };
  }

  private getTaskSubtasks(task: Task) {
    return task.subtasks?.length ? this.cloneSubtasks(task.subtasks) : this.createDefaultSubtasks(task);
  }

  private createDefaultSubtasks(task: Task): SubTask[] {
    const baseTitles = [
      'Xác nhận phạm vi và đầu việc liên quan',
      'Cập nhật tiến độ và minh chứng xử lý',
      'Rà soát kết quả trước khi bàn giao'
    ];

    return baseTitles.map((title, index) => {
      const progress =
        index === 0
          ? task.progress >= 35
            ? 100
            : task.progress
          : index === 1
            ? Math.max(0, task.progress - 35)
            : Math.max(0, task.progress - 70);

      return {
        id: task.id * 100 + index + 1,
        taskId: task.id,
        title,
        assigneeId: task.assigneeId,
        dueDate: task.dueDate,
        progress: Math.min(100, progress),
        done: progress >= 100,
        createdAt: Date.now() - index * 3600000,
        order: index + 1
      };
    });
  }

  private calculateSubtaskProgress(subtasks: SubTask[]) {
    if (!subtasks.length) {
      return 0;
    }

    const total = subtasks.reduce((sum, subtask) => sum + this.normalizeProgress(subtask.progress), 0);
    return Math.round(total / subtasks.length);
  }

  private validateSubtaskDeadline(task: Task, dueDate?: Date): TaskActionResult {
    if (!task.dueDate || !dueDate || this.authService.hasSpecialTaskPermission()) {
      return { success: true };
    }

    const parentDueDate = new Date(task.dueDate);
    const subtaskDueDate = new Date(dueDate);
    const graceDays = 2;
    const latestAllowedDate = new Date(parentDueDate);
    latestAllowedDate.setDate(parentDueDate.getDate() + graceDays);

    if (subtaskDueDate.getTime() <= latestAllowedDate.getTime()) {
      return { success: true };
    }

    return {
      success: false,
      message:
        'Deadline nhiệm vụ con không được vượt quá công việc hiện tại quá 2 ngày nếu không có quyền đặc biệt.'
    };
  }

  private normalizeProgress(value: number) {
    return Math.min(100, Math.max(0, Math.round(Number(value) || 0)));
  }

  private cloneSubtasks(subtasks?: SubTask[]) {
    return (subtasks ?? []).map((subtask) => ({ ...subtask, dueDate: subtask.dueDate ? new Date(subtask.dueDate) : undefined }));
  }

  private cloneSubtasksForTask(subtasks: SubTask[] | undefined, taskId: number) {
    const now = Date.now();
    return (subtasks ?? []).map((subtask, index) => ({
      ...subtask,
      id: now + index,
      taskId,
      dueDate: subtask.dueDate ? new Date(subtask.dueDate) : undefined,
      createdAt: now,
      updatedAt: now
    }));
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
    const pushActivity = (actionType: string, oldValue?: string, newValue?: string) => {
      nextActivities.push({
        id: Date.now() + nextActivities.length,
        taskId: updatedTask.id,
        userId,
        actionType,
        oldValue,
        newValue,
        createdAt: now
      });
    };

    if (previousTask.assigneeId !== updatedTask.assigneeId) {
      pushActivity(
        'assignee_change',
        previousTask.assigneeId ? `User ${previousTask.assigneeId}` : 'Chưa chọn',
        updatedTask.assigneeId ? `User ${updatedTask.assigneeId}` : 'Chưa chọn'
      );
    }

    if (previousTask.statusId !== updatedTask.statusId) {
      pushActivity(
        'status_change',
        previousTask.statusId ? getTaskStatusLabel(previousTask.statusId) : 'Chưa xác định',
        updatedTask.statusId ? getTaskStatusLabel(updatedTask.statusId) : 'Chưa xác định'
      );
    }

    if (previousTask.progress !== updatedTask.progress) {
      pushActivity('progress_change', `${previousTask.progress}%`, `${updatedTask.progress}%`);
    }

    if (previousTask.priorityId !== updatedTask.priorityId) {
      pushActivity(
        'priority_change',
        previousTask.priorityId ? `Mức ${previousTask.priorityId}` : 'Chưa chọn',
        updatedTask.priorityId ? `Mức ${updatedTask.priorityId}` : 'Chưa chọn'
      );
    }

    if (this.getTime(previousTask.dueDate) !== this.getTime(updatedTask.dueDate)) {
      pushActivity(
        'deadline_change',
        previousTask.dueDate ? this.formatDate(previousTask.dueDate) : 'Chưa có hạn',
        updatedTask.dueDate ? this.formatDate(updatedTask.dueDate) : 'Chưa có hạn'
      );
    }

    if (this.getListSignature(previousTask.attachmentNames) !== this.getListSignature(updatedTask.attachmentNames)) {
      pushActivity(
        'attachment_change',
        `${previousTask.attachmentNames?.length ?? 0} file`,
        `${updatedTask.attachmentNames?.length ?? 0} file`
      );
    }

    if ((previousTask.processingNotes?.length ?? 0) !== (updatedTask.processingNotes?.length ?? 0)) {
      pushActivity(
        'processing_note_change',
        `${previousTask.processingNotes?.length ?? 0} ghi chú`,
        `${updatedTask.processingNotes?.length ?? 0} ghi chú`
      );
    }

    if (!nextActivities.length) {
      return;
    }

    this.activities.update((activities) => [...nextActivities, ...activities]);
  }

  private recordActivity(taskId: number, actionType: string, oldValue?: string, newValue?: string) {
    this.activities.update((activities) => [
      {
        id: Date.now() + Math.floor(Math.random() * 1000),
        taskId,
        userId: this.authService.user()?.id ?? 1,
        actionType,
        oldValue,
        newValue,
        createdAt: new Date()
      },
      ...activities
    ]);
  }

  private canReviewTask(task: Task) {
    const user = this.authService.user();
    return !!user && (this.authService.hasSpecialTaskPermission() || user.id === task.reporterId);
  }

  private notFoundResult(): TaskActionResult {
    return {
      success: false,
      message: 'Không tìm thấy công việc.'
    };
  }

  private formatDate(value: Date) {
    const date = new Date(value);
    const day = String(date.getDate()).padStart(2, '0');
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const year = date.getFullYear();

    return `${day}/${month}/${year}`;
  }

  private getTime(value?: Date) {
    return value ? new Date(value).getTime() : 0;
  }

  private getListSignature(value?: string[]) {
    return [...(value ?? [])].sort().join('|');
  }
}
