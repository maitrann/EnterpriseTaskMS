import { HttpClient } from '@angular/common/http';
import { Inject, Injectable, computed, signal } from '@angular/core';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { API_BASE_URL, AUTH_STORAGE_KEY, AUTH_TOKEN_KEY } from '../constants/app.constants';
import { AUTH_DATA_SOURCE, AuthDataSource } from '../data-sources/auth.datasource';
import { Task } from '../models/task.model';
import { User } from '../models/user.model';

type LoginResponse = {
  accessToken: string;
  expiresAt: string;
  user: Omit<User, 'createdAt'> & { createdAt: string };
};

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly mockAdminUser: ReturnType<AuthDataSource['getMockAdminUser']>;

  readonly user = signal<User | null>(this.restoreUser());
  readonly isLoggedIn = computed(() => !!this.user() && !!localStorage.getItem(AUTH_TOKEN_KEY));

  constructor(
    private readonly router: Router,
    private readonly http: HttpClient,
    @Inject(AUTH_DATA_SOURCE) private readonly authDataSource: AuthDataSource
  ) {
    this.mockAdminUser = this.authDataSource.getMockAdminUser();
  }

  get mockCredentials() {
    return {
      email: this.mockAdminUser.email ?? 'admin@etms.local',
      password: this.mockAdminUser.password
    };
  }

  async login(email: string, password: string) {
    try {
      const response = await firstValueFrom(
        this.http.post<LoginResponse>(`${API_BASE_URL}/auth/login`, { email, password })
      );
      const user = this.normalizeUser(response.user);

      localStorage.setItem(AUTH_TOKEN_KEY, response.accessToken);
      localStorage.setItem(AUTH_STORAGE_KEY, JSON.stringify(user));
      this.user.set(user);
      this.router.navigate(['/dashboard']);

      return { success: true };
    } catch {
      return {
        success: false,
        message: 'Khong dang nhap duoc API. Hay kiem tra backend dang chay va tai khoan seed hop le.'
      };
    }
  }

  logout() {
    localStorage.removeItem(AUTH_TOKEN_KEY);
    localStorage.removeItem(AUTH_STORAGE_KEY);
    this.user.set(null);
    this.router.navigate(['/login']);
  }

  forgotPassword(email: string) {
    return {
      success: true,
      message: email
        ? 'Neu email ton tai trong he thong, huong dan dat lai mat khau se duoc gui toi hop thu do.'
        : 'Vui long nhap email can khoi phuc.'
    };
  }

  isAuthenticated() {
    return this.isLoggedIn();
  }

  hasSpecialTaskPermission() {
    const role = this.user()?.role?.toLowerCase() ?? '';
    return role.includes('admin') || role.includes('director') || role.includes('lanh dao');
  }

  canEditTask(task: Task) {
    const user = this.user();

    if (!user) {
      return false;
    }

    if (this.hasSpecialTaskPermission()) {
      return true;
    }

    if (!task.departmentId || !user.departmentId) {
      return false;
    }

    return task.departmentId === user.departmentId;
  }

  private restoreUser(): User | null {
    const raw = localStorage.getItem(AUTH_STORAGE_KEY);

    if (!raw) {
      return null;
    }

    try {
      return this.normalizeUser(JSON.parse(raw) as Omit<User, 'createdAt'> & { createdAt: string });
    } catch {
      localStorage.removeItem(AUTH_STORAGE_KEY);
      localStorage.removeItem(AUTH_TOKEN_KEY);
      return null;
    }
  }

  private normalizeUser(user: Omit<User, 'createdAt'> & { createdAt: string }): User {
    return {
      ...user,
      createdAt: new Date(user.createdAt)
    };
  }
}
