import { Component, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
    CdkDragDrop,
    DragDropModule,
    moveItemInArray,
    transferArrayItem
} from '@angular/cdk/drag-drop';

import { Task } from '../../../../core/models/task.model';
import { TASK_MOCK } from '../../task.mock';

import { TaskCardComponent } from '../task-card/task-card.component';
import { TaskDetailDrawerComponent } from '../task-detail-drawer/task-detail-drawer.component';
import { TaskEditModalComponent } from '../task-edit-modal/task-edit-modal.component';

@Component({
    selector: 'app-task-board',
    standalone: true,
    imports: [
        CommonModule,
        DragDropModule,
        TaskCardComponent,
        TaskDetailDrawerComponent,
        TaskEditModalComponent
    ],
    templateUrl: './task-board.component.html',
    styleUrl: './task-board.component.scss'
})
export class TaskBoardComponent {

    tasks = signal<Task[]>(TASK_MOCK);

    selectedTask = signal<Task | null>(null); // drawer
    editingTask = signal<Task | null>(null); // modal

    search = signal('');
    filterPriority = signal<number | null>(null);

    statuses = [
        { id: 1, name: 'Todo' },
        { id: 2, name: 'In Progress' },
        { id: 3, name: 'Review' },
        { id: 4, name: 'Done' }
    ];

    filteredTasks = computed(() => {

        const keyword = this.search().toLowerCase().trim();
        const priority = this.filterPriority();

        return this.tasks().filter(task => {

            const matchSearch =
                !keyword ||
                task.title.toLowerCase().includes(keyword) ||
                (task.description ?? '').toLowerCase().includes(keyword);

            const matchPriority =
                !priority || task.priorityId === priority;

            return matchSearch && matchPriority;

        });

    });
    onPriorityChange(event: Event) {
        const value = (event.target as HTMLSelectElement).value;
        this.filterPriority.set(value ? +value : null);
    }
    getTasks(statusId: number) {
        return this.filteredTasks().filter(t => t.statusId === statusId);
    }

    // open drawer
    openTask(task: Task) {
        this.selectedTask.set(task);
    }

    closeDrawer() {
        this.selectedTask.set(null);
    }

    // open edit modal
    openEdit(task: Task) {
        this.editingTask.set({ ...task }); // clone
    }

    closeEditModal() {
        this.editingTask.set(null);
    }

    // SAVE TASK
    saveTask(updated: Task) {

        const updatedTasks = this.tasks().map(t =>
            t.id === updated.id ? updated : t
        );

        this.tasks.set(updatedTasks);

        this.editingTask.set(null);
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