import { Component } from '@angular/core';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';

@Component({
  standalone: true,
  selector: 'app-sidebar',
  imports: [
    CommonModule,
    MatListModule,
    MatIconModule,
    RouterModule
  ],
  template: `
<div class="logo">
  Enterprise
</div>

<nav class="nav">

  <a routerLink="/dashboard" routerLinkActive="active">
    Dashboard
  </a>

  <a routerLink="/department" routerLinkActive="active">
    Departments
  </a>

  <a routerLink="/task" routerLinkActive="active">
    Tasks
  </a>

</nav>
  `,
  styles: [`
.logo {
  font-size: 18px;
  font-weight: 600;
  margin-bottom: 50px;
  letter-spacing: -0.5px;
}

.nav a {
  display: block;
  padding: 12px 16px;
  margin-bottom: 10px;
  border-radius: 14px;
  text-decoration: none;
  font-weight: 500;
  color: #64748b;
  transition: 0.2s ease;
}

.nav a:hover {
  background: #f1f5f9;
  color: #0f172a;
}

.nav a.active {
  background: #4f46e5;
  color: white;
}
  `]
})
export class SidebarComponent {}