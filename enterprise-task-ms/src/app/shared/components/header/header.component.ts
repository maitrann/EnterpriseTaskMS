import { Component, computed, inject } from '@angular/core';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { AuthService } from '../../../core/services/auth.service';
import { CommonModule } from '@angular/common';

@Component({
  standalone: true,
  selector: 'app-header',
  imports: [
    CommonModule,
    MatToolbarModule,
    MatIconModule,
    MatButtonModule
  ],
  template: `
<div class="header">

  <div class="title">
    Dashboard
  </div>

  <div class="actions">
    <button class="btn-ghost">Logout</button>
  </div>

</div>
  `,
  styles: [`
.header {
  height: 80px;
  background: transparent;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 50px;
}

.title {
  font-size: 22px;
  font-weight: 600;
  letter-spacing: -0.3px;
}

.btn-ghost {
  border: 1px solid #e2e8f0;
  padding: 8px 18px;
  border-radius: 14px;
  background: white;
  cursor: pointer;
  transition: 0.2s;
}

.btn-ghost:hover {
  background: #f1f5f9;
}
  `]
})
export class HeaderComponent {

  private authService = inject(AuthService);

  user = computed(() => this.authService.user());

  logout() {
    this.authService.logout();
  }
}