import { Injectable } from '@angular/core';

import { TASK_ACTIVITY_MOCK } from '../mock-data/task-activity.mock';
import { TASK_FORM_OPTIONS_MOCK, TASK_MOCK } from '../mock-data/task.mock';
import { TaskDataSource } from './task.datasource';

@Injectable({ providedIn: 'root' })
export class MockTaskDataSource implements TaskDataSource {
  getTasks() {
    return TASK_MOCK.map((task) => ({ ...task }));
  }

  getTaskActivities() {
    return TASK_ACTIVITY_MOCK.map((activity) => ({ ...activity }));
  }

  getTaskFormOptions() {
    return {
      ...TASK_FORM_OPTIONS_MOCK,
      taskTypes: [...TASK_FORM_OPTIONS_MOCK.taskTypes],
      departments: [...TASK_FORM_OPTIONS_MOCK.departments],
      users: [...TASK_FORM_OPTIONS_MOCK.users],
      priorities: [...TASK_FORM_OPTIONS_MOCK.priorities],
      urgencyLevels: [...TASK_FORM_OPTIONS_MOCK.urgencyLevels],
      securityLevels: [...TASK_FORM_OPTIONS_MOCK.securityLevels],
      sources: [...TASK_FORM_OPTIONS_MOCK.sources]
    };
  }
}
