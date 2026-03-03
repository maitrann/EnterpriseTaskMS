import { Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';

@Injectable({ providedIn: 'root' })
export class AuthService {

  user = signal<any>(null);

  constructor(private router: Router) {}

  login(email: string, password: string) {
    const mockUser = {
      id: 1,
      name: 'Admin User',
      role: 'admin'
    };

    localStorage.setItem('token', 'mock-token');
    this.user.set(mockUser);
    this.router.navigate(['/dashboard']);
  }

  logout() {
    localStorage.removeItem('token');
    this.user.set(null);
    this.router.navigate(['/login']);
  }

  isAuthenticated() {
    return !!localStorage.getItem('token');
  }
}