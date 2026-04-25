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
      case 'attachment_change':
        return 'cap nhat tep dinh kem';
      case 'deadline_change':
        return 'doi deadline';
      case 'processing_note_change':
        return 'cap nhat ghi chu xu ly';
      case 'accept_task':
        return 'nhan viec';
      case 'reject_acceptance':
        return 'tu choi tiep nhan';
      case 'request_extension':
        return 'xin gia han';
      case 'approve_extension':
        return 'duyet gia han';
      case 'reject_extension':
        return 'tu choi gia han';
      case 'transfer_assignee':
        return 'chuyen nguoi xu ly';
      case 'complete_task':
        return 'hoan thanh cong viec';
      case 'confirm_completion':
        return 'xac nhan hoan thanh';
      case 'cancel_task':
        return 'huy cong viec';
      case 'duplicate_task':
        return 'sao chep cong viec';
      case 'create_similar_task':
        return 'tao cong viec tuong tu';
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
