import { Injectable } from '@angular/core';

import { MOCK_ADMIN_USER } from '../mock-data/auth.mock';
import { AuthDataSource } from './auth.datasource';

@Injectable({ providedIn: 'root' })
export class MockAuthDataSource implements AuthDataSource {
  getMockAdminUser() {
    return { ...MOCK_ADMIN_USER };
  }
}
