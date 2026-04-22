import { InjectionToken } from '@angular/core';

import { DepartmentCard } from '../models/department-card.model';

export interface DepartmentDataSource {
  getDepartmentCards(): DepartmentCard[];
}

export const DEPARTMENT_DATA_SOURCE = new InjectionToken<DepartmentDataSource>('DEPARTMENT_DATA_SOURCE');
