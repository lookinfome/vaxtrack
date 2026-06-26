import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { LoginRequest } from '../../../core/models/auth.model';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './login.html',
  styleUrl: './login.css'
})
export class Login {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);

  form = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]]
  });

  loading = signal(false);
  error = signal('');

  get emailControl() { return this.form.get('email')!; }
  get passwordControl() { return this.form.get('password')!; }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.error.set('');

    const request = this.form.getRawValue() as LoginRequest;

    this.authService.login(request).subscribe({
      next: () => {
        this.loading.set(false);
        const destination = this.authService.isAdmin() ? '/hospital' : '/user/profile';
        this.router.navigate([destination]);
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err.error?.message ?? 'Login failed. Please try again.');
        console.error('[Login] login failed:', err);
      }
    });
  }
}
