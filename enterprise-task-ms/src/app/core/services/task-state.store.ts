import { Inject, Injectable, signal } from '@angular/core';

import { TASK_DATA_SOURCE, TaskDataSource } from '../data-sources/task.datasource';
import { TaskFormOptions } from '../models/task-form.model';
import { TaskActivity } from '../models/task-activity.model';
import { Task } from '../models/task.model';

@Injectable({ providedIn: 'root' })
export class TaskStateStore {
  readonly tasks = signal<Task[]>([]);
  readonly activities = signal<TaskActivity[]>([]);
  readonly formOptions = signal<TaskFormOptions>({
    taskTypes: [],
    departments: [],
    users: [],
    priorities: [],
    urgencyLevels: [],
    securityLevels: [],
    sources: []
  });

  constructor(@Inject(TASK_DATA_SOURCE) taskDataSource: TaskDataSource) {
    this.tasks.set(taskDataSource.getTasks());
    this.activities.set(taskDataSource.getTaskActivities());
    this.formOptions.set(taskDataSource.getTaskFormOptions());
  }
}
