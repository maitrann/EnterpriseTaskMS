import { HttpClient } from '@angular/common/http';
import { Injectable, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { API_BASE_URL } from '../constants/app.constants';
import { Permission, Role } from '../models/role.model';

@Injectable({ providedIn: 'root' })
export class RoleService {
  readonly roles = signal<Role[]>([]);
  readonly permissions = signal<Permission[]>([]);

  constructor(private readonly http: HttpClient) {}

  async loadRoles() {
    const roles = await firstValueFrom(this.http.get<Role[]>(`${API_BASE_URL}/roles`));
    this.roles.set(roles);
    return roles;
  }

  async loadPermissions() {
    const permissions = await firstValueFrom(this.http.get<Permission[]>(`${API_BASE_URL}/roles/permissions`));
    this.permissions.set(permissions);
    return permissions;
  }
}
