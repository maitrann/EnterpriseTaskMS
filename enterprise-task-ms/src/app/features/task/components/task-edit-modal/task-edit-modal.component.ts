import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import {
  canTransitionTaskStatus,
  getAllowedNextStatusIds,
  getAllowedStatusOptions,
  getTaskStatusLabel,
  TASK_STATUS_OPTIONS
} from '../../../../core/constants/task-status.constants';
import { EntityId } from '../../../../core/models/common-id.model';
import { TaskFormOptions, TaskMemberOption } from '../../../../core/models/task-form.model';
import { Task } from '../../../../core/models/task.model';
import {
  CustomSelectComponent,
  CustomSelectOption
} from '../../../../shared/ui/custom-select/custom-select.component';

@Component({
  selector: 'app-task-edit-modal',
  standalone: true,
  imports: [CommonModule, FormsModule, CustomSelectComponent],
  templateUrl: './task-edit-modal.component.html',
  styleUrl: './task-edit-modal.component.scss'
})
export class TaskEditModalComponent {
  private _task!: Task;

  @Input({ required: true }) formOptions!: TaskFormOptions;
  @Input() projectOptions: Array<{ value: EntityId; label: string }> = [];
  @Input() canManageClosedTasks = false;

  @Input({ required: true }) set task(value: Task) {
    this._task = {
      ...value,
      collaboratorIds: [...(value.collaboratorIds ?? [])],
      watcherIds: [...(value.watcherIds ?? [])],
      attachmentNames: [...(value.attachmentNames ?? [])],
      tags: [...(value.tags ?? [])],
      processingNotes: [...(value.processingNotes ?? [])]
    };
    this.originalStatusId.set(value.statusId);
    this.attachmentDraft.set('');
    this.tagDraft.set('');
    this.processingNoteDraft.set('');
  }

  get task() {
    return this._task;
  }

  @Output() save = new EventEmitter<Task>();
  @Output() close = new EventEmitter<void>();

  readonly priorities: CustomSelectOption<number>[] = [
    { value: 1, label: 'Thấp' },
    { value: 2, label: 'Trung bình' },
    { value: 3, label: 'Cao' },
    { value: 4, label: 'Khẩn cấp' }
  ];

  readonly statuses = TASK_STATUS_OPTIONS;
  readonly originalStatusId = signal<number | undefined>(undefined);

  readonly attachmentDraft = signal('');
  readonly tagDraft = signal('');
  readonly processingNoteDraft = signal('');

  readonly taskTypeOptions = computed<CustomSelectOption[]>(() => [
    { value: '', label: 'Chưa chọn' },
    ...(this.formOptions?.taskTypes ?? [])
  ]);

  readonly sourceOptions = computed<CustomSelectOption[]>(() => [
    { value: '', label: 'Chưa chọn' },
    ...(this.formOptions?.sources ?? [])
  ]);

  readonly urgencyOptions = computed<CustomSelectOption[]>(() => [
    { value: '', label: 'Chưa chọn' },
    ...(this.formOptions?.urgencyLevels ?? [])
  ]);

  readonly securityOptions = computed<CustomSelectOption[]>(() => [
    { value: '', label: 'Chưa chọn' },
    ...(this.formOptions?.securityLevels ?? [])
  ]);

  readonly departmentOptions = computed<CustomSelectOption[]>(() => [
    { value: null, label: 'Chưa chọn phòng ban' },
    ...(this.formOptions?.departments ?? []).map((department) => ({
      value: department.id,
      label: department.label
    }))
  ]);

  readonly projectSelectOptions = computed<CustomSelectOption[]>(() => [
    { value: null, label: 'Chưa chọn' },
    ...this.projectOptions.map((project) => ({
      value: project.value,
      label: project.label
    }))
  ]);

  readonly filteredUsers = computed(() => {
    const users = this.formOptions?.users ?? [];
    const departmentId = this.task?.departmentId;

    if (!departmentId || this.isCrossDepartmentTask()) {
      return users;
    }

    return users.filter((user) => user.departmentId === departmentId);
  });

  readonly assigneeOptions = computed<CustomSelectOption[]>(() =>
    this.filteredUsers().map((user) => this.mapUserToOption(user))
  );

  readonly collaboratorOptions = computed<CustomSelectOption[]>(() =>
    this.filteredUsers().map((user) => this.mapUserToOption(user))
  );

  readonly watcherOptions = computed<CustomSelectOption[]>(() =>
    this.filteredUsers().map((user) => this.mapUserToOption(user))
  );

  readonly availableStatuses = computed(() => {
    const currentStatus = this.originalStatusId();
    const currentOption = this.statuses.find((status) => status.value === currentStatus);
    const nextOptions = getAllowedStatusOptions(currentStatus, this.canManageClosedTasks);

    return currentOption ? [currentOption, ...nextOptions] : nextOptions;
  });

  readonly transitionHint = computed(() => {
    const nextStatusIds = getAllowedNextStatusIds(this.originalStatusId(), this.canManageClosedTasks);
    if (!nextStatusIds.length) {
      return 'Trạng thái hiện tại đã ở điểm kết thúc, không còn bước chuyển tiếp tiếp theo.';
    }

    return `Có thể chuyển sang: ${nextStatusIds.map((statusId) => getTaskStatusLabel(statusId)).join(' / ')}.`;
  });

