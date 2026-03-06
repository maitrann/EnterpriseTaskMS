import { Component, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  CdkDragDrop,
  DragDropModule,
  moveItemInArray,
  transferArrayItem
} from '@angular/cdk/drag-drop';

import { Task } from '../../../../core/models/task.model';
import { TaskStatusEnum } from '../../../../core/models/task-detail-view.model';

import { TASK_MOCK } from '../../task.mock';

import { TaskCardComponent } from '../task-card/task-card.component';
import { TaskDetailDrawerComponent } from '../task-detail-drawer/task-detail-drawer.component';

@Component({
  selector: 'app-task-board',
  standalone: true,
  imports: [
    CommonModule,
    DragDropModule,
    TaskCardComponent,
    TaskDetailDrawerComponent
  ],
  templateUrl: './task-board.component.html',
  styleUrl: './task-board.component.scss'
})
export class TaskBoardComponent {

  tasks = signal<Task[]>(TASK_MOCK);

  selectedTask = signal<Task | null>(null);

  // SEARCH
  search = signal('');

  // FILTER
  filterPriority = signal<number | null>(null);
  filterAssignee = signal<number | null>(null);

  // STATUS LIST
  statuses = [
    { id: TaskStatusEnum.Todo, name: 'Todo' },
    { id: TaskStatusEnum.InProgress, name: 'In Progress' },
    { id: TaskStatusEnum.Review, name: 'Review' },
    { id: TaskStatusEnum.Done, name: 'Done' }
  ];

  // FILTERED TASKS
  filteredTasks = computed(() => {

    let list = this.tasks();

    if (this.search()) {
      list = list.filter(t =>
        t.title.toLowerCase().includes(this.search().toLowerCase())
      );
    }

    if (this.filterPriority()) {
      list = list.filter(t => t.priorityId === this.filterPriority());
    }

    if (this.filterAssignee()) {
      list = list.filter(t => t.assigneeId === this.filterAssignee());
    }

    return list;

  });

  // TASKS BY STATUS
  getTasks(statusId: number) {
    return this.filteredTasks().filter(t => t.statusId === statusId);
  }

  // OPEN TASK
  openTask(task: Task) {
    this.selectedTask.set(task);
  }

  closeDrawer() {
    this.selectedTask.set(null);
  }

  // DRAG DROP
  drop(event: CdkDragDrop<Task[]>, newStatus: number) {

    if (event.previousContainer === event.container) {

      moveItemInArray(
        event.container.data,
        event.previousIndex,
        event.currentIndex
      );

      return;
    }

    transferArrayItem(
      event.previousContainer.data,
      event.container.data,
      event.previousIndex,
      event.currentIndex
    );

    const movedTask = event.container.data[event.currentIndex];
    movedTask.statusId = newStatus;

    this.tasks.update(t => [...t]);
  }

}