import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { FooterComponent } from '../../shared/components/footer/footer';

@Component({
  selector: 'app-account-reactivation',
  imports: [ReactiveFormsModule, RouterLink, FooterComponent],
  templateUrl: './account-reactivation.html',
})
export class AccountReactivation {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);

  loading = signal(false);
  submitted = signal(false);
  responseMessage = signal('');

  form = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    reason: ['']
  });

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    const raw = this.form.getRawValue();

    this.authService.requestAccountReactivation({ email: raw.email!, reason: raw.reason || undefined }).subscribe({
      next: (res) => {
        this.loading.set(false);
        this.submitted.set(true);
        this.responseMessage.set(res.message);
      },
      error: () => {
        this.loading.set(false);
        this.submitted.set(true);
        // Same generic message either way — matches the backend's anti-enumeration design.
        this.responseMessage.set('If this account is disabled, your reactivation request has been submitted for review.');
      }
    });
  }
}
