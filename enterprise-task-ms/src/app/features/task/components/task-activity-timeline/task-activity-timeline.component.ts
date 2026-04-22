import { CommonModule } from '@angular/common';
import { Component, computed, Input, signal } from '@angular/core';

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

  private readonly _activities = signal<TaskActivity[]>([]);

  readonly activitiesSorted = computed(() =>
    [...this._activities()].sort(
      (first, second) =>
        new Date(second.createdAt).getTime() - new Date(first.createdAt).getTime()
    )
  );

  getActionText(activity: TaskActivity) {
    switch (activity.actionType) {
      case 'status_change':
        return 'cap nhat trang thai';
      case 'priority_change':
        return 'doi muc uu tien';
      case 'assignee_change':
        return 'doi nguoi phu trach';
      case 'progress_change':
        return 'cap nhat tien do';
      case 'comment_added':
        return 'them ghi chu';
      case 'attachment_added':
        return 'them tep dinh kem';
      default:
        return activity.actionType ?? 'cap nhat cong viec';
    }
  }

  formatTime(date: Date) {
    const diff = Date.now() - new Date(date).getTime();
    const minutes = Math.floor(diff / 60000);
    const hours = Math.floor(diff / 3600000);
    const days = Math.floor(diff / 86400000);

    if (minutes < 60) {
      return `${minutes}m truoc`;
    }

    if (hours < 24) {
      return `${hours}h truoc`;
    }

    return `${days} ngay truoc`;
  }
}
