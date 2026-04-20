import { Task } from "./task.model";
import { TaskComment } from "./task-comment.model";
import { Attachment } from "./attachment.model";
import { Tag } from "./tag.model";
import { TaskWatcher } from "./task-watcher.model";
import { TimeLog } from "./time-log.model";

export interface TaskDetailView {
  task: Task;
  comments: TaskComment[];
  attachments: Attachment[];
  tags: Tag[];
  watchers: TaskWatcher[];
  timelogs: TimeLog[];
}

export enum TaskStatusEnum {
  Todo = 1,
  InProgress = 2,
  Review = 3,
  Done = 4
}

export enum TaskPriorityEnum {
  Low = 1,
  Medium = 2,
  High = 3,
  Critical = 4
}

export interface TaskTimelineItem {
  id: number;
  type: 'activity' | 'comment';

  userId: number;

  actionType?: string;
  oldValue?: string;
  newValue?: string;

  comment?: string;

  createdAt: Date;
}