import { CommonModule } from '@angular/common';
import { Component, computed, EventEmitter, Input, Output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import {
  canTransitionTaskStatus,
  TASK_STATUS_IDS,
  getTaskStatusLabel
} from '../../../../core/constants/task-status.constants';
import { EntityId } from '../../../../core/models/common-id.model';
import { SubTask } from '../../../../core/models/subtask.model';
import { TaskComment } from '../../../../core/models/task-comment.model';
import { TaskFormOptions } from '../../../../core/models/task-form.model';
import { Task, TaskExtensionRequest } from '../../../../core/models/task.model';
import { TaskActionResult, TaskService } from '../../../../core/services/task.service';
import {
  CustomSelectComponent,
  CustomSelectOption
} from '../../../../shared/ui/custom-select/custom-select.component';
import { TaskActivityTimelineComponent } from '../task-activity-timeline/task-activity-timeline.component';

@Component({
  selector: 'app-task-detail-drawer',
  standalone: true,
  imports: [CommonModule, FormsModule, CustomSelectComponent, TaskActivityTimelineComponent],
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
  readonly newSubtaskAssigneeId = signal<EntityId | null>(null);
  readonly newSubtaskDueDate = signal('');
  readonly newSubtaskProgress = signal(0);
  readonly editingSubtaskId = signal<EntityId | null>(null);
  readonly editingTitle = signal('');
  readonly actionMessage = signal('');
  readonly feedbackDraft = signal('');
  readonly rejectReason = signal('');
  readonly extensionDate = signal('');
  readonly extensionReason = signal('');
  readonly transferAssigneeId = signal<EntityId | null>(null);
  readonly transferReason = signal('');
  readonly completionNote = signal('');
  readonly cancelReason = signal('');
  readonly extensionReviewNote = signal('');

  readonly progress = computed(() => {
    const list = this.subtasks();
    if (!list.length) {
      return 0;
    }

    return Math.round(list.reduce((sum, subtask) => sum + subtask.progress, 0) / list.length);
  });

  readonly taskActivities = computed(() => this.taskService.getActivitiesByTaskId(this.task.id));
  readonly assigneeOptions = computed(() => this.formOptions?.users ?? []);
  readonly assigneeSelectOptions = computed<CustomSelectOption<EntityId | null>[]>(() => [
    { value: null, label: 'Chưa chọn' },
    ...(this.formOptions?.users ?? []).map((user) => ({
      value: user.id,
      label: user.label,
      description: user.role
    }))
  ]);
  readonly extensionRequests = computed(() => this.task.extensionRequests ?? []);
  readonly pendingExtensionRequest = computed(
    () => this.extensionRequests().find((request) => request.status === 'pending') ?? null
  );
  readonly parentCompletionSuggested = computed(
    () => !!this.subtasks().length && this.subtasks().every((subtask) => subtask.done) && this.task.progress === 100
  );

  readonly overviewItems = computed(() => [
    { label: 'Mã công việc', value: this.task.code || `#${this.task.id}` },
    { label: 'Người phụ trách', value: this.resolveUserLabel(this.task.assigneeId) },
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
        return 'Chưa chọn';
    }
  }

  get statusLabel() {
    return getTaskStatusLabel(this.task.statusId);
  }

  get dueStatus(): 'normal' | 'warning' | 'overdue' {
    if (this.task.statusId === TASK_STATUS_IDS.QUA_HAN) {
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

    const result = this.taskService.createSubtask(this.task.id, {
      title,
      assigneeId: this.newSubtaskAssigneeId() ?? undefined,
      dueDate: this.fromDateInputValue(this.newSubtaskDueDate()),
      progress: this.newSubtaskProgress()
    });

    this.applySubtaskResult(result, 'Đã thêm nhiệm vụ con.');

    if (result.success) {
      this.newSubtaskTitle.set('');
      this.newSubtaskAssigneeId.set(null);
      this.newSubtaskDueDate.set('');
      this.newSubtaskProgress.set(0);
    }
  }

  toggleSubtask(subtask: SubTask) {
    this.applySubtaskResult(this.taskService.toggleSubtaskDone(this.task.id, subtask.id), 'Đã cập nhật nhiệm vụ con.');
  }

  deleteSubtask(id: EntityId) {
    this.applySubtaskResult(this.taskService.deleteSubtask(this.task.id, id), 'Đã xóa nhiệm vụ con.');
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
    return canTransitionTaskStatus(this.task.statusId, TASK_STATUS_IDS.HOAN_THANH);
  }

  canConfirmCompletion() {
    return this.task.statusId === TASK_STATUS_IDS.HOAN_THANH;
  }

  canCancel() {
    return canTransitionTaskStatus(this.task.statusId, TASK_STATUS_IDS.HUY);
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

    this.applySubtaskResult(
      this.taskService.updateSubtask(this.task.id, subtask.id, { title: nextTitle }),
      'Đã cập nhật nhiệm vụ con.'
    );

    this.editingSubtaskId.set(null);
  }

  updateSubtaskProgress(subtask: SubTask, value: string | number) {
    this.applySubtaskResult(
      this.taskService.updateSubtask(this.task.id, subtask.id, { progress: Number(value) }),
      'Đã cập nhật tiến độ nhiệm vụ con.'
    );
  }

  updateSubtaskAssignee(subtask: SubTask, value: EntityId | null) {
    this.applySubtaskResult(
      this.taskService.updateSubtask(this.task.id, subtask.id, { assigneeId: value ?? undefined }),
      'Đã cập nhật người xử lý nhiệm vụ con.'
    );
  }

  updateSubtaskDueDate(subtask: SubTask, value: string) {
    this.applySubtaskResult(
      this.taskService.updateSubtask(this.task.id, subtask.id, { dueDate: this.fromDateInputValue(value) }),
      'Đã cập nhật deadline nhiệm vụ con.'
    );
  }

  toInputDate(value?: Date) {
    return this.toDateInputValue(value);
  }

  cancelEdit() {
    this.editingSubtaskId.set(null);
  }

  private seedMockDetails(task: Task) {
    this.subtasks.set(this.taskService.getSubtasksByTaskId(task.id));
    this.taskComments.set([]);
    this.newSubtaskTitle.set('');
    this.newSubtaskAssigneeId.set(task.assigneeId ?? null);
    this.newSubtaskDueDate.set(this.toDateInputValue(task.dueDate));
    this.newSubtaskProgress.set(0);
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

  private applySubtaskResult(result: TaskActionResult, successMessage: string) {
    this.applyActionResult(result, successMessage);

    if (result.success && result.task) {
      this.subtasks.set(result.task.subtasks ?? []);
    }
  }

  private applyCreateResult(result: TaskActionResult, successMessage: string) {
    this.actionMessage.set(result.success ? successMessage : result.message ?? 'Không thể tạo công việc.');

    if (result.success && result.task) {
      this.taskCreated.emit(result.task);
    }
  }

  private resolveUserLabel(userId?: EntityId) {
    if (!userId) {
      return 'Chưa chọn';
    }

    return this.formOptions?.users.find((user) => user.id === userId)?.label ?? String(userId).slice(0, 8);
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

  private fromDateInputValue(value: string) {
    return value ? new Date(`${value}T00:00:00`) : undefined;
  }
}
