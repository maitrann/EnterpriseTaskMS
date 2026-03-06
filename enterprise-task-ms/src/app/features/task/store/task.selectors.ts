import { createSelector, createFeatureSelector } from '@ngrx/store';
import { TaskState } from './task.state';

export const selectTaskState =
  createFeatureSelector<TaskState>('tasks');

export const selectAllTasks =
  createSelector(
    selectTaskState,
    state => state.tasks
  );

export const selectSelectedTask =
  createSelector(
    selectTaskState,
    state => state.selectedTask
  );

export const selectTasksByStatus = (statusId: number) =>
  createSelector(
    selectAllTasks,
    tasks => tasks.filter(t => t.statusId === statusId)
  );