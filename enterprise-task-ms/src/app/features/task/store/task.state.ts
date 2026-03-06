import { Task } from '../../../core/models/task.model';
import { TaskStatus } from '../../../core/models/task-status.model';

export interface TaskState {

  tasks: Task[];

  statuses: TaskStatus[];

  selectedTask: Task | null;

}

export const initialState: TaskState = {

  tasks: [],

  statuses: [],

  selectedTask: null

};