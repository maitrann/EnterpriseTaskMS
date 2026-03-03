import { Component } from '@angular/core';
import { MatSidenavModule } from '@angular/material/sidenav';
import { RouterOutlet } from '@angular/router';
import { HeaderComponent } from '../shared/components/header/header.component';
import { SidebarComponent } from '../shared/components/sidebar/sidebar.component';

@Component({
  standalone: true,
  selector: 'app-main-layout',
  imports: [MatSidenavModule, RouterOutlet, HeaderComponent, SidebarComponent],
  template: `
<div class="app-shell">

  <aside class="sidebar">
    <app-sidebar></app-sidebar>
  </aside>

  <div class="main">

    <app-header></app-header>

    <div class="content">
      <router-outlet></router-outlet>
    </div>

  </div>

</div>
  `,
  styles: [`
.app-shell {
  display: flex;
  height: 100vh;
  background: #f8fafc;
}

.sidebar {
  width: 260px;
  background: white;
  padding: 30px 20px;
  border-right: 1px solid #f1f5f9;
}

.main {
  flex: 1;
  display: flex;
  flex-direction: column;
}

.content {
  padding: 50px;
}
  `]
})
export class MainLayoutComponent {}