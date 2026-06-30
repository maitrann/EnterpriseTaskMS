import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { API_BASE_URL } from '../constants/app.constants';
import { EntityId } from '../models/common-id.model';
import {
  AddTaskCommentRequest,
  ArchiveTaskRequest,
  CreateSubTaskRequest,
  CreateTaskExtensionRequest,
  CreateTaskRequest,
  DuplicateTaskRequest,
  ReviewTaskExtensionRequest,
  TaskCreateResponse,
  TaskDuplicateResponse,
  TaskMutationResponse,
  TransferTaskAssigneeRequest,
  UpdateSubTaskRequest,
  UpdateTaskRequest,
  UpdateTaskStatusRequest
} from '../models/task-api-contract.model';
import { TaskFormOptions } from '../models/task-form.model';
import { TaskActivity } from '../models/task-activity.model';
import { Task } from '../models/task.model';

@Injectable({ providedIn: 'root' })
export class TaskApiClient {
  constructor(private readonly http: HttpClient) {}

  async loadSnapshot() {
    const [tasks, activities, formOptions] = await Promise.all([
      firstValueFrom(this.http.get<Task[]>(`${API_BASE_URL}/tasks`)),
      firstValueFrom(this.http.get<TaskActivity[]>(`${API_BASE_URL}/tasks/activities`)),
      firstValueFrom(this.http.get<TaskFormOptions>(`${API_BASE_URL}/tasks/form-options`))
    ]);

    return { tasks, activities, formOptions };
  }

  getTask(taskId: EntityId) {
    return firstValueFrom(this.http.get<Task>(`${API_BASE_URL}/tasks/${taskId}`));
  }

  createTask(payload: CreateTaskRequest) {
    return firstValueFrom(this.http.post<TaskCreateResponse>(`${API_BASE_URL}/tasks`, payload));
  }

  updateTask(taskId: EntityId, payload: UpdateTaskRequest) {
    return firstValueFrom(this.http.put<TaskMutationResponse>(`${API_BASE_URL}/tasks/${taskId}`, payload));
  }

  updateStatus(taskId: EntityId, payload: UpdateTaskStatusRequest) {
    return firstValueFrom(this.http.post<TaskMutationResponse>(`${API_BASE_URL}/tasks/${taskId}/status`, payload));
  }

  transferAssignee(taskId: EntityId, payload: TransferTaskAssigneeRequest) {
    return firstValueFrom(this.http.post<TaskMutationResponse>(`${API_BASE_URL}/tasks/${taskId}/assignee`, payload));
  }

  duplicateTask(taskId: EntityId, payload: DuplicateTaskRequest) {
    return firstValueFrom(this.http.post<TaskDuplicateResponse>(`${API_BASE_URL}/tasks/${taskId}/duplicate`, payload));
  }

  archiveTask(taskId: EntityId, payload: ArchiveTaskRequest) {
    return firstValueFrom(this.http.post<TaskMutationResponse>(`${API_BASE_URL}/tasks/${taskId}/archive`, payload));
  }

  addComment(taskId: EntityId, payload: AddTaskCommentRequest) {
    return firstValueFrom(this.http.post<TaskCreateResponse>(`${API_BASE_URL}/tasks/${taskId}/comments`, payload));
  }

  requestExtension(taskId: EntityId, payload: CreateTaskExtensionRequest) {
    return firstValueFrom(this.http.post<TaskCreateResponse>(`${API_BASE_URL}/tasks/${taskId}/extension-requests`, payload));
  }

  reviewExtension(taskId: EntityId, requestId: EntityId, payload: ReviewTaskExtensionRequest) {
    return firstValueFrom(
      this.http.post<TaskMutationResponse>(`${API_BASE_URL}/tasks/${taskId}/extension-requests/${requestId}/review`, payload)
    );
  }

  createSubtask(taskId: EntityId, payload: CreateSubTaskRequest) {
    return firstValueFrom(this.http.post<TaskCreateResponse>(`${API_BASE_URL}/tasks/${taskId}/subtasks`, payload));
  }

  updateSubtask(taskId: EntityId, subtaskId: EntityId, payload: UpdateSubTaskRequest) {
    return firstValueFrom(this.http.put<TaskMutationResponse>(`${API_BASE_URL}/tasks/${taskId}/subtasks/${subtaskId}`, payload));
  }

  deleteSubtask(taskId: EntityId, subtaskId: EntityId) {
    return firstValueFrom(this.http.delete<TaskMutationResponse>(`${API_BASE_URL}/tasks/${taskId}/subtasks/${subtaskId}`));
  }
}
