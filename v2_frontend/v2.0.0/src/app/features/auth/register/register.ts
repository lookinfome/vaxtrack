import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { UserService } from '../../../core/services/user.service';
import { CreateUserRequest } from '../../../core/models/user.model';

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

  form = this.fb.group({
    firstName: ['', Validators.required],
    lastName: [''],
    userBirthdate: ['', Validators.required],
    userGender: ['', Validators.required],
    userPhone: ['', [Validators.required, Validators.pattern(/^\d{10}$/)]],
    userAddress: ['', Validators.required],
    userPinCode: ['', [Validators.required, Validators.pattern(/^\d{6}$/)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]]
  });

  get controls() { return this.form.controls; }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.error.set('');

    const request = this.form.getRawValue() as CreateUserRequest;

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
