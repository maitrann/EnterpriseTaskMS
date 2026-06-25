import { CommonModule, DatePipe } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';

import { TASK_STATUS_IDS } from '../../../../core/constants/task-status.constants';
import { Task } from '../../../../core/models/task.model';

@Component({
  selector: 'app-task-card',
  standalone: true,
  imports: [CommonModule, DatePipe],
  templateUrl: './task-card.component.html',
  styleUrl: './task-card.component.scss'
})
export class TaskCardComponent {
  @Input() task!: Task;
  @Input() canEdit = true;

  @Output() open = new EventEmitter<Task>();
  @Output() edit = new EventEmitter<void>();

  openDetail() {
    this.open.emit(this.task);
  }

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

  get dueStatus(): 'normal' | 'warning' | 'overdue' {
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

  get isOverdueHighlight() {
    return this.task.statusId === TASK_STATUS_IDS.QUA_HAN || this.dueStatus === 'overdue';
  }

  get assigneeLabel() {
    return `NV-${String(this.task.assigneeId ?? 0).padStart(2, '0')}`;
  }

  get estimatedLabel() {
    return `${this.task.estimatedHours ?? 0}h`;
  }

  get actualLabel() {
    return `${this.task.actualHours ?? 0}h`;
  }
}
