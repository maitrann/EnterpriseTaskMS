import { CommonModule } from '@angular/common';
import { Component, computed, EventEmitter, Input, Output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import {
  TASK_STATUS_IDS,
  getTaskStatusLabel
} from '../../../../core/constants/task-status.constants';
import { SubTask } from '../../../../core/models/subtask.model';
import { TaskComment } from '../../../../core/models/task-comment.model';
import { TaskFormOptions } from '../../../../core/models/task-form.model';
import { Task, TaskExtensionRequest } from '../../../../core/models/task.model';
import { TaskActionResult, TaskService } from '../../../../core/services/task.service';
import { TaskActivityTimelineComponent } from '../task-activity-timeline/task-activity-timeline.component';

@Component({
  selector: 'app-task-detail-drawer',
  standalone: true,
  imports: [CommonModule, FormsModule, TaskActivityTimelineComponent],
  templateUrl: './task-detail-drawer.component.html',
  styleUrl: './task-detail-drawer.component.scss'
})
export class TaskDetailDrawerComponent {
  constructor(private readonly taskService: TaskService) {}

  private _task!: Task;

  @Input() set task(value: Task) {
    this._task = value;
    this.seedMockDetails(value);
  }

  get task() {
    return this._task;
  }

  @Input({ required: true }) formOptions!: TaskFormOptions;

  @Output() close = new EventEmitter<void>();
  @Output() taskChanged = new EventEmitter<Task>();
  @Output() taskCreated = new EventEmitter<Task>();

  readonly subtasks = signal<SubTask[]>([]);
  readonly taskComments = signal<TaskComment[]>([]);
  readonly newSubtaskTitle = signal('');
  readonly editingSubtaskId = signal<number | null>(null);
  readonly editingTitle = signal('');
  readonly actionMessage = signal('');
  readonly feedbackDraft = signal('');
  readonly rejectReason = signal('');
  readonly extensionDate = signal('');
  readonly extensionReason = signal('');
  readonly transferAssigneeId = signal<number | null>(null);
  readonly transferReason = signal('');
  readonly completionNote = signal('');
  readonly cancelReason = signal('');
  readonly extensionReviewNote = signal('');

  readonly progress = computed(() => {
    const list = this.subtasks();
    if (!list.length) {
      return 0;
    }

    return Math.round((list.filter((subtask) => subtask.done).length / list.length) * 100);
  });

  readonly taskActivities = computed(() => this.taskService.getActivitiesByTaskId(this.task.id));
  readonly assigneeOptions = computed(() => this.formOptions?.users ?? []);
  readonly extensionRequests = computed(() => this.task.extensionRequests ?? []);
  readonly pendingExtensionRequest = computed(
    () => this.extensionRequests().find((request) => request.status === 'pending') ?? null
  );

  readonly overviewItems = computed(() => [
    { label: 'Mã công việc', value: this.task.code || `#${this.task.id}` },
    { label: 'Người phụ trách', value: `NV-${String(this.task.assigneeId ?? 0).padStart(2, '0')}` },
    { label: 'Dự kiến', value: `${this.task.estimatedHours ?? 0}h` },
    { label: 'Thực tế', value: `${this.task.actualHours ?? 0}h` }
  ]);

  get priorityLabel() {
    switch (this.task.priorityId) {
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

  get statusLabel() {
    return getTaskStatusLabel(this.task.statusId);
  }

  get dueStatus(): 'normal' | 'warning' | 'overdue' {
    if (this.task.statusId === 10) {
      return 'overdue';
    }

    if (!this.task.dueDate) {
      return 'normal';
    }

    const today = new Date();
    const due = new Date(this.task.dueDate);
    const diff = (due.getTime() - today.getTime()) / (1000 * 60 * 60 * 24);

    if (diff < 0) {
      return 'overdue';
    }

    if (diff <= 2) {
      return 'warning';
    }

    return 'normal';
  }

  get dueLabel() {
    switch (this.dueStatus) {
      case 'overdue':
        return 'Trễ hạn';
      case 'warning':
        return 'Sắp đến hạn';
      default:
        return 'Đúng tiến độ';
    }
  }

  addSubtask() {
    const title = this.newSubtaskTitle().trim();
    if (!title) {
      return;
    }

    const list = this.subtasks();
    const newSubtask: SubTask = {
      id: Date.now(),
      taskId: this.task.id,
      title,
      done: false,
      createdAt: Date.now(),
      order: list.length + 1
    };

    this.subtasks.set([...list, newSubtask]);
    this.newSubtaskTitle.set('');
  }

  toggleSubtask(subtask: SubTask) {
    this.subtasks.set(
      this.subtasks().map((item) => (item.id === subtask.id ? { ...item, done: !item.done } : item))
    );
  }

  deleteSubtask(id: number) {
    this.subtasks.set(this.subtasks().filter((item) => item.id !== id));
  }

  acceptTask() {
    this.applyActionResult(this.taskService.acceptTask(this.task.id), 'Đã nhận việc.');
  }

  rejectAcceptance() {
    this.applyActionResult(
      this.taskService.rejectAcceptance(this.task.id, this.rejectReason()),
      'Đã từ chối tiếp nhận.'
    );
    this.rejectReason.set('');
  }

  sendFeedback() {
    this.applyActionResult(
      this.taskService.addTaskFeedback(this.task.id, this.feedbackDraft()),
      'Đã gửi phản hồi.'
    );
    this.feedbackDraft.set('');
  }

  requestExtension() {
    if (!this.extensionDate()) {
      this.actionMessage.set('Cần chọn hạn xử lý mới.');
      return;
    }

    this.applyActionResult(
      this.taskService.requestExtension(
        this.task.id,
        new Date(`${this.extensionDate()}T00:00:00`),
        this.extensionReason()
      ),
      'Đã gửi yêu cầu gia hạn.'
    );
    this.extensionReason.set('');
  }

  approveExtensionRequest(request: TaskExtensionRequest) {
    this.applyActionResult(
      this.taskService.approveExtensionRequest(this.task.id, request.id, this.extensionReviewNote()),
      'Đã duyệt yêu cầu gia hạn.'
    );
    this.extensionReviewNote.set('');
  }

  rejectExtensionRequest(request: TaskExtensionRequest) {
    this.applyActionResult(
      this.taskService.rejectExtensionRequest(this.task.id, request.id, this.extensionReviewNote()),
      'Đã từ chối yêu cầu gia hạn.'
    );
    this.extensionReviewNote.set('');
  }

  transferAssignee() {
    if (!this.transferAssigneeId()) {
      this.actionMessage.set('Cần chọn người xử lý mới.');
      return;
    }

    this.applyActionResult(
      this.taskService.transferAssignee(this.task.id, this.transferAssigneeId()!, this.transferReason()),
      'Đã chuyển người xử lý.'
    );
    this.transferReason.set('');
  }

  completeTask() {
    this.applyActionResult(
      this.taskService.completeTask(this.task.id, this.completionNote()),
      'Đã đánh dấu hoàn thành.'
    );
    this.completionNote.set('');
  }

  confirmCompletion() {
    this.applyActionResult(
      this.taskService.confirmCompletion(this.task.id, this.completionNote()),
      'Đã xác nhận hoàn thành.'
    );
    this.completionNote.set('');
  }

  cancelTask() {
    this.applyActionResult(this.taskService.cancelTask(this.task.id, this.cancelReason()), 'Đã hủy công việc.');
    this.cancelReason.set('');
  }

  duplicateTask() {
    this.applyCreateResult(this.taskService.duplicateTask(this.task.id), 'Đã sao chép công việc.');
  }

  createSimilarTask() {
    this.applyCreateResult(
      this.taskService.createSimilarTask(this.task.id),
      'Đã tạo công việc tương tự.'
    );
  }

  closeDrawer() {
    this.close.emit();
  }

  canAccept() {
    return this.task.statusId === TASK_STATUS_IDS.CHO_TIEP_NHAN;
  }

  canRejectAcceptance() {
    return this.task.statusId === TASK_STATUS_IDS.CHO_TIEP_NHAN;
  }

  canComplete() {
    const completedStatusIds: number[] = [
      TASK_STATUS_IDS.HOAN_THANH,
      TASK_STATUS_IDS.DONG,
      TASK_STATUS_IDS.HUY
    ];

    return !completedStatusIds.includes(this.task.statusId ?? -1);
  }

  canConfirmCompletion() {
    return this.task.statusId === TASK_STATUS_IDS.HOAN_THANH;
  }

  canCancel() {
    const terminalStatusIds: number[] = [TASK_STATUS_IDS.DONG, TASK_STATUS_IDS.HUY];

    return !terminalStatusIds.includes(this.task.statusId ?? -1);
  }

  startEdit(subtask: SubTask) {
    this.editingSubtaskId.set(subtask.id);
    this.editingTitle.set(subtask.title);
  }

  saveEdit(subtask: SubTask) {
    const nextTitle = this.editingTitle().trim();
    if (!nextTitle) {
      this.cancelEdit();
      return;
    }

    this.subtasks.set(
      this.subtasks().map((item) => (item.id === subtask.id ? { ...item, title: nextTitle } : item))
    );

    this.editingSubtaskId.set(null);
  }

  cancelEdit() {
    this.editingSubtaskId.set(null);
  }

  private seedMockDetails(task: Task) {
    const baseTitles = [
      'Xác nhận phạm vi và đầu việc liên quan',
      'Cập nhật tiến độ và minh chứng xử lý',
      'Rà soát kết quả trước khi bàn giao'
    ];

    const seededSubtasks = baseTitles.map((title, index) => ({
      id: task.id * 100 + index + 1,
      taskId: task.id,
      title,
      done: index === 0 ? task.progress >= 35 : index === 1 ? task.progress >= 70 : task.progress >= 95,
      createdAt: Date.now() - index * 3600000,
      order: index + 1
    }));

    this.subtasks.set(seededSubtasks);
    this.taskComments.set([]);
    this.newSubtaskTitle.set('');
    this.editingSubtaskId.set(null);
    this.editingTitle.set('');
    this.actionMessage.set('');
    this.feedbackDraft.set('');
    this.rejectReason.set('');
    this.extensionDate.set(this.toDateInputValue(task.dueDate));
    this.extensionReason.set('');
    this.transferAssigneeId.set(task.assigneeId ?? null);
    this.transferReason.set('');
    this.completionNote.set('');
    this.cancelReason.set('');
    this.extensionReviewNote.set('');
  }

  private applyActionResult(result: TaskActionResult, successMessage: string) {
    this.actionMessage.set(result.success ? successMessage : result.message ?? 'Không thể thực hiện thao tác.');

    if (result.success && result.task) {
      this._task = result.task;
      this.taskChanged.emit(result.task);
    }
  }

  private applyCreateResult(result: TaskActionResult, successMessage: string) {
    this.actionMessage.set(result.success ? successMessage : result.message ?? 'Không thể tạo công việc.');

    if (result.success && result.task) {
      this.taskCreated.emit(result.task);
    }
  }

  private toDateInputValue(value?: Date) {
    if (!value) {
      return '';
    }

    const date = new Date(value);
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');

    return `${year}-${month}-${day}`;
  }
}
