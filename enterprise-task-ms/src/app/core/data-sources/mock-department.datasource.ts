import { Injectable } from '@angular/core';

import { DEPARTMENT_CARD_MOCK } from '../mock-data/department.mock';
import { DepartmentDataSource } from './department.datasource';

@Injectable({ providedIn: 'root' })
export class MockDepartmentDataSource implements DepartmentDataSource {
  getDepartmentCards() {
    return DEPARTMENT_CARD_MOCK.map((department) => ({ ...department }));
  }
}
