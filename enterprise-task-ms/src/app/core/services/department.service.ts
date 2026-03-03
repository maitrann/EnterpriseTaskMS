import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class DepartmentService {

  departments = signal([
    { id: 1, name: 'IT' },
    { id: 2, name: 'HR' },
    { id: 3, name: 'Finance' }
  ]);
}