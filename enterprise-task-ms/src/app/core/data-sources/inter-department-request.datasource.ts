import { InjectionToken } from '@angular/core';

import {
  InterDepartmentRequest,
  RequestDepartmentRef,
  RequestOwnerRef,
  RequestSlaPolicy
} from '../models/inter-department-request.model';

export interface InterDepartmentRequestDataSource {
  getRequests(): InterDepartmentRequest[];
  getDepartmentOptions(): RequestDepartmentRef[];
  getOwnerOptions(): RequestOwnerRef[];
  getSlaPolicies(): RequestSlaPolicy[];
}

export const INTER_DEPARTMENT_REQUEST_DATA_SOURCE = new InjectionToken<InterDepartmentRequestDataSource>(
  'INTER_DEPARTMENT_REQUEST_DATA_SOURCE'
);
