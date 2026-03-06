import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { Task } from '../../../../core/models/task.model';
import { TaskPriorityEnum } from '../../../../core/models/task-detail-view.model';

@Component({
  selector: 'app-task-edit-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './task-edit-modal.component.html',
  styleUrl: './task-edit-modal.component.scss'
})
export class TaskEditModalComponent {

  @Input() task!: Task;

  @Output() save = new EventEmitter<Task>();
  @Output() close = new EventEmitter<void>();

  priorities = TaskPriorityEnum;

  saveTask() {
    this.task.updatedAt = new Date();
    this.save.emit(this.task);
  }

}