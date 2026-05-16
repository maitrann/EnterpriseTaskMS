import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import {
  CreateTaskInput,
  TaskFormOptions,
  TaskMemberOption
} from '../../../../core/models/task-form.model';
import {
  CustomSelectComponent,
  CustomSelectOption
} from '../../../../shared/ui/custom-select/custom-select.component';

type TaskDraft = {
  title: string;
  description: string;
  taskType: string;
  departmentId: number | null;
  assigneeId: number | null;
  collaboratorIds: number[];
  watcherIds: number[];
  startDate: string;
  dueDate: string;
  priorityId: number;
  urgencyLevel: string;
  securityLevel: string;
  estimatedHours: number | null;
  attachmentNames: string[];
  tags: string[];
  projectId: number | null;
  source: string;
};

@Component({
  selector: 'app-task-create-modal',
  standalone: true,
  imports: [CommonModule, FormsModule, CustomSelectComponent],
  templateUrl: './task-create-modal.component.html',
  styleUrl: './task-create-modal.component.scss'
})
export class TaskCreateModalComponent {
  @Input({ required: true }) taskCode!: string;
  @Input({ required: true }) formOptions!: TaskFormOptions;
  @Input() projectOptions: Array<{ value: number; label: string }> = [];

  @Output() close = new EventEmitter<void>();
  @Output() create = new EventEmitter<CreateTaskInput>();

  readonly draft = signal<TaskDraft>({
    title: '',
    description: '',
    taskType: '',
    departmentId: null,
    assigneeId: null,
    collaboratorIds: [],
    watcherIds: [],
    startDate: '',
    dueDate: '',
    priorityId: 2,
    urgencyLevel: '',
    securityLevel: '',
    estimatedHours: null,
    attachmentNames: [],
    tags: [],
    projectId: 1,
    source: ''
  });

  readonly noteFields = {
    attachment: signal(''),
    tag: signal('')
  };

  readonly validationMessage = computed(() => {
    const draft = this.draft();

    if (!draft.title.trim()) {
      return 'Tên công việc là trường bắt buộc.';
    }

    if (!draft.assigneeId) {
      return 'Cần chọn ít nhất 1 người xử lý chính.';
    }

    if (draft.startDate && draft.dueDate && draft.dueDate < draft.startDate) {
      return 'Deadline không được nhỏ hơn ngày bắt đầu.';
    }

    return '';
  });

  readonly filteredUsers = computed(() => {
    const departmentId = this.draft().departmentId;
    const users = this.formOptions?.users ?? [];

    if (!departmentId || this.isCrossDepartmentTask()) {
      return users;
    }

    return users.filter((user) => user.departmentId === departmentId);
  });

  readonly taskTypeOptions = computed<CustomSelectOption[]>(() => [
    { value: '', label: 'Chưa chọn' },
    ...(this.formOptions?.taskTypes ?? [])
  ]);
  readonly sourceOptions = computed<CustomSelectOption[]>(() => [
    { value: '', label: 'Chưa chọn' },
    ...(this.formOptions?.sources ?? [])
  ]);
  readonly priorityOptions = computed<CustomSelectOption[]>(() => this.formOptions?.priorities ?? []);
  readonly urgencyOptions = computed<CustomSelectOption[]>(() => [
    { value: '', label: 'Chưa chọn' },
    ...(this.formOptions?.urgencyLevels ?? [])
  ]);
  readonly securityOptions = computed<CustomSelectOption[]>(() => [
    { value: '', label: 'Chưa chọn' },
    ...(this.formOptions?.securityLevels ?? [])
  ]);

  readonly departmentSelectOptions = computed<CustomSelectOption[]>(() =>
    [
      { value: null, label: 'Chưa chọn phòng ban' },
      ...(this.formOptions?.departments ?? []).map((department) => ({
        value: department.id,
        label: department.label
      }))
    ]
  );

  readonly projectSelectOptions = computed<CustomSelectOption[]>(() =>
    [
      { value: null, label: 'Chưa chọn' },
      ...this.projectOptions.map((project) => ({
        value: project.value,
        label: project.label
      }))
    ]
  );

  readonly assigneeOptions = computed<CustomSelectOption[]>(() =>
    this.filteredUsers().map((user) => this.mapUserToOption(user))
  );

  readonly collaboratorOptions = computed<CustomSelectOption[]>(() =>
    this.filteredUsers().map((user) => this.mapUserToOption(user))
  );

  readonly watcherOptions = computed<CustomSelectOption[]>(() =>
    this.filteredUsers().map((user) => this.mapUserToOption(user))
  );

  updateField<K extends keyof TaskDraft>(field: K, value: TaskDraft[K]) {
    this.draft.update((draft) => ({ ...draft, [field]: value }));

    if (field === 'departmentId') {
      this.reconcileDepartmentUsers(value as number | null);
    }
  }

  addToken(field: 'attachmentNames' | 'tags', noteField: 'attachment' | 'tag') {
    const raw = this.noteFields[noteField]().trim();
    if (!raw) {
      return;
    }

    this.draft.update((draft) => {
      if (draft[field].includes(raw)) {
        return draft;
      }

      return { ...draft, [field]: [...draft[field], raw] };
    });

    this.noteFields[noteField].set('');
  }

  removeToken(field: 'attachmentNames' | 'tags', value: string) {
    this.draft.update((draft) => ({
      ...draft,
      [field]: draft[field].filter((item) => item !== value)
    }));
  }

  createTask() {
    if (this.validationMessage()) {
      return;
    }

    const draft = this.draft();

    this.create.emit({
      title: draft.title,
      description: draft.description,
      taskType: draft.taskType || undefined,
      departmentId: draft.departmentId ?? undefined,
      assigneeId: draft.assigneeId ?? undefined,
      collaboratorIds: draft.collaboratorIds,
      watcherIds: draft.watcherIds,
      startDate: this.fromDateInputValue(draft.startDate),
      dueDate: this.fromDateInputValue(draft.dueDate),
      priorityId: draft.priorityId,
      urgencyLevel: draft.urgencyLevel || undefined,
      securityLevel: draft.securityLevel || undefined,
      estimatedHours: draft.estimatedHours ?? undefined,
      attachmentNames: draft.attachmentNames,
      tags: draft.tags,
      projectId: draft.projectId ?? undefined,
      parentTaskId: undefined,
      source: draft.source || undefined
    });
  }

  updatePriority(value: string | number | null | Array<string | number>) {
    this.updateField('priorityId', typeof value === 'number' ? value : 2);
  }

  private reconcileDepartmentUsers(departmentId: number | null) {
    if (!departmentId) {
      return;
    }

    const validUserIds = this.filteredUsers().map((user) => user.id);

    this.draft.update((draft) => ({
      ...draft,
      assigneeId: validUserIds.includes(draft.assigneeId ?? -1) ? draft.assigneeId : null,
      collaboratorIds: draft.collaboratorIds.filter((id) => validUserIds.includes(id)),
      watcherIds: draft.watcherIds.filter((id) => validUserIds.includes(id))
    }));
  }

  private mapUserToOption(user: TaskMemberOption): CustomSelectOption<number> {
    return {
      value: user.id,
      label: user.label,
      description: user.role,
      groupLabel: this.getDepartmentLabel(user.departmentId)
    };
  }

  private isCrossDepartmentTask() {
    return this.draft().taskType === 'lien-phong';
  }

  private getDepartmentLabel(departmentId: number) {
    return this.formOptions?.departments.find((department) => department.id === departmentId)?.label ?? 'Khác';
  }

  private fromDateInputValue(value: string) {
    return value ? new Date(`${value}T00:00:00`) : undefined;
  }
}
