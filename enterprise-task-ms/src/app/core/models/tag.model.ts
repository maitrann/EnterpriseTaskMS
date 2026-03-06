export interface Tag {
  id: number;
  name?: string;
  color?: string;
}
export interface TaskTag {
  id: number;
  taskId: number;
  tagId: number;
}