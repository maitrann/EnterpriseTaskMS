import { Injectable } from '@angular/core';

import {
  INTER_DEPARTMENT_OPTION_MOCK,
  INTER_DEPARTMENT_REQUEST_MOCK
} from '../mock-data/inter-department-request.mock';
import { InterDepartmentRequestDataSource } from './inter-department-request.datasource';

@Injectable({ providedIn: 'root' })
export class MockInterDepartmentRequestDataSource implements InterDepartmentRequestDataSource {
  getRequests() {
    return INTER_DEPARTMENT_REQUEST_MOCK.map((request) => ({ ...request }));
  }

  getDepartmentOptions() {
    return [...INTER_DEPARTMENT_OPTION_MOCK.departments];
  }

  getOwnerOptions() {
    return [...INTER_DEPARTMENT_OPTION_MOCK.owners];
  }
}
