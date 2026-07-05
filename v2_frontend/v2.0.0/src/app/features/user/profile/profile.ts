import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { UserService } from '../../../core/services/user.service';
import { AuthService } from '../../../core/services/auth.service';
import { User, UpdateUserRequest } from '../../../core/models/user.model';
import { FooterComponent } from '../../../shared/components/footer/footer';

@Component({
  selector: 'app-profile',
  imports: [ReactiveFormsModule, DatePipe, FooterComponent],
  templateUrl: './profile.html',
  styleUrl: './profile.css'
})
export class Profile implements OnInit {
  private userService  = inject(UserService);
  readonly authService = inject(AuthService);
  private fb           = inject(FormBuilder);

  user        = signal<User | null>(null);
  loading     = signal(true);
  error       = signal('');
  editMode    = signal(false);
  saving      = signal(false);
  saveError   = signal('');
  saveSuccess = signal('');

  selectedFile = signal<File | null>(null);
  previewUrl   = signal<string | null>(null);
  uploadError  = signal('');
  uploading    = signal(false);

  private readonly MAX_PICTURE_SIZE = 2 * 1024 * 1024;
  private readonly ALLOWED_PICTURE_TYPES = ['image/jpeg', 'image/png', 'image/webp'];

  // Three-way badge: platform admin > hospital-admin > plain member
  roleBadge = computed<{ label: string; dotClass: string; badgeClass: string }>(() => {
    if (this.authService.isAdmin()) {
      return { label: 'Administrator', dotClass: 'bg-purple-400', badgeClass: 'bg-purple-500/20 text-purple-300 ring-1 ring-purple-500/30' };
    }
    if (this.authService.isHospitalAdmin()) {
      return { label: 'Hospital Admin', dotClass: 'bg-blue-400', badgeClass: 'bg-blue-500/20 text-blue-300 ring-1 ring-blue-500/30' };
    }
    return { label: 'Member', dotClass: 'bg-teal-400', badgeClass: 'bg-teal-500/20 text-teal-300 ring-1 ring-teal-500/30' };
  });

  editForm = this.fb.group({
    firstName:          ['', Validators.required],
    lastName:           [''],
    userGender:         [''],
    userPhone:          ['', Validators.pattern(/^\d{10}$/)],
    userAddress:        [''],
    userPinCode:        ['', Validators.pattern(/^\d{6}$/)],
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
      firstName:          firstName ?? '',
      lastName:           rest.join(' '),
      userGender:         user.userGender,
      userPhone:          user.userPhone,
      userAddress:        user.userAddress,
      userPinCode:        user.userPinCode,
      profilePicturePath: user.profilePicturePath
    });
  }

  toggleEdit(): void {
    this.editMode.set(!this.editMode());
    this.saveError.set('');
    this.saveSuccess.set('');
    this.uploadError.set('');
    this.selectedFile.set(null);
    this.previewUrl.set(null);
  }

  onFileSelected(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0] ?? null;
    this.uploadError.set('');
    if (!file) return;

    if (!this.ALLOWED_PICTURE_TYPES.includes(file.type) || file.size > this.MAX_PICTURE_SIZE) {
      this.uploadError.set('Please upload a valid image file (JPG, PNG, or WebP) under 2MB.');
      return;
    }

    this.selectedFile.set(file);
    this.previewUrl.set(URL.createObjectURL(file));
  }

  uploadProfilePicture(): void {
    const file = this.selectedFile();
    const userId = this.authService.currentUser()?.userId;
    if (!file || !userId) return;

    this.uploading.set(true);
    this.uploadError.set('');

    this.userService.uploadProfilePicture(userId, file).subscribe({
      next: () => {
        this.uploading.set(false);
        this.selectedFile.set(null);
        this.previewUrl.set(null);
        this.ngOnInit();
      },
      error: (err) => {
        this.uploading.set(false);
        this.uploadError.set(err.error?.message ?? 'Upload failed. Please try again.');
      }
    });
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
