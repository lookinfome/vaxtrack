import { Component, inject, signal, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { UserService } from '../../../core/services/user.service';
import { AuthService } from '../../../core/services/auth.service';
import { User, UpdateUserRequest } from '../../../core/models/user.model';

@Component({
  selector: 'app-profile',
  imports: [ReactiveFormsModule, RouterLink, DatePipe],
  templateUrl: './profile.html',
  styleUrl: './profile.css'
})
export class Profile implements OnInit {
  private userService = inject(UserService);
  private authService = inject(AuthService);
  private fb = inject(FormBuilder);

  user = signal<User | null>(null);
  loading = signal(true);
  error = signal('');
  editMode = signal(false);
  saving = signal(false);
  saveError = signal('');
  saveSuccess = signal('');

  editForm = this.fb.group({
    firstName: ['', Validators.required],
    lastName: [''],
    userGender: [''],
    userPhone: ['', Validators.pattern(/^\d{10}$/)],
    userAddress: [''],
    userPinCode: ['', Validators.pattern(/^\d{6}$/)],
    profilePicturePath: ['']
  });

  ngOnInit(): void {
    const userId = this.authService.currentUser()?.userId;
    if (!userId) return;

    this.userService.getUserById(userId).subscribe({
      next: (data) => {
        this.user.set(data);
        this.loading.set(false);
        this.populateForm(data);
      },
      error: (err) => {
        this.error.set(err.error?.message ?? 'Failed to load profile.');
        this.loading.set(false);
        console.error('[Profile] getUserById failed:', err);
      }
    });
  }

  private populateForm(user: User): void {
    const [firstName, ...rest] = user.userName.split(' ');
    this.editForm.patchValue({
      firstName: firstName ?? '',
      lastName: rest.join(' '),
      userGender: user.userGender,
      userPhone: user.userPhone,
      userAddress: user.userAddress,
      userPinCode: user.userPinCode,
      profilePicturePath: user.profilePicturePath
    });
  }

  toggleEdit(): void {
    this.editMode.set(!this.editMode());
    this.saveError.set('');
    this.saveSuccess.set('');
  }

  onSave(): void {
    if (this.editForm.invalid) {
      this.editForm.markAllAsTouched();
      return;
    }

    const userId = this.authService.currentUser()?.userId;
    if (!userId) return;

    this.saving.set(true);
    this.saveError.set('');

    const request: UpdateUserRequest = {
      userId,
      ...(this.editForm.getRawValue() as Omit<UpdateUserRequest, 'userId'>)
    };

    this.userService.updateUser(request).subscribe({
      next: () => {
        this.saving.set(false);
        this.saveSuccess.set('Profile updated successfully.');
        this.editMode.set(false);
        this.ngOnInit();
      },
      error: (err) => {
        this.saving.set(false);
        this.saveError.set(err.error?.message ?? 'Failed to update profile.');
        console.error('[Profile] updateUser failed:', err);
      }
    });
  }
}
