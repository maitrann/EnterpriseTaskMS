import { InjectionToken } from '@angular/core';

import { TaskFormOptions } from '../models/task-form.model';
import { TaskActivity } from '../models/task-activity.model';
import { Task } from '../models/task.model';

export interface TaskDataSource {
  getTasks(): Task[];
  getTaskActivities(): TaskActivity[];
  getTaskFormOptions(): TaskFormOptions;
}

export const TASK_DATA_SOURCE = new InjectionToken<TaskDataSource>('TASK_DATA_SOURCE');
