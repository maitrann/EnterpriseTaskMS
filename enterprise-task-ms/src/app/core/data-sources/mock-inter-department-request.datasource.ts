import { Injectable } from '@angular/core';

import {
  INTER_DEPARTMENT_DEPARTMENT_MOCK,
  INTER_DEPARTMENT_OWNER_MOCK,
  INTER_DEPARTMENT_REQUEST_MOCK
} from '../mock-data/inter-department-request.mock';
import { INTER_DEPARTMENT_SLA_POLICY_MOCK } from '../mock-data/inter-department-request.mock';
import { InterDepartmentRequestDataSource } from './inter-department-request.datasource';

@Injectable({ providedIn: 'root' })
export class MockInterDepartmentRequestDataSource implements InterDepartmentRequestDataSource {
  getRequests() {
    return INTER_DEPARTMENT_REQUEST_MOCK.map((request) => ({ ...request }));
  }

  getDepartmentOptions() {
    return INTER_DEPARTMENT_DEPARTMENT_MOCK.map((department) => ({ ...department }));
  }

  getOwnerOptions() {
    return INTER_DEPARTMENT_OWNER_MOCK.map((owner) => ({ ...owner }));
  }

  getSlaPolicies() {
    return INTER_DEPARTMENT_SLA_POLICY_MOCK.map((policy) => ({ ...policy }));
  }
}
