// import { Injectable, inject } from '@angular/core';
// import { Actions, createEffect, ofType } from '@ngrx/effects';
// import { of } from 'rxjs';
// import { map } from 'rxjs/operators';
// import * as TaskActions from './task.actions';
// import { TASK_MOCK } from '../task.mock';

// @Injectable()
// export class TaskEffects {

//   private actions$ = inject(Actions);

//   loadTasks$ = createEffect(() =>
//     this.actions$.pipe(
//       ofType(TaskActions.loadTasks),
//       map(() => TaskActions.loadTasksSuccess({ tasks: TASK_MOCK }))
//     )
//   );
// }