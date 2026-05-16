import { CommonModule } from '@angular/common';
import { Component, ElementRef, HostListener, computed, inject, signal } from '@angular/core';
import { RouterModule } from '@angular/router';

import { AuthService } from '../../core/services/auth.service';
import { ThemeService } from '../../core/services/theme.service';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './header.component.html',
  styleUrl: './header.component.scss'
})
export class HeaderComponent {
  private readonly authService = inject(AuthService);
  private readonly themeService = inject(ThemeService);
  private readonly elementRef = inject(ElementRef<HTMLElement>);

  readonly isUserMenuOpen = signal(false);
  readonly themeMode = this.themeService.mode;

  readonly quickStats = [
    { label: 'Đang xử lý', value: '24' },
    { label: 'Sắp đến hạn', value: '08' },
    { label: 'Cần xác nhận', value: '05' }
  ];

  readonly userProfile = computed(() => {
    const user = this.authService.user();
    const displayName = user?.fullName || user?.username || 'Người dùng';
    const initials = displayName
      .split(' ')
      .filter(Boolean)
      .slice(0, 2)
      .map((part) => part[0]?.toUpperCase() ?? '')
      .join('');

    return {
      name: displayName,
      role: user?.role || 'Người xem',
      initials: initials || 'ND'
    };
  });

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: Event) {
    if (!this.elementRef.nativeElement.contains(event.target as Node)) {
      this.isUserMenuOpen.set(false);
    }
  }

  toggleTheme() {
    this.themeService.toggleTheme();
  }

  toggleUserMenu() {
    this.isUserMenuOpen.update((value) => !value);
  }

  openProfile() {
    this.isUserMenuOpen.set(false);
  }

  logout() {
    this.isUserMenuOpen.set(false);
    this.authService.logout();
  }
}
