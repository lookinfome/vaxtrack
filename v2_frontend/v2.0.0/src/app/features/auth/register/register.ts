import { Component, inject, signal } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { UserService } from '../../../core/services/user.service';
import { CreateUserRequest } from '../../../core/models/user.model';

function passwordsMatch(): ValidatorFn {
  return (group: AbstractControl): ValidationErrors | null => {
    const pw = group.get('password')?.value;
    const confirm = group.get('confirmPassword')?.value;
    return pw === confirm ? null : { passwordMismatch: true };
  };
}

@Component({
  selector: 'app-register',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './register.html',
  styleUrl: './register.css'
})
export class Register {
  private fb = inject(FormBuilder);
  private userService = inject(UserService);
  private router = inject(Router);

  loading = signal(false);
  error = signal('');
  showPassword = signal(false);
  showConfirmPassword = signal(false);

  form = this.fb.group({
    firstName: ['', Validators.required],
    lastName: [''],
    userBirthdate: ['', Validators.required],
    userGender: ['', Validators.required],
    userPhone: ['', [Validators.required, Validators.pattern(/^\d{10}$/)]],
    userAddress: ['', Validators.required],
    userPinCode: ['', [Validators.required, Validators.pattern(/^\d{6}$/)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]],
    confirmPassword: ['', Validators.required]
  }, { validators: passwordsMatch() });

  get controls() { return this.form.controls; }

  get confirmMismatch(): boolean {
    return this.form.hasError('passwordMismatch') && this.controls['confirmPassword'].touched;
  }

  passwordStrength(): 'poor' | 'weak' | 'fair' | 'strong' | null {
    const pw = this.controls['password'].value ?? '';
    if (pw.length === 0) return null;
    if (pw.length < 8) return 'poor';
    const hasUpper = /[A-Z]/.test(pw);
    const hasNumber = /\d/.test(pw);
    const hasSpecial = /[^A-Za-z0-9]/.test(pw);
    const score = [hasUpper, hasNumber, hasSpecial].filter(Boolean).length;
    if (score <= 1) return 'weak';
    if (score === 2) return 'fair';
    return 'strong';
  }

  strengthBarClass(): string {
    const s = this.passwordStrength();
    const map: Record<string, string> = {
      poor: 'w-1/4 bg-red-500',
      weak: 'w-2/4 bg-orange-400',
      fair: 'w-3/4 bg-yellow-400',
      strong: 'w-full bg-green-500'
    };
    return s ? map[s] : '';
  }

  strengthLabelClass(): string {
    const s = this.passwordStrength();
    const map: Record<string, string> = {
      poor: 'text-xs font-medium text-red-500',
      weak: 'text-xs font-medium text-orange-400',
      fair: 'text-xs font-medium text-yellow-500',
      strong: 'text-xs font-medium text-green-600'
    };
    return s ? map[s] : '';
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.error.set('');

    const { confirmPassword: _, ...rest } = this.form.getRawValue();
    const request = rest as CreateUserRequest;

    this.userService.createUser(request).subscribe({
      next: () => {
        this.loading.set(false);
        this.router.navigate(['/auth/login']);
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err.error?.message ?? 'Registration failed. Please try again.');
        console.error('[Register] createUser failed:', err);
      }
    });
  }
}
