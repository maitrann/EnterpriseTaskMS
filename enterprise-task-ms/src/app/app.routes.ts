import { Routes } from '@angular/router';
import { MainLayoutComponent } from './layout/main-layout/main-layout.component';

export const routes: Routes = [
  {
    path: '',
    component: MainLayoutComponent,
    children: [
      {
        path: 'tasks',
        loadComponent: () =>
          import('./features/task/components/task-board/task-board.component')
            .then(m => m.TaskBoardComponent)
      },
      {
        path: '',
        redirectTo: 'tasks',
        pathMatch: 'full'
      }
    ]
  }
];