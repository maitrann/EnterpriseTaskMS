import { provideHttpClient } from '@angular/common/http';
import { ApplicationConfig } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideState, provideStore } from '@ngrx/store';

import { AUTH_DATA_SOURCE } from './core/data-sources/auth.datasource';
import { DEPARTMENT_DATA_SOURCE } from './core/data-sources/department.datasource';
import { INTER_DEPARTMENT_REQUEST_DATA_SOURCE } from './core/data-sources/inter-department-request.datasource';
import { MockAuthDataSource } from './core/data-sources/mock-auth.datasource';
import { MockDepartmentDataSource } from './core/data-sources/mock-department.datasource';
import { MockInterDepartmentRequestDataSource } from './core/data-sources/mock-inter-department-request.datasource';
import { MockTaskDataSource } from './core/data-sources/mock-task.datasource';
import { TASK_DATA_SOURCE } from './core/data-sources/task.datasource';
import { routes } from './app.routes';
import { taskReducer } from './features/task/store/task.reducer';

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
    provideHttpClient(),
    provideStore(),
    provideState('tasks', taskReducer),
    { provide: AUTH_DATA_SOURCE, useClass: MockAuthDataSource },
    { provide: TASK_DATA_SOURCE, useClass: MockTaskDataSource },
    { provide: DEPARTMENT_DATA_SOURCE, useClass: MockDepartmentDataSource },
    {
      provide: INTER_DEPARTMENT_REQUEST_DATA_SOURCE,
      useClass: MockInterDepartmentRequestDataSource
    }
  ]
};
