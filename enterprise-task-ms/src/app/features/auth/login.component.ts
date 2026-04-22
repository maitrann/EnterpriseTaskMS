import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';

import { AuthService } from '../../core/services/auth.service';

@Component({
  standalone: true,
  selector: 'app-login',
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  readonly errorMessage = signal('');

  readonly form = this.fb.group({
    email: [this.authService.mockCredentials.email, [Validators.required, Validators.email]],
    password: [this.authService.mockCredentials.password, [Validators.required]]
  });

  readonly demoCredentials = this.authService.mockCredentials;

  ngOnInit() {
    if (this.authService.isAuthenticated()) {
      this.router.navigate(['/tasks']);
    }
  }

  fillDemoCredentials() {
    this.form.patchValue(this.demoCredentials);
    this.errorMessage.set('');
  }

  onSubmit() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { email, password } = this.form.getRawValue();
    const result = this.authService.login(email ?? '', password ?? '');

    if (!result.success) {
      this.errorMessage.set(result.message ?? 'Dang nhap that bai.');
    }
  }
}
