import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import {
  CdkDragDrop,
  DragDropModule,
  moveItemInArray,
  transferArrayItem
} from '@angular/cdk/drag-drop';

import { TASK_STATUS_DEFINITIONS } from '../../../../core/constants/task-status.constants';
import { CreateTaskInput } from '../../../../core/models/task-form.model';
import { Task } from '../../../../core/models/task.model';
import { TaskService } from '../../../../core/services/task.service';
import {
  CustomSelectComponent,
  CustomSelectOption
} from '../../../../shared/ui/custom-select/custom-select.component';
import { TaskCardComponent } from '../task-card/task-card.component';
import { TaskCreateModalComponent } from '../task-create-modal/task-create-modal.component';
import { TaskDetailDrawerComponent } from '../task-detail-drawer/task-detail-drawer.component';
import { TaskEditModalComponent } from '../task-edit-modal/task-edit-modal.component';

@Component({
  selector: 'app-task-board',
  standalone: true,
  imports: [
    CommonModule,
    DragDropModule,
    CustomSelectComponent,
    TaskCardComponent,
    TaskCreateModalComponent,
    TaskDetailDrawerComponent,
    TaskEditModalComponent
  ],
  templateUrl: './task-board.component.html',
  styleUrl: './task-board.component.scss'
})
export class TaskBoardComponent {
  private readonly taskService = inject(TaskService);
  private readonly statusPresets = {
    focus: [1, 2, 3, 5, 10],
    active: [2, 3, 4, 5, 6, 10],
    closing: [7, 8, 9]
  } as const;

  readonly tasks = this.taskService.tasks;
  readonly formOptions = this.taskService.formOptions;

  readonly selectedTask = signal<Task | null>(null);
  readonly editingTask = signal<Task | null>(null);
  readonly isCreateModalOpen = signal(false);

  readonly search = signal('');
  readonly filterPriority = signal<number | null>(null);
  readonly statusView = signal<'focus' | 'active' | 'closing' | 'all'>('focus');

  readonly parentTaskOptions = computed(() => this.taskService.getParentTaskOptions());
  readonly editParentTaskOptions = computed(() =>
    this.taskService.getParentTaskOptions(this.editingTask()?.id)
  );
  readonly priorityFilterOptions: CustomSelectOption<number | null>[] = [
    { value: null, label: 'Tất cả' },
    { value: 1, label: 'Thấp' },
    { value: 2, label: 'Trung bình' },
    { value: 3, label: 'Cao' },
    { value: 4, label: 'Khẩn cấp' }
  ];
  readonly nextTaskCode = computed(() => {
    const nextId = this.tasks().length ? Math.max(...this.tasks().map((task) => task.id)) + 1 : 1;
    return `CV-${String(nextId).padStart(4, '0')}`;
  });

  readonly statuses = TASK_STATUS_DEFINITIONS;
  readonly statusViewOptions = [
    { value: 'focus' as const, label: 'Trọng tâm', helper: 'Giữ các trạng thái cần theo dõi nhiều nhất' },
    { value: 'active' as const, label: 'Đang xử lý', helper: 'Nhóm các trạng thái vận hành trong ngày' },
    { value: 'closing' as const, label: 'Kết thúc', helper: 'Các việc đã chốt, hủy hoặc đóng' },
    { value: 'all' as const, label: 'Tất cả', helper: 'Hiển thị toàn bộ 10 trạng thái' }
  ];
  readonly visibleStatuses = computed(() => {
    const mode = this.statusView();

    if (mode === 'all') {
      return this.statuses;
    }

    const visibleIds =
      mode === 'focus'
        ? this.statusPresets.focus
        : mode === 'active'
          ? this.statusPresets.active
          : this.statusPresets.closing;

    return this.statuses.filter((status) => visibleIds.includes(status.id as never));
  });

  readonly filteredTasks = computed(() => {
    const keyword = this.search().toLowerCase().trim();
    const priority = this.filterPriority();

    return this.tasks().filter((task) => {
      const matchSearch =
        !keyword ||
        task.title.toLowerCase().includes(keyword) ||
        (task.description ?? '').toLowerCase().includes(keyword);

      const matchPriority = !priority || task.priorityId === priority;

      return matchSearch && matchPriority;
    });
  });

  readonly summaryCards = computed(() => {
    const tasks = this.filteredTasks();
    const overdueCount = tasks.filter((task) => this.getDueState(task) === 'overdue').length;
    const completionRate = tasks.length
      ? Math.round(tasks.reduce((sum, task) => sum + task.progress, 0) / tasks.length)
      : 0;

    return [
      {
        label: 'Tổng công việc',
        value: tasks.length,
        helper: 'Đang hiển thị theo bộ lọc hiện tại'
      },
      {
        label: 'Trễ hạn',
        value: overdueCount,
        helper: 'Cần ưu tiên xử lý trước'
      },
      {
        label: 'Tiến độ trung bình',
        value: `${completionRate}%`,
        helper: 'Mức hoàn thành của cả bảng'
      }
    ];
  });

  onPriorityChange(value: string | number | null | Array<string | number>) {
    this.filterPriority.set(typeof value === 'number' ? value : null);
  }

  clearFilters() {
    this.search.set('');
    this.filterPriority.set(null);
    this.statusView.set('focus');
  }

  setStatusView(view: 'focus' | 'active' | 'closing' | 'all') {
    this.statusView.set(view);
  }

  getTasks(statusId: number) {
    return this.filteredTasks().filter((task) => task.statusId === statusId);
  }

  getConnectedDropLists(currentStatusId: number) {
    return this.visibleStatuses()
      .filter((status) => status.id !== currentStatusId)
      .map((status) => this.getDropListId(status.id));
  }

  getDropListId(statusId: number) {
    return `task-column-${statusId}`;
  }

  getTrackByTaskId(_: number, task: Task) {
    return task.id;
  }

  openTask(task: Task) {
    this.selectedTask.set(task);
  }

  closeDrawer() {
    this.selectedTask.set(null);
  }

  openEdit(task: Task) {
    this.editingTask.set({ ...task });
  }

  openCreateModal() {
    this.isCreateModalOpen.set(true);
  }

  closeCreateModal() {
    this.isCreateModalOpen.set(false);
  }

  createTask(payload: CreateTaskInput) {
    const createdTask = this.taskService.createTask(payload);
    this.isCreateModalOpen.set(false);
    this.selectedTask.set(createdTask);
  }

  closeEditModal() {
    this.editingTask.set(null);
  }

  saveTask(updated: Task) {
    this.taskService.updateTask(updated);
    this.editingTask.set(null);
  }

  getDueState(task: Task): 'normal' | 'warning' | 'overdue' {
    if (task.statusId === 10) {
      return 'overdue';
    }

    if (!task.dueDate) {
      return 'normal';
    }

    const today = new Date();
    const due = new Date(task.dueDate);
    const diff = (due.getTime() - today.getTime()) / (1000 * 60 * 60 * 24);

    if (diff < 0) {
      return 'overdue';
    }

    if (diff <= 2) {
      return 'warning';
    }

    return 'normal';
  }

  drop(event: CdkDragDrop<Task[]>, newStatus: number) {
    if (event.previousContainer === event.container) {
      moveItemInArray(event.container.data, event.previousIndex, event.currentIndex);
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

    this.taskService.replaceAll([...this.tasks()]);
  }
}
