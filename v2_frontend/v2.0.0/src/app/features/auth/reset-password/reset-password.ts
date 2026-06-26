import { Component, inject, signal, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink, ActivatedRoute } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-reset-password',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './reset-password.html',
  styleUrl: './reset-password.css'
})
export class ResetPassword implements OnInit {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  loading = signal(false);
  error = signal('');
  success = signal(false);

  form = this.fb.group({
    resetToken: ['', Validators.required],
    newPassword: ['', [Validators.required, Validators.minLength(8)]]
  });

  get tokenControl() { return this.form.get('resetToken')!; }
  get passwordControl() { return this.form.get('newPassword')!; }

  ngOnInit(): void {
    const token = this.route.snapshot.queryParamMap.get('token');
    if (token) {
      this.form.patchValue({ resetToken: token });
    }
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.error.set('');

    const { resetToken, newPassword } = this.form.getRawValue();

    this.authService.resetForgottenPassword({
      resetToken: resetToken!,
      newPassword: newPassword!
    }).subscribe({
      next: () => {
        this.loading.set(false);
        this.success.set(true);
        setTimeout(() => this.router.navigate(['/auth/login']), 2000);
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err.error?.message ?? 'Failed to reset password. Please try again.');
        console.error('[ResetPassword] resetForgottenPassword failed:', err);
      }
    });
  }
}
