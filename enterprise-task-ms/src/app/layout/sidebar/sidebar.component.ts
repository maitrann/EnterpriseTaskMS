import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';

import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule
  ],
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.scss'
})
export class SidebarComponent {
  constructor(public readonly authService: AuthService) {}

  readonly menuGroups = [
    {
      title: 'Dieu hanh',
      items: [
        { label: 'Dashboard lanh dao', caption: 'Tong quan KPI va deadline', route: '/dashboard' },
        { label: 'Du an', caption: 'Quan ly du an va tien do', route: '/projects' },
        { label: 'Tong quan cong viec', caption: 'Board van hanh hien tai', route: '/tasks' }
      ]
    },
    {
      title: 'Van hanh noi bo',
      items: [
        { label: 'Phong ban', caption: 'Danh muc va co cau', route: '/departments' },
        { label: 'Yeu cau lien phong', caption: 'Luong xu ly noi bo', route: '/inter-department-requests' },
        { label: 'Bao cao', caption: 'Theo tien do va KPI', route: null }
      ]
    },
    {
      title: 'Quan tri',
      adminOnly: true,
      items: [
        { label: 'Nguoi dung & phan quyen', caption: 'Role, khoa tai khoan va scope phong ban', route: '/admin/users' }
      ]
    }
  ];
}
