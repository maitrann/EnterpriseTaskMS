import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { SelectModule } from 'primeng/select';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';

import {
  DepartmentCreateRequest,
  DepartmentListItem,
  DepartmentUpdateRequest
} from '../../../core/models/department-card.model';
import { AdminUser } from '../../../core/models/user.model';
import { DepartmentService } from '../../../core/services/department.service';
import { UserService } from '../../../core/services/user.service';

type DepartmentForm = DepartmentCreateRequest;

@Component({
  standalone: true,
  selector: 'app-admin-departments',
  imports: [CommonModule, FormsModule, ButtonModule, SelectModule, TableModule, TagModule],
  templateUrl: './admin-departments.component.html',
  styleUrl: './admin-departments.component.scss'
})
export class AdminDepartmentsComponent {
  private readonly departmentService = inject(DepartmentService);
  private readonly userService = inject(UserService);

  readonly departments = this.departmentService.departmentList;
  readonly managerUsers = signal<AdminUser[]>([]);
  readonly includeInactive = signal(true);
  readonly editingDepartmentId = signal<number | null>(null);
  readonly isSaving = signal(false);
  readonly actionMessage = signal<string | null>(null);
  readonly errorMessage = signal<string | null>(null);

  readonly form = signal<DepartmentForm>({
    companyId: 1,
    code: null,
    name: '',
    description: null,
    parentDepartmentId: null,
    managerId: null
  });

  readonly activeDepartments = computed(() => this.departments().filter((department) => department.isActive));
  readonly inactiveDepartments = computed(() => this.departments().filter((department) => !department.isActive));
  readonly companyOptions = computed(() => {
    const ids = Array.from(new Set(this.departments().map((department) => department.companyId)));
    return ids.length ? ids.map((id) => ({ id, name: `Company ${id}` })) : [{ id: 1, name: 'Company 1' }];
  });
  readonly parentOptions = computed(() => {
    const editingId = this.editingDepartmentId();
    return this.departments()
      .filter((department) => department.isActive && department.id !== editingId)
      .map((department) => ({
        id: department.id,
        name: department.parentDepartmentName
          ? `${department.parentDepartmentName} / ${department.name}`
          : department.name
      }));
  });
  readonly managerOptions = computed(() =>
    this.managerUsers().map((user) => ({
      id: user.id,
      name: user.fullName || user.email || user.employeeCode || user.id
    }))
  );

  constructor() {
    void this.loadInitialData();
  }

  async loadInitialData() {
    await Promise.all([
      this.reloadDepartments(),
      this.loadManagerOptions()
    ]);
  }

  async reloadDepartments() {
    await this.departmentService.loadAdminList(this.includeInactive());
    await this.departmentService.loadAdminTree(this.includeInactive());
    this.ensureCompanyDefault();
  }

  async loadManagerOptions() {
    const result = await this.userService.loadUsers({
      page: 1,
      pageSize: 200,
      isActive: true
    });
    this.managerUsers.set(result.items);
  }

  async applyIncludeInactive() {
    await this.runAction('Đã tải lại danh sách phòng ban.', () => this.reloadDepartments(), false);
  }

  startCreate() {
    this.editingDepartmentId.set(null);
    this.form.set({
      companyId: this.companyOptions()[0]?.id ?? 1,
      code: null,
      name: '',
      description: null,
      parentDepartmentId: null,
      managerId: null
    });
  }

  editDepartment(department: DepartmentListItem) {
    this.editingDepartmentId.set(department.id);
    this.form.set({
      companyId: department.companyId,
      code: department.code,
      name: department.name,
      description: department.description,
      parentDepartmentId: department.parentDepartmentId,
      managerId: department.managerId
    });
  }

  async saveDepartment() {
    const current = this.normalizeForm(this.form());
    if (!current.name) {
      this.errorMessage.set('Hãy nhập tên phòng ban.');
      return;
    }

    const editingId = this.editingDepartmentId();
    await this.runAction(
      editingId ? 'Đã cập nhật phòng ban.' : 'Đã tạo phòng ban.',
      async () => {
        if (editingId) {
          const updateRequest: DepartmentUpdateRequest = {
            code: current.code,
            name: current.name,
            description: current.description,
            parentDepartmentId: current.parentDepartmentId
          };
          await this.departmentService.updateDepartment(editingId, updateRequest);
          await this.departmentService.assignDepartmentManager(editingId, { managerId: current.managerId });
        } else {
          await this.departmentService.createDepartment(current);
        }

        await this.reloadDepartments();
        this.startCreate();
      }
    );
  }

  async deactivateDepartment(department: DepartmentListItem) {
    if (!department.isActive) {
      return;
    }

    await this.runAction(`Đã vô hiệu hóa ${department.name}.`, async () => {
      await this.departmentService.deactivateDepartment(department.id);
      await this.reloadDepartments();
    });
  }

  updateForm<K extends keyof DepartmentForm>(key: K, value: DepartmentForm[K]) {
    this.form.update((current) => ({
      ...current,
      [key]: value
    }));
  }

  getStatusSeverity(department: DepartmentListItem) {
    return department.isActive ? 'success' : 'danger';
  }

  trackByDepartmentId(_: number, department: DepartmentListItem) {
    return department.id;
  }

  private ensureCompanyDefault() {
    if (this.form().companyId !== 1 || !this.departments().length) {
      return;
    }

    this.form.update((current) => ({
      ...current,
      companyId: this.departments()[0]?.companyId ?? current.companyId
    }));
  }

  private normalizeForm(form: DepartmentForm): DepartmentForm {
    return {
      companyId: Number(form.companyId),
      code: this.normalizeOptional(form.code),
      name: form.name.trim(),
      description: this.normalizeOptional(form.description),
      parentDepartmentId: form.parentDepartmentId ? Number(form.parentDepartmentId) : null,
      managerId: this.normalizeOptional(form.managerId)
    };
  }

  private normalizeOptional(value: string | null) {
    return value?.trim() ? value.trim() : null;
  }

  private async runAction(message: string, action: () => Promise<void>, showSuccess = true) {
    this.errorMessage.set(null);
    this.actionMessage.set(null);
    this.isSaving.set(true);

    try {
      await action();
      if (showSuccess) {
        this.actionMessage.set(message);
      }
    } catch {
      this.errorMessage.set('Thao tác phòng ban thất bại. Hãy kiểm tra quyền admin, dữ liệu liên quan hoặc backend API.');
    } finally {
      this.isSaving.set(false);
    }
  }
}
