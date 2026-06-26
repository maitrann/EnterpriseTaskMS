import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { API_BASE_URL } from '../constants/app.constants';
import { AdminUser, PagedResult, UserListQuery } from '../models/user.model';

@Injectable({ providedIn: 'root' })
export class UserService {
  readonly users = signal<AdminUser[]>([]);
  readonly totalItems = signal(0);
  readonly isLoading = signal(false);

  constructor(private readonly http: HttpClient) {}

  async loadUsers(query: UserListQuery = {}) {
    this.isLoading.set(true);
    try {
      const result = await firstValueFrom(
        this.http.get<PagedResult<AdminUser>>(`${API_BASE_URL}/users`, {
          params: this.createParams(query)
        })
      );
      this.users.set(result.items);
      this.totalItems.set(result.totalItems);
      return result;
    } finally {
      this.isLoading.set(false);
    }
  }

  getUser(id: string) {
    return firstValueFrom(this.http.get<AdminUser>(`${API_BASE_URL}/users/${id}`));
  }

  async lockUser(id: string) {
    await firstValueFrom(this.http.post<void>(`${API_BASE_URL}/users/${id}/lock`, {}));
    this.updateUserActiveState(id, false);
  }

  async unlockUser(id: string) {
    await firstValueFrom(this.http.post<void>(`${API_BASE_URL}/users/${id}/unlock`, {}));
    this.updateUserActiveState(id, true);
  }

  async assignRole(userId: string, roleId: number) {
    await firstValueFrom(this.http.post<void>(`${API_BASE_URL}/users/${userId}/roles`, { roleId }));
  }

  async removeRole(userId: string, roleId: number) {
    await firstValueFrom(this.http.delete<void>(`${API_BASE_URL}/users/${userId}/roles/${roleId}`));
  }

  async assignDepartmentScope(userId: string, departmentId: number) {
    await firstValueFrom(
      this.http.post<void>(`${API_BASE_URL}/users/${userId}/department-scopes`, { departmentId })
    );
    this.updateUserDepartmentScopes(userId, departmentId, true);
  }

  async removeDepartmentScope(userId: string, departmentId: number) {
    await firstValueFrom(
      this.http.delete<void>(`${API_BASE_URL}/users/${userId}/department-scopes/${departmentId}`)
    );
    this.updateUserDepartmentScopes(userId, departmentId, false);
  }

  private createParams(query: UserListQuery) {
    let params = new HttpParams();

    if (query.page !== undefined) {
      params = params.set('page', query.page);
    }

    if (query.pageSize !== undefined) {
      params = params.set('pageSize', query.pageSize);
    }

    if (query.search?.trim()) {
      params = params.set('search', query.search.trim());
    }

    if (query.departmentId !== undefined) {
      params = params.set('departmentId', query.departmentId);
    }

    if (query.isActive !== undefined) {
      params = params.set('isActive', query.isActive);
    }

    return params;
  }

  private updateUserActiveState(id: string, isActive: boolean) {
    this.users.update((users) =>
      users.map((user) => (user.id === id ? { ...user, isActive } : user))
    );
  }

  private updateUserDepartmentScopes(userId: string, departmentId: number, assigned: boolean) {
    this.users.update((users) =>
      users.map((user) => {
        if (user.id !== userId) {
          return user;
        }

        const scopedDepartmentIds = assigned
          ? Array.from(new Set([...user.scopedDepartmentIds, departmentId]))
          : user.scopedDepartmentIds.filter((id) => id !== departmentId);

        return { ...user, scopedDepartmentIds };
      })
    );
  }
}
