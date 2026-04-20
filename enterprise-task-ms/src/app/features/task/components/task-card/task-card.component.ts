import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Task } from '../../../../core/models/task.model';

@Component({
    selector: 'app-task-card',
    standalone: true,
    imports: [CommonModule],
    templateUrl: './task-card.component.html',
    styleUrl: './task-card.component.scss'
})
export class TaskCardComponent {

    @Input() task!: Task;

    @Output() open = new EventEmitter<Task>();
    @Output() edit = new EventEmitter<void>();
    openDetail() {
        this.open.emit(this.task);
    }
    getPriorityLabel(priorityId?: number) {

        switch (priorityId) {

            case 1: return 'Low';
            case 2: return 'Medium';
            case 3: return 'High';
            case 4: return 'Critical';

            default: return 'None';
        }
    }
    getDueStatus(): 'normal' | 'warning' | 'overdue' {

        if (!this.task.dueDate) return 'normal';

        const today = new Date();
        const due = new Date(this.task.dueDate);

        const diff =
            (due.getTime() - today.getTime()) / (1000 * 60 * 60 * 24);

        if (diff < 0) return 'overdue';

        if (diff <= 2) return 'warning';

        return 'normal';
    }
}