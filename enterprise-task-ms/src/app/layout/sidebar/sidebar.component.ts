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
        { label: 'Dashboard lanh dao', caption: 'Tong quan KPI va deadline', route: '/dashboard' },
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
    }
  ];
}
