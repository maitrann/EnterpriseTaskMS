export interface TaskDependency {
  id: number;
  taskId: number;
  dependsOnTaskId: number;
  dependencyType?: string;
}