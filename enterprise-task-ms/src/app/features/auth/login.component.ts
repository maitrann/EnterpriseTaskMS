import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { AuthService } from '../../core/services/auth.service';
import { Router } from '@angular/router';

@Component({
  standalone: true,
  selector: 'app-login',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule
  ],
  template: `
    <div class="login-container">
      <mat-card class="login-card">

        <h2 class="title">Enterprise Task MS</h2>
        <p class="subtitle">Sign in to your account</p>

        <form [formGroup]="form" (ngSubmit)="onSubmit()">

          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Email</mat-label>
            <input matInput formControlName="email" type="email">
            <mat-icon matSuffix>email</mat-icon>

            <mat-error *ngIf="form.get('email')?.hasError('required')">
              Email is required
            </mat-error>

            <mat-error *ngIf="form.get('email')?.hasError('email')">
              Invalid email format
            </mat-error>
          </mat-form-field>

          <mat-form-field appearance="outline" class="full-width">
            <mat-label>Password</mat-label>
            <input matInput formControlName="password" type="password">
            <mat-icon matSuffix>lock</mat-icon>

            <mat-error *ngIf="form.get('password')?.hasError('required')">
              Password is required
            </mat-error>
          </mat-form-field>

          <button
            mat-raised-button
            color="primary"
            class="full-width login-btn"
            [disabled]="form.invalid">

            Login
          </button>

        </form>

      </mat-card>
    </div>
  `,
  styles: [`
    .login-container {
      height: 100vh;
      display: flex;
      justify-content: center;
      align-items: center;
      background: linear-gradient(135deg, #e3f2fd, #f5f5f5);
    }

    .login-card {
      width: 380px;
      padding: 30px;
      border-radius: 12px;
    }

    .title {
      text-align: center;
      margin-bottom: 5px;
      font-weight: 600;
    }

    .subtitle {
      text-align: center;
      margin-bottom: 25px;
      color: #666;
      font-size: 14px;
    }

    .full-width {
      width: 100%;
      margin-bottom: 15px;
    }

    .login-btn {
      height: 45px;
      font-weight: 600;
    }
  `]
})
export class LoginComponent implements OnInit {

  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);

  form = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]]
  });

  ngOnInit(): void {
    // Nếu đã login rồi thì không cho quay lại login
    if (this.authService.isAuthenticated()) {
      this.router.navigate(['/dashboard']);
    }
  }

  onSubmit() {
    if (this.form.invalid) return;

    const { email, password } = this.form.value;

    this.authService.login(email!, password!);
  }
}