import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { SelectModule } from 'primeng/select';
import { TableLazyLoadEvent, TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';

import { AdminUser } from '../../../core/models/user.model';
import { AuthService } from '../../../core/services/auth.service';
import { DepartmentService } from '../../../core/services/department.service';
import { RoleService } from '../../../core/services/role.service';
import { UserService } from '../../../core/services/user.service';

@Component({
  standalone: true,
  selector: 'app-admin-users',
  imports: [CommonModule, FormsModule, ButtonModule, SelectModule, TableModule, TagModule],
  templateUrl: './admin-users.component.html',
  styleUrl: './admin-users.component.scss'
})
export class AdminUsersComponent {
  private readonly userService = inject(UserService);
  private readonly roleService = inject(RoleService);
  private readonly departmentService = inject(DepartmentService);
  private readonly authService = inject(AuthService);

  readonly users = this.userService.users;
  readonly roles = this.roleService.roles;
  readonly departments = this.departmentService.departmentOptions;
  readonly totalItems = this.userService.totalItems;
  readonly isLoading = this.userService.isLoading;

  readonly search = signal('');
  readonly isActive = signal<'all' | 'active' | 'inactive'>('all');
  readonly page = signal(1);
  readonly pageSize = signal(20);
  readonly rowsPerPageOptions = [10, 20, 50];
  readonly activeStatusOptions = [
    { label: 'Tất cả', value: 'all' },
    { label: 'Đang hoạt động', value: 'active' },
    { label: 'Đã khóa', value: 'inactive' }
  ];
  readonly selectedRoleByUser = signal<Record<string, number | null>>({});
  readonly selectedDepartmentByUser = signal<Record<string, number | null>>({});
  readonly actionMessage = signal<string | null>(null);
  readonly errorMessage = signal<string | null>(null);

  readonly activeUsers = computed(() => this.users().filter((user) => user.isActive).length);
  readonly inactiveUsers = computed(() => this.users().filter((user) => !user.isActive).length);

  constructor() {
    void this.loadInitialData();
  }

  async loadInitialData() {
    await Promise.all([
      this.reloadUsers(),
      this.roleService.loadRoles(),
      this.departmentService.loadOptions()
    ]);
  }

  async reloadUsers() {
    await this.runAction('Đã tải danh sách người dùng.', async () => {
      await this.userService.loadUsers({
        page: this.page(),
        pageSize: this.pageSize(),
        search: this.search(),
        isActive: this.isActive() === 'all' ? undefined : this.isActive() === 'active'
      });
    }, false);
  }

  async applyFilters() {
    this.page.set(1);
    await this.reloadUsers();
  }

  async onTableLazyLoad(event: TableLazyLoadEvent) {
    const rows = event.rows ?? this.pageSize();
    const first = event.first ?? 0;
    this.pageSize.set(rows);
    this.page.set(Math.floor(first / rows) + 1);
    await this.reloadUsers();
  }

  async lockUser(user: AdminUser) {
    if (this.isCurrentUser(user)) {
      this.errorMessage.set('Không thể khóa tài khoản đang đăng nhập.');
      return;
    }

    await this.runAction(`Đã khóa ${this.getUserLabel(user)}.`, async () => {
      await this.userService.lockUser(user.id);
      await this.reloadUsers();
    });
  }

  async unlockUser(user: AdminUser) {
    if (this.isCurrentUser(user)) {
      this.errorMessage.set('Không thể sửa trạng thái tài khoản đang đăng nhập.');
      return;
    }

    await this.runAction(`Đã mở khóa ${this.getUserLabel(user)}.`, async () => {
      await this.userService.unlockUser(user.id);
      await this.reloadUsers();
    });
  }

  async assignSelectedRole(user: AdminUser) {
    if (this.isCurrentUser(user)) {
      this.errorMessage.set('Không thể sửa role của tài khoản đang đăng nhập.');
      return;
    }

    const roleId = this.selectedRoleByUser()[user.id];
    if (!roleId) {
      this.errorMessage.set('Hãy chọn role trước khi gán.');
      return;
    }

    await this.runAction(`Đã gán role cho ${this.getUserLabel(user)}.`, async () => {
      await this.userService.assignRole(user.id, roleId);
      this.clearSelectedRole(user.id);
      await this.reloadUsers();
    });
  }

  async removeRole(user: AdminUser, roleCode: string) {
    if (this.isCurrentUser(user)) {
      this.errorMessage.set('Không thể sửa role của tài khoản đang đăng nhập.');
      return;
    }

    const role = this.roles().find((item) => item.code === roleCode);
    if (!role) {
      this.errorMessage.set('Không tìm thấy role trong danh sách hiện tại.');
      return;
    }

    await this.runAction(`Đã gỡ role ${roleCode} khỏi ${this.getUserLabel(user)}.`, async () => {
      await this.userService.removeRole(user.id, role.id);
      await this.reloadUsers();
    });
  }

  async assignSelectedDepartmentScope(user: AdminUser) {
    if (this.isCurrentUser(user)) {
      this.errorMessage.set('Không thể sửa scope của tài khoản đang đăng nhập.');
      return;
    }

    const departmentId = this.selectedDepartmentByUser()[user.id];
    if (!departmentId) {
      this.errorMessage.set('Hãy chọn phòng ban trước khi gán scope.');
      return;
    }

    await this.runAction(`Đã gán scope phòng ban cho ${this.getUserLabel(user)}.`, async () => {
      await this.userService.assignDepartmentScope(user.id, departmentId);
      this.clearSelectedDepartment(user.id);
      await this.reloadUsers();
    });
  }

  async removeDepartmentScope(user: AdminUser, departmentId: number) {
    if (this.isCurrentUser(user)) {
      this.errorMessage.set('Không thể sửa scope của tài khoản đang đăng nhập.');
      return;
    }

    await this.runAction(`Đã gỡ scope phòng ban khỏi ${this.getUserLabel(user)}.`, async () => {
      await this.userService.removeDepartmentScope(user.id, departmentId);
      await this.reloadUsers();
    });
  }

  getUserLabel(user: AdminUser) {
    return user.fullName || user.email || user.employeeCode || user.id;
  }

  getDepartmentName(departmentId: number) {
    return this.departments().find((department) => department.id === departmentId)?.name ?? `Phòng ban ${departmentId}`;
  }

  getRoleName(roleCode: string, index: number, user: AdminUser) {
    return user.roleNames[index] ?? this.roles().find((role) => role.code === roleCode)?.name ?? roleCode;
  }

  isCurrentUser(user: AdminUser) {
    return this.authService.user()?.id === user.id;
  }

  updateSelectedRole(userId: string, value: string) {
    this.selectedRoleByUser.update((current) => ({
      ...current,
      [userId]: value ? Number(value) : null
    }));
  }

  updateSelectedDepartment(userId: string, value: string) {
    this.selectedDepartmentByUser.update((current) => ({
      ...current,
      [userId]: value ? Number(value) : null
    }));
  }

  trackByUserId(_: number, user: AdminUser) {
    return user.id;
  }

  private clearSelectedRole(userId: string) {
    this.selectedRoleByUser.update((current) => ({ ...current, [userId]: null }));
  }

  private clearSelectedDepartment(userId: string) {
    this.selectedDepartmentByUser.update((current) => ({ ...current, [userId]: null }));
  }

  private async runAction(message: string, action: () => Promise<void>, showSuccess = true) {
    this.errorMessage.set(null);
    this.actionMessage.set(null);

    try {
      await action();
      if (showSuccess) {
        this.actionMessage.set(message);
      }
    } catch {
      this.errorMessage.set('Thao tác thất bại. Hãy kiểm tra quyền admin, token hoặc backend API.');
    }
  }
}
