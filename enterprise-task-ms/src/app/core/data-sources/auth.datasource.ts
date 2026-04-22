import { InjectionToken } from '@angular/core';

import { MockAuthUser } from '../mock-data/auth.mock';

export interface AuthDataSource {
  getMockAdminUser(): MockAuthUser;
}

export const AUTH_DATA_SOURCE = new InjectionToken<AuthDataSource>('AUTH_DATA_SOURCE');
