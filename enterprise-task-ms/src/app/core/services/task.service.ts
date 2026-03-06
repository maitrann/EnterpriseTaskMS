import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Task } from '../models/task.model';

@Injectable({
  providedIn: 'root'
})
export class TaskService {

  private http = inject(HttpClient);

  private baseUrl = '/api/tasks'; // .NET Core endpoint

  getAll(): Observable<Task[]> {
    return this.http.get<Task[]>(this.baseUrl);
  }

  getById(id: number): Observable<Task> {
    return this.http.get<Task>(`${this.baseUrl}/${id}`);
  }

  create(model: Partial<Task>): Observable<Task> {
    return this.http.post<Task>(this.baseUrl, model);
  }

  update(id: number, model: Partial<Task>): Observable<Task> {
    return this.http.put<Task>(`${this.baseUrl}/${id}`, model);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}