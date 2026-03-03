import { Component, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TaskService } from '../../core/services/task.service';

@Component({
  standalone: true,
  selector: 'app-dashboard',
  imports: [CommonModule],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss']
})
export class DashboardComponent {

  private taskService = inject(TaskService);

  tasks = computed(() => this.taskService.tasks());

  totalTasks = computed(() => this.tasks().length);

  todoCount = computed(() =>
    this.tasks().filter(t => t.status === 'todo').length
  );

  inProgressCount = computed(() =>
    this.tasks().filter(t => t.status === 'inprogress').length
  );

  doneCount = computed(() =>
    this.tasks().filter(t => t.status === 'done').length
  );
}