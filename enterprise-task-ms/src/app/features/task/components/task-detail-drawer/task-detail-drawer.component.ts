import { CommonModule } from '@angular/common';
import { Component, computed, EventEmitter, Input, Output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { getTaskStatusLabel } from '../../../../core/constants/task-status.constants';
import { SubTask } from '../../../../core/models/subtask.model';
import { TaskComment } from '../../../../core/models/task-comment.model';
import { Task } from '../../../../core/models/task.model';
import { TaskService } from '../../../../core/services/task.service';
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

  @Output() close = new EventEmitter<void>();

  readonly subtasks = signal<SubTask[]>([]);
  readonly taskComments = signal<TaskComment[]>([]);
  readonly newSubtaskTitle = signal('');
  readonly editingSubtaskId = signal<number | null>(null);
  readonly editingTitle = signal('');

  readonly progress = computed(() => {
    const list = this.subtasks();
    if (!list.length) {
      return 0;
    }

    return Math.round((list.filter((subtask) => subtask.done).length / list.length) * 100);
  });

  readonly taskActivities = computed(() => this.taskService.getActivitiesByTaskId(this.task.id));

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

  closeDrawer() {
    this.close.emit();
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
  }
}
