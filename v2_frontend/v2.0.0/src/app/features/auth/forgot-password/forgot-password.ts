import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { ForgotPasswordResponse } from '../../../core/models/auth.model';

@Component({
  selector: 'app-forgot-password',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './forgot-password.html',
  styleUrl: './forgot-password.css'
})
export class ForgotPassword {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);

  loading = signal(false);
  error = signal('');
  resetResponse = signal<ForgotPasswordResponse | null>(null);

  form = this.fb.group({
    email: ['', [Validators.required, Validators.email]]
  });

  get emailControl() { return this.form.get('email')!; }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.error.set('');

    this.authService.forgotPassword({ email: this.emailControl.value! }).subscribe({
      next: (res) => {
        this.loading.set(false);
        this.resetResponse.set(res);
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err.error?.message ?? 'Something went wrong. Please try again.');
        console.error('[ForgotPassword] forgotPassword failed:', err);
      }
    });
  }

  proceedToReset(): void {
    const token = this.resetResponse()?.resetToken;
    if (!token) return;
    this.router.navigate(['/auth/reset-password'], { queryParams: { token } });
  }
}
