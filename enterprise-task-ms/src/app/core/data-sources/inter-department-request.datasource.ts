import { InjectionToken } from '@angular/core';

import { InterDepartmentRequest } from '../models/inter-department-request.model';

export interface InterDepartmentRequestDataSource {
  getRequests(): InterDepartmentRequest[];
  getDepartmentOptions(): string[];
  getOwnerOptions(): string[];
}

export const INTER_DEPARTMENT_REQUEST_DATA_SOURCE = new InjectionToken<InterDepartmentRequestDataSource>(
  'INTER_DEPARTMENT_REQUEST_DATA_SOURCE'
);
