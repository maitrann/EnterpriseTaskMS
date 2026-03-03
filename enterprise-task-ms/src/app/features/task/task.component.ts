import { Component, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  DragDropModule,
  CdkDragDrop,
  moveItemInArray,
  transferArrayItem
} from '@angular/cdk/drag-drop';

@Component({
  selector: 'app-task',
  standalone: true,
  imports: [CommonModule, FormsModule, DragDropModule],
  templateUrl: './task.component.html',
  styleUrl: './task.component.scss'
})
export class TaskComponent {

  // ===== Mock Departments =====
  departments = signal([
    { id: 1, name: 'Engineering' },
    { id: 2, name: 'Marketing' },
    { id: 3, name: 'HR' }
  ]);

  // ===== Mock Tasks =====
  tasks = signal([
    {
      id: 1,
      title: 'Build Landing Page',
      description: 'Create new SaaS landing UI',
      status: 'todo',
      priority: 'High',
      departmentId: 1,
      deadline: new Date('2026-03-10'),
      assignee: 'Alex',
      subtasks: [
        { title: 'Hero section', done: true },
        { title: 'Pricing table', done: false },
        { title: 'Testimonials', done: false }
      ]
    },
    {
      id: 2,
      title: 'Fix Login Bug',
      description: 'Resolve authentication issue',
      status: 'inprogress',
      priority: 'Medium',
      departmentId: 1,
      deadline: new Date('2026-03-12'),
      assignee: 'Emma',
      subtasks: [
        { title: 'Investigate token', done: true },
        { title: 'Fix middleware', done: true }
      ]
    },
    {
      id: 3,
      title: 'Marketing Campaign',
      description: 'Prepare social media strategy',
      status: 'done',
      priority: 'Low',
      departmentId: 2,
      deadline: new Date('2026-03-15'),
      assignee: 'Liam',
      subtasks: [
        { title: 'Content plan', done: true },
        { title: 'Design banner', done: true }
      ]
    },
    {
      id: 4,
      title: 'Employee Onboarding',
      description: 'Prepare onboarding documents',
      status: 'todo',
      priority: 'Medium',
      departmentId: 3,
      deadline: new Date('2026-03-18')
    },
    {
      id: 5,
      title: 'API Refactor',
      description: 'Refactor task API for performance',
      status: 'inprogress',
      priority: 'High',
      departmentId: 1,
      deadline: new Date('2026-03-20')
    },
    {
      id: 6,
      title: 'SEO Optimization',
      description: 'Improve Google ranking',
      status: 'todo',
      priority: 'Low',
      departmentId: 2,
      deadline: new Date('2026-03-22')
    },
    {
      id: 7,
      title: 'Internal Audit',
      description: 'Quarterly HR compliance audit',
      status: 'done',
      priority: 'High',
      departmentId: 3,
      deadline: new Date('2026-03-25')
    },
    {
      id: 8,
      title: 'Dashboard Analytics',
      description: 'Add chart statistics',
      status: 'inprogress',
      priority: 'Medium',
      departmentId: 1,
      deadline: new Date('2026-02-28')
    }
  ]);

  // ===== Filters =====
  search = signal('');
  selectedPriority = signal('All');
  selectedDepartment = signal('All');

  // ===== Kanban Columns =====
  columns = computed(() => [
    {
      title: 'To Do',
      status: 'todo',
      tasks: this.tasks().filter(t => t.status === 'todo')
    },
    {
      title: 'In Progress',
      status: 'inprogress',
      tasks: this.tasks().filter(t => t.status === 'inprogress')
    },
    {
      title: 'Done',
      status: 'done',
      tasks: this.tasks().filter(t => t.status === 'done')
    }
  ]);

  // ===== Filtered Columns =====
  filteredColumns = computed(() =>
    this.columns().map(col => ({
      ...col,
      tasks: col.tasks.filter(task => {

        const matchSearch =
          task.title.toLowerCase()
            .includes(this.search().toLowerCase());

        const matchPriority =
          this.selectedPriority() === 'All' ||
          task.priority === this.selectedPriority();

        const matchDept =
          this.selectedDepartment() === 'All' ||
          task.departmentId == +this.selectedDepartment();

        return matchSearch && matchPriority && matchDept;
      })
    }))
  );

  // ===== Drag Drop =====
  drop(event: CdkDragDrop<any[]>, newStatus: string) {

    if (event.previousContainer === event.container) {
      moveItemInArray(
        event.container.data,
        event.previousIndex,
        event.currentIndex
      );
    } else {
      transferArrayItem(
        event.previousContainer.data,
        event.container.data,
        event.previousIndex,
        event.currentIndex
      );
    }

    const task = event.container.data[event.currentIndex];
    task.status = newStatus;
  }
  getDepartmentName(id: number) {
    return this.departments().find(d => d.id === id)?.name || '';
  }
  getProgress(task: any): number {
    if (!task.subtasks || task.subtasks.length === 0) return 0;

    const done = task.subtasks.filter((s: any) => s.done).length;
    return Math.round((done / task.subtasks.length) * 100);
  }

  isOverdue(task: any): boolean {
    return new Date(task.deadline) < new Date()
      && task.status !== 'done';
  }


  // ===== Modal =====
  selectedTask = signal<any | null>(null);

  openCreateModal() {
    this.selectedTask.set({
      id: Date.now(),
      title: '',
      description: '',
      status: 'todo',
      priority: 'Low',
      departmentId: 1,
      deadline: new Date(),
      assignee: '',
      subtasks: []
    });
  }

  openTask(task: any) {
    this.selectedTask.set({ ...task });
  }

  closeModal() {
    this.selectedTask.set(null);
  }

  saveTask() {
    const updated = this.selectedTask();
    const index = this.tasks()
      .findIndex(t => t.id === updated.id);

    if (index !== -1) {
      this.tasks.update(tasks => {
        tasks[index] = updated;
        return [...tasks];
      });
    }

    this.closeModal();
  }

  // ===== Delete =====
  deleteTask(task: any, event: Event) {
    event.stopPropagation();

    this.tasks.update(tasks =>
      tasks.filter(t => t.id !== task.id)
    );
  }
}