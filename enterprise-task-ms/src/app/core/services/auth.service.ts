import { Inject, Injectable, computed, signal } from '@angular/core';
import { Router } from '@angular/router';

import { AUTH_DATA_SOURCE, AuthDataSource } from '../data-sources/auth.datasource';
import { Task } from '../models/task.model';
import { User } from '../models/user.model';

const AUTH_STORAGE_KEY = 'etms-auth-user';
const AUTH_TOKEN_KEY = 'etms-auth-token';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly mockAdminUser: ReturnType<AuthDataSource['getMockAdminUser']>;
  private readonly mockUsers: ReturnType<AuthDataSource['getMockUsers']>;

  readonly user = signal<User | null>(this.restoreUser());
  readonly isLoggedIn = computed(() => !!this.user() && !!localStorage.getItem(AUTH_TOKEN_KEY));

  constructor(
    private readonly router: Router,
    @Inject(AUTH_DATA_SOURCE) private readonly authDataSource: AuthDataSource
  ) {
    this.mockAdminUser = this.authDataSource.getMockAdminUser();
    this.mockUsers = this.authDataSource.getMockUsers();
  }

  get mockCredentials() {
    return {
      email: this.mockAdminUser.email!,
      password: this.mockAdminUser.password
    };
  }

  login(email: string, password: string) {
    const normalizedEmail = email.trim().toLowerCase();
    const matchedUser = this.mockUsers.find(
      (user) => user.email?.toLowerCase() === normalizedEmail && user.password === password
    );

    if (!matchedUser) {
      return {
        success: false,
        message: 'Thong tin dang nhap khong dung. Thu lai mot tai khoan mock hop le.'
      };
    }

    const { password: _password, ...safeUser } = matchedUser;

    localStorage.setItem(AUTH_TOKEN_KEY, `mock-token-${safeUser.id}`);
    localStorage.setItem(AUTH_STORAGE_KEY, JSON.stringify(safeUser));
    this.user.set(safeUser);
    this.router.navigate(['/dashboard']);

    return {
      success: true
    };
  }

  logout() {
    localStorage.removeItem(AUTH_TOKEN_KEY);
    localStorage.removeItem(AUTH_STORAGE_KEY);
    this.user.set(null);
    this.router.navigate(['/login']);
  }

  forgotPassword(email: string) {
    const normalizedEmail = email.trim().toLowerCase();
    const knownUser = this.mockUsers.find((user) => user.email?.toLowerCase() === normalizedEmail);

    return {
      success: true,
      message: knownUser
        ? `Da gui huong dan dat lai mat khau mock cho tai khoan ${knownUser.email}.`
        : 'Neu email ton tai trong he thong, huong dan dat lai mat khau se duoc gui toi hop thu do.'
    };
  }

  isAuthenticated() {
    return this.isLoggedIn();
  }

  hasSpecialTaskPermission() {
    const role = this.user()?.role?.toLowerCase() ?? '';
    return role.includes('admin') || role.includes('lanh dao') || role.includes('lãnh đạo');
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
      return JSON.parse(raw) as User;
    } catch {
      localStorage.removeItem(AUTH_STORAGE_KEY);
      localStorage.removeItem(AUTH_TOKEN_KEY);
      return null;
    }
  }
}
