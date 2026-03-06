import { createAction, props } from '@ngrx/store';
import { Task } from '../../../core/models/task.model';

export const loadTasks = createAction(
  '[Task] Load Tasks'
);

export const setTasks = createAction(
  '[Task] Set Tasks',
  props<{ tasks: Task[] }>()
);

export const updateTaskStatus = createAction(
  '[Task] Update Task Status',
  props<{ taskId: number; statusId: number }>()
);

export const selectTask = createAction(
  '[Task] Select Task',
  props<{ task: Task }>()
);

export const closeTaskDetail = createAction(
  '[Task] Close Task Detail'
);