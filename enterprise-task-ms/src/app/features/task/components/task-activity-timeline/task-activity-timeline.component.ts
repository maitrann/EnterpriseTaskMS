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
        return 'cập nhật trạng thái';
      case 'priority_change':
        return 'đổi mức ưu tiên';
      case 'assignee_change':
        return 'đổi người phụ trách';
      case 'progress_change':
        return 'cập nhật tiến độ';
      case 'comment_added':
        return 'thêm ghi chú';
      case 'attachment_added':
        return 'thêm tệp đính kèm';
      case 'attachment_change':
        return 'cập nhật tệp đính kèm';
      case 'deadline_change':
        return 'đổi deadline';
      case 'processing_note_change':
        return 'cập nhật ghi chú xử lý';
      case 'accept_task':
        return 'nhận việc';
      case 'reject_acceptance':
        return 'từ chối tiếp nhận';
      case 'request_extension':
        return 'xin gia hạn';
      case 'approve_extension':
        return 'duyệt gia hạn';
      case 'reject_extension':
        return 'từ chối gia hạn';
      case 'transfer_assignee':
        return 'chuyển người xử lý';
      case 'complete_task':
        return 'hoàn thành công việc';
      case 'confirm_completion':
        return 'xác nhận hoàn thành';
      case 'cancel_task':
        return 'hủy công việc';
      case 'duplicate_task':
        return 'sao chép công việc';
      case 'create_similar_task':
        return 'tạo công việc tương tự';
      default:
        return activity.actionType ?? 'cập nhật công việc';
    }
  }

  formatTime(date: Date) {
    const diff = Date.now() - new Date(date).getTime();
    const minutes = Math.floor(diff / 60000);
    const hours = Math.floor(diff / 3600000);
    const days = Math.floor(diff / 86400000);

    if (minutes < 60) {
      return `${minutes}m trước`;
    }

    if (hours < 24) {
      return `${hours}h trước`;
    }

    return `${days} ngày trước`;
  }
}
