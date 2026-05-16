import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

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
  readonly menuGroups = [
    {
      title: 'Dieu hanh',
      items: [
        { label: 'Dashboard lãnh đạo', caption: 'Tổng quan KPI và deadline', route: '/dashboard' },
        { label: 'Dự án', caption: 'Quản lý dự án và tiến độ', route: '/projects' },
        { label: 'Tổng quan công việc', caption: 'Board vận hành hiện tại', route: '/tasks' }
      ]
    },
    {
      title: 'Vận hành nội bộ',
      items: [
        { label: 'Phòng ban', caption: 'Danh mục và cơ cấu', route: '/departments' },
        { label: 'Yêu cầu liên phòng', caption: 'Luồng xử lý nội bộ', route: '/inter-department-requests' },
        { label: 'Báo cáo', caption: 'Theo tiến độ và KPI', route: null }
      ]
    }
  ];
}