  readonly validationMessage = computed(() => {
    if (!this.task.title?.trim()) {
      return 'Tên công việc không được để trống.';
    }

    if (!this.task.assigneeId) {
      return 'Cần chọn ít nhất 1 người xử lý chính.';
    }

    if (this.startDateValue && this.dueDateValue && this.dueDateValue < this.startDateValue) {
      return 'Deadline không được nhỏ hơn ngày bắt đầu.';
    }

    if (
      this.task.statusId &&
      this.originalStatusId() &&
      this.task.statusId !== this.originalStatusId() &&
      !canTransitionTaskStatus(this.originalStatusId(), this.task.statusId, this.canManageClosedTasks)
    ) {
      return `Không thể chuyển từ "${getTaskStatusLabel(this.originalStatusId())}" sang "${getTaskStatusLabel(this.task.statusId)}".`;
    }

    return '';
  });

  get startDateValue() {
    return this.toDateInputValue(this.task.startDate);
  }

  set startDateValue(value: string) {
    this.task.startDate = this.fromDateInputValue(value);
  }

  get dueDateValue() {
    return this.toDateInputValue(this.task.dueDate);
  }

  set dueDateValue(value: string) {
    this.task.dueDate = this.fromDateInputValue(value);
  }

  get priorityLabel() {
    return this.priorities.find((priority) => priority.value === this.task.priorityId)?.label ?? 'Chưa chọn';
  }

  get statusLabel() {
    return getTaskStatusLabel(this.task.statusId);
  }

  get assigneeLabel() {
    return this.resolveUserName(this.task.assigneeId) ?? 'Chưa chọn';
  }

  addAttachment() {
    const value = this.attachmentDraft().trim();
    if (!value) {
      return;
    }

    const attachments = this.task.attachmentNames ?? [];
    if (!attachments.includes(value)) {
      this.task.attachmentNames = [...attachments, value];
    }

    this.attachmentDraft.set('');
  }

  removeAttachment(name: string) {
    this.task.attachmentNames = (this.task.attachmentNames ?? []).filter((item) => item !== name);
  }

  addTag() {
    const value = this.tagDraft().trim();
    if (!value) {
      return;
    }

    const tags = this.task.tags ?? [];
    if (!tags.includes(value)) {
      this.task.tags = [...tags, value];
    }

    this.tagDraft.set('');
  }

  removeTag(tag: string) {
    this.task.tags = (this.task.tags ?? []).filter((item) => item !== tag);
  }

  addProcessingNote() {
    const value = this.processingNoteDraft().trim();
    if (!value) {
      return;
    }

    this.task.processingNotes = [value, ...(this.task.processingNotes ?? [])];
    this.processingNoteDraft.set('');
  }

  removeProcessingNote(note: string) {
    this.task.processingNotes = (this.task.processingNotes ?? []).filter((item) => item !== note);
  }

  updateStatus(value: string | number | null | Array<string | number>) {
    if (typeof value !== 'number') {
      return;
    }

    if (value === this.originalStatusId()) {
      this.task.statusId = value;
      return;
    }

    if (canTransitionTaskStatus(this.originalStatusId(), value, this.canManageClosedTasks)) {
      this.task.statusId = value;
    }
  }

  updatePriority(value: string | number | null | Array<string | number>) {
    this.task.priorityId = typeof value === 'number' ? value : this.task.priorityId;
  }

  updateDepartment(value: string | number | null | Array<string | number>) {
    this.task.departmentId = typeof value === 'number' ? value : undefined;

    if (!this.isCrossDepartmentTask()) {
      const validUserIds = this.filteredUsers().map((user) => user.id);
      this.task.assigneeId = validUserIds.includes(this.task.assigneeId ?? -1) ? this.task.assigneeId : undefined;
      this.task.collaboratorIds = (this.task.collaboratorIds ?? []).filter((id) => validUserIds.includes(id));
      this.task.watcherIds = (this.task.watcherIds ?? []).filter((id) => validUserIds.includes(id));
    }
  }

  saveTask() {
    if (this.validationMessage()) {
      return;
    }

    this.addAttachment();
    this.addTag();
    this.addProcessingNote();
    this.task.updatedAt = new Date();
    this.save.emit(this.task);
  }

  private resolveUserName(userId?: EntityId) {
    return this.formOptions?.users.find((user) => user.id === userId)?.label;
  }

  private mapUserToOption(user: TaskMemberOption): CustomSelectOption<EntityId> {
    return {
      value: user.id,
      label: user.label,
      description: user.role,
      groupLabel: this.getDepartmentLabel(user.departmentId)
    };
  }

  private isCrossDepartmentTask() {
    return this.task.taskType === 'lien-phong' || this.task.taskType === 'Liên phòng';
  }

  private getDepartmentLabel(departmentId: number) {
    return this.formOptions?.departments.find((department) => department.id === departmentId)?.label ?? 'Khác';
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
