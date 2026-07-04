import { Component, inject, signal, OnInit, computed } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { UserService } from '../../../core/services/user.service';
import { AuthService } from '../../../core/services/auth.service';
import { BookingService } from '../../../core/services/booking.service';
import { HospitalService } from '../../../core/services/hospital.service';
import { User, UpdateUserRequest } from '../../../core/models/user.model';
import { Booking } from '../../../core/models/booking.model';
import { Hospital } from '../../../core/models/hospital.model';
import { FooterComponent } from '../../../shared/components/footer/footer';

@Component({
  selector: 'app-profile',
  imports: [ReactiveFormsModule, RouterLink, DatePipe, FooterComponent],
  templateUrl: './profile.html',
  styleUrl: './profile.css'
})
export class Profile implements OnInit {
  private userService     = inject(UserService);
  readonly authService    = inject(AuthService);
  private bookingService  = inject(BookingService);
  private hospitalService = inject(HospitalService);
  private router          = inject(Router);
  private fb              = inject(FormBuilder);

  user        = signal<User | null>(null);
  loading     = signal(true);
  error       = signal('');
  editMode    = signal(false);
  saving      = signal(false);
  saveError   = signal('');
  saveSuccess = signal('');

  booking   = signal<Booking | null>(null);
  hospitals = signal<Hospital[]>([]);

  vaccinationStatus = computed<'not-activated' | 'pending' | 'partial' | 'fully'>(() => {
    const b = this.booking();
    if (!b || b.isD1RequestCanceled) return 'not-activated';
    if (b.isVaccinationCompleted)    return 'fully';
    if (b.isDose1Completed)          return 'partial';
    return 'pending';
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

    if (!this.authService.isAdmin()) {
      const userUid = this.authService.currentUser()?.userUid ?? '';
      this.bookingService.getBookingsByUserId(userUid).subscribe({
        next:  (b)  => this.booking.set(b),
        error: ()   => this.booking.set(null)
      });

      this.hospitalService.getAllHospitals().subscribe({
        next:  (list) => this.hospitals.set(list),
        error: ()     => {}
      });
    }
  }

  hospitalName(uid: string): string {
    return this.hospitals().find(h => h.hospitalId === uid)?.hospitalName ?? '—';
  }

  slotTimeDisplay(n: number): string {
    const mins = 9 * 60 + ((n - 1) % 32) * 15;
    const h    = Math.floor(mins / 60);
    const m    = mins % 60;
    const ampm = h >= 12 ? 'PM' : 'AM';
    const h12  = h > 12 ? h - 12 : h === 0 ? 12 : h;
    return `${h12}:${m.toString().padStart(2, '0')} ${ampm}`;
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

  logout(): void {
    this.authService.logout().subscribe({
      next:  () => this.router.navigate(['/auth/login']),
      error: () => {
        this.authService.clearSession();
        this.router.navigate(['/auth/login']);
      }
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
