import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Task } from '../../../../core/models/task.model';

@Component({
  selector: 'app-task-detail-drawer',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './task-detail-drawer.component.html',
  styleUrl: './task-detail-drawer.component.scss'
})
export class TaskDetailDrawerComponent {

  @Input() task!: Task | null;

  @Output() close = new EventEmitter<void>();

  closeDrawer() {
    this.close.emit();
  }

}