import { Routes } from '@angular/router';
import { MainLayoutComponent } from './layout/main-layout/main-layout.component';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () =>
      import('./features/auth/login.component').then((m) => m.LoginComponent)
  },
  {
    path: 'forgot-password',
    loadComponent: () =>
      import('./features/auth/forgot-password.component').then((m) => m.ForgotPasswordComponent)
  },
  {
    path: '',
    canActivate: [authGuard],
    component: MainLayoutComponent,
    children: [
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./features/dashboard/dashboard.component').then((m) => m.DashboardComponent)
      },
      {
        path: 'tasks',
        loadComponent: () =>
          import('./features/task/components/task-board/task-board.component')
            .then(m => m.TaskBoardComponent)
      },
      {
        path: 'projects',
        loadComponent: () =>
          import('./features/project/project.component').then((m) => m.ProjectComponent)
      },
      {
        path: 'departments',
        loadComponent: () =>
          import('./features/department/department.component').then((m) => m.DepartmentComponent)
      },
      {
        path: 'inter-department-requests',
        loadComponent: () =>
          import('./features/inter-department-request/inter-department-request.component')
            .then((m) => m.InterDepartmentRequestComponent)
      },
      {
        path: '',
        redirectTo: 'dashboard',
        pathMatch: 'full'
      }
    ]
  },
  {
    path: '**',
    redirectTo: ''
  }
];
