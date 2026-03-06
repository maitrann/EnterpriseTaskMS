import { ApplicationConfig } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideStore } from '@ngrx/store';
// import { provideEffects } from '@ngrx/effects';
import { provideState } from '@ngrx/store';

import { routes } from './app.routes';
import { taskReducer } from './features/task/store/task.reducer';
// import { TaskEffects } from './features/task/store/task.effects';

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
    provideHttpClient(),
    provideStore(), // root store
    provideState('tasks', taskReducer),
    // provideEffects(TaskEffects),
  ],
};