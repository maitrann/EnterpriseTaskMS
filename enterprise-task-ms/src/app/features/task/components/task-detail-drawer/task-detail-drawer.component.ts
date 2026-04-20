import { Component, Input, Output, EventEmitter, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Task } from '../../../../core/models/task.model';
import { SubTask } from '../../../../core/models/subtask.model';
import { TaskComment } from '../../../../core/models/task-comment.model';

import { TASK_ACTIVITY_MOCK } from '../../task-activity.mock';
import { TaskActivityTimelineComponent } from '../task-activity-timeline/task-activity-timeline.component';

@Component({
    selector: 'app-task-detail-drawer',
    standalone: true,
    imports: [CommonModule, FormsModule, TaskActivityTimelineComponent],
    templateUrl: './task-detail-drawer.component.html',
    styleUrl: './task-detail-drawer.component.scss'
})
export class TaskDetailDrawerComponent {

    @Input() task!: Task;

    @Output() close = new EventEmitter<void>();
    subtasks = signal<SubTask[]>([]);
    taskComments = signal<TaskComment[]>([]);

    newSubtaskTitle = signal('');
    editingSubtaskId = signal<number | null>(null);
    editingTitle = signal('');

    progress = computed(() => {

        const list = this.subtasks();

        if (!list.length) return 0;

        const done = list.filter(s => s.done).length;

        return Math.round((done / list.length) * 100);

    });

    taskActivities = computed(() =>
        TASK_ACTIVITY_MOCK.filter(a => a.taskId === this.task.id)
    );

    timelineItems = computed(() => {

        const activities = this.taskActivities().map(a => ({
            id: a.id,
            type: 'activity' as const,
            userId: a.userId,
            actionType: a.actionType,
            oldValue: a.oldValue,
            newValue: a.newValue,
            createdAt: a.createdAt
        }));

        const comments = this.taskComments().map(c => ({
            id: c.id,
            type: 'comment' as const,
            userId: c.userId,
            comment: c.content,
            createdAt: c.createdAt
        }));

        return [...activities, ...comments]
            .sort((a, b) =>
                new Date(b.createdAt).getTime() -
                new Date(a.createdAt).getTime()
            );

    });

    addSubtask() {

        const title = this.newSubtaskTitle().trim();
        if (!title) return;

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

        const updated = this.subtasks().map(s =>
            s.id === subtask.id ? { ...s, done: !s.done } : s
        );

        this.subtasks.set(updated);
    }

    deleteSubtask(id: number) {

        this.subtasks.set(
            this.subtasks().filter(s => s.id !== id)
        );

    }

    closeDrawer() {
        this.close.emit();
    }

    startEdit(sub: SubTask) {
        this.editingSubtaskId.set(sub.id);
        this.editingTitle.set(sub.title);
    }
    saveEdit(sub: SubTask) {

        const updated = this.subtasks().map(s =>
            s.id === sub.id
                ? { ...s, title: this.editingTitle() }
                : s
        );

        this.subtasks.set(updated);

        this.editingSubtaskId.set(null);
    }
    cancelEdit() {
        this.editingSubtaskId.set(null);
    }


}