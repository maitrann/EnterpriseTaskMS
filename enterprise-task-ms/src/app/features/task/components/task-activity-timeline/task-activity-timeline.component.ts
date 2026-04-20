import { Component, Input, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TaskActivity } from '../../../../core/models/task-activity.model';

@Component({
  selector: 'app-task-activity-timeline',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './task-activity-timeline.component.html',
  styleUrl: './task-activity-timeline.component.scss'
})
export class TaskActivityTimelineComponent {

  @Input() set activities(value: TaskActivity[]) {
    this._activities.set(value ?? []);
  }

  private _activities = signal<TaskActivity[]>([]);

  activitiesSorted = computed(() =>
    [...this._activities()].sort(
      (a, b) =>
        new Date(b.createdAt).getTime() -
        new Date(a.createdAt).getTime()
    )
  );

  getActionText(activity: TaskActivity): string {
    switch (activity.actionType) {
      case 'status_change':
        return `changed status`;
      case 'priority_change':
        return `changed priority`;
      case 'assignee_change':
        return `changed assignee`;
      case 'progress_change':
        return `updated progress`;
      case 'comment_added':
        return `added comment`;
      case 'attachment_added':
        return `added attachment`;
      default:
        return activity.actionType ?? 'updated task';
    }
  }

  formatTime(date: Date): string {
    const diff = Date.now() - new Date(date).getTime();

    const minutes = Math.floor(diff / 60000);
    const hours = Math.floor(diff / 3600000);
    const days = Math.floor(diff / 86400000);

    if (minutes < 60) return `${minutes}m ago`;
    if (hours < 24) return `${hours}h ago`;
    return `${days}d ago`;
  }

}