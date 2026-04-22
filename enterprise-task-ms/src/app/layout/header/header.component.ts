import { CommonModule } from '@angular/common';
import { Component, computed, inject } from '@angular/core';
import { RouterModule } from '@angular/router';

import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './header.component.html',
  styleUrl: './header.component.scss'
})
export class HeaderComponent {
  private readonly authService = inject(AuthService);

  readonly quickStats = [
    { label: 'Dang xu ly', value: '24' },
    { label: 'Sap den han', value: '08' },
    { label: 'Can xac nhan', value: '05' }
  ];

  readonly userProfile = computed(() => {
    const user = this.authService.user();
    const displayName = user?.fullName || user?.username || 'Guest User';
    const initials = displayName
      .split(' ')
      .filter(Boolean)
      .slice(0, 2)
      .map((part) => part[0]?.toUpperCase() ?? '')
      .join('');

    return {
      name: displayName,
      role: user?.role || 'Workspace Viewer',
      initials: initials || 'GU'
    };
  });

  logout() {
    this.authService.logout();
  }
}
