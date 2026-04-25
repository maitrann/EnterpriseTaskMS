import { Injectable } from '@angular/core';

import { MOCK_ADMIN_USER, MOCK_AUTH_USERS } from '../mock-data/auth.mock';
import { AuthDataSource } from './auth.datasource';

@Injectable({ providedIn: 'root' })
export class MockAuthDataSource implements AuthDataSource {
  getMockAdminUser() {
    return { ...MOCK_ADMIN_USER };
  }

  getMockUsers() {
    return MOCK_AUTH_USERS.map((user) => ({ ...user }));
  }
}
