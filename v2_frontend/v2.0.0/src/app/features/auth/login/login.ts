import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { LoginRequest } from '../../../core/models/auth.model';
import { FooterComponent } from '../../../shared/components/footer/footer';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, RouterLink, FooterComponent],
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
  disabledReason = signal<string | null>(null);
  showPassword = signal(false);

  get emailControl() { return this.form.get('email')!; }
  get passwordControl() { return this.form.get('password')!; }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.error.set('');
    this.disabledReason.set(null);

    const request = this.form.getRawValue() as LoginRequest;

    this.authService.login(request).subscribe({
      next: () => {
        this.loading.set(false);
        // Every role lands on Profile first — the nav bar handles further navigation from there.
        this.router.navigate(['/user']);
      },
      error: (err) => {
        this.loading.set(false);
        if (err.error?.code === 'ACCOUNT_DISABLED') {
          this.disabledReason.set(err.error?.reason ?? 'No reason was given.');
        } else {
          this.error.set(err.error?.message ?? 'Login failed. Please try again.');
        }
        console.error('[Login] login failed:', err);
      }
    });
  }
}
