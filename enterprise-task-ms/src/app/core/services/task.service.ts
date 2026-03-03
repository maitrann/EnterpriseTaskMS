import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class TaskService {

  tasks = signal([
    {
      id: 1,
      title: 'Design Database',
      description: 'Create DB and table',
      status: 'todo',
      priority: 'high',
      departmentId: 1,
      assignedUserId: 1,
      deadline: new Date()
    },
    {
      id: 2,
      title: 'Build API',
      description: 'Create .NET Core and API',
      status: 'inprogress',
      priority: 'medium',
      departmentId: 2,
      assignedUserId: 2,
      deadline: new Date()
    },
    {
      id: 3,
      title: 'Deploy App',
      description: 'Demo app in Git',
      status: 'done',
      priority: 'low',
      departmentId: 1,
      assignedUserId: 1,
      deadline: new Date()
    }
  ]);
}