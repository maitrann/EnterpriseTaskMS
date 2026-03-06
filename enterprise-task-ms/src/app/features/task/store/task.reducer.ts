import { createReducer, on } from '@ngrx/store';
import { initialState } from './task.state';
import * as TaskActions from './task.actions';

export const taskReducer = createReducer(

  initialState,

  on(TaskActions.setTasks, (state, { tasks }) => ({
    ...state,
    tasks
  })),

  on(TaskActions.updateTaskStatus, (state, { taskId, statusId }) => ({

    ...state,

    tasks: state.tasks.map(t =>
      t.id === taskId
        ? { ...t, statusId }
        : t
    )

  })),

  on(TaskActions.selectTask, (state, { task }) => ({
    ...state,
    selectedTask: task
  })),

  on(TaskActions.closeTaskDetail, state => ({
    ...state,
    selectedTask: null
  }))

);