import { Component, inject, signal, OnInit, computed } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { BookingService } from '../../../core/services/booking.service';
import { HospitalService } from '../../../core/services/hospital.service';
import { AuthService } from '../../../core/services/auth.service';
import { Booking, CreateBookingRequest, BookDose2Request, EditBookingRequest, RebookDose1Request } from '../../../core/models/booking.model';
import { Hospital } from '../../../core/models/hospital.model';
import { FooterComponent } from '../../../shared/components/footer/footer';
import { AuditTrailComponent } from '../../../shared/components/audit-trail/audit-trail';
import { ConfirmActionModalComponent } from '../../../shared/components/confirm-action-modal/confirm-action-modal';

type PageView = 'loading' | 'no-booking' | 'booking' | 'dose1-form' | 'dose2-form' | 'edit-form';

function weekdayValidator(control: AbstractControl): ValidationErrors | null {
  if (!control.value) return null;
  // Append T12:00:00 to avoid timezone-boundary day-shift
  const day = new Date(`${control.value}T12:00:00`).getDay();
  return day === 0 || day === 6 ? { weekend: true } : null;
}

function formatSlotTime(iso: string): string {
  const d = new Date(iso);
  let h = d.getHours();
  const m = d.getMinutes();
  const suffix = h >= 12 ? 'PM' : 'AM';
  const h12 = h % 12 || 12;
  return `${h12}:${String(m).padStart(2, '0')} ${suffix}`;
}

@Component({
  selector: 'app-my-bookings',
  imports: [ReactiveFormsModule, DatePipe, FooterComponent, AuditTrailComponent, ConfirmActionModalComponent],
  templateUrl: './my-bookings.html',
})
export class MyBookings implements OnInit {
  private bookingService = inject(BookingService);
  private hospitalService = inject(HospitalService);
  private authService   = inject(AuthService);
  private fb            = inject(FormBuilder);

  booking       = signal<Booking | null>(null);
  hospitals     = signal<Hospital[]>([]);
  view          = signal<PageView>('loading');
  actionLoading = signal(false);
  actionError   = signal('');
  actionSuccess = signal('');
  showCancelConfirm = signal(false);

  // true when the dose1-form view is being used to rebook a cancelled/rejected Dose 1
  // rather than book a brand-new Dose 1
  isRebooking = signal(false);

  // Which dose is being edited (1 or 2)
  editDoseNumber = signal<1 | 2>(1);
  selectedEditHospitalId = signal('');

  editForm = this.fb.group({
    editHospitalUid: ['', Validators.required],
    editDate:        ['', [Validators.required, weekdayValidator]],
  });

  // Signals that mirror the hospital dropdown selections so computed() can react
  selectedDose1HospitalId = signal('');
  selectedDose2HospitalId = signal('');

  dose1Form = this.fb.group({
    dose1HospitalUid: ['', Validators.required],
    dose1Date:        ['', [Validators.required, weekdayValidator]],
  });

  dose2Form = this.fb.group({
    dose2HospitalUid: ['', Validators.required],
    dose2Date:        ['', [Validators.required, weekdayValidator]],
  });

  // userUid (GUID) is always read from the JWT session — matches the JWT sub claim used by the backend
  private get userUid(): string {
    return this.authService.currentUser()?.userUid ?? '';
  }

  // ── Server-computed slot preview (Phase 8) ────────────────────────────
  // These signals are populated by GetNextAvailableSlotAsync — a read-only "peek" that
  // computes but does not persist anything. The server remains the sole source of truth;
  // this only preserves the existing reactive "info card" UX.

  dose1SlotPreview = signal<{ slotNumber: number; slotDateTime: string } | null>(null);
  dose1PreviewLoading = signal(false);
  dose1PreviewError = signal('');

  dose2SlotPreview = signal<{ slotNumber: number; slotDateTime: string } | null>(null);
  dose2PreviewLoading = signal(false);
  dose2PreviewError = signal('');

  editSlotPreview = signal<{ slotNumber: number; slotDateTime: string } | null>(null);
  editPreviewLoading = signal(false);
  editPreviewError = signal('');

  dose1SlotTimeDisplay = computed(() => {
    const p = this.dose1SlotPreview();
    return p ? formatSlotTime(p.slotDateTime) : '';
  });

  dose2SlotTimeDisplay = computed(() => {
    const p = this.dose2SlotPreview();
    return p ? formatSlotTime(p.slotDateTime) : '';
  });

  editSlotTimeDisplay = computed(() => {
    const p = this.editSlotPreview();
    return p ? formatSlotTime(p.slotDateTime) : '';
  });

  private refreshSlotPreview(
    hospitalId: string,
    date: string,
    preview: ReturnType<typeof signal<{ slotNumber: number; slotDateTime: string } | null>>,
    loading: ReturnType<typeof signal<boolean>>,
    previewError: ReturnType<typeof signal<string>>
  ): void {
    if (!hospitalId || !date) {
      preview.set(null);
      previewError.set('');
      return;
    }

    loading.set(true);
    previewError.set('');

    this.bookingService.getNextAvailableSlot(hospitalId, date).subscribe({
      next: (slot) => {
        loading.set(false);
        preview.set(slot);
      },
      error: (err) => {
        loading.set(false);
        preview.set(null);
        previewError.set(err.error?.message ?? 'This hospital has no available slots for that date. Please choose another.');
      }
    });
  }

  private refreshDose1Preview(): void {
    const raw = this.dose1Form.getRawValue();
    this.refreshSlotPreview(raw.dose1HospitalUid ?? '', raw.dose1Date ?? '', this.dose1SlotPreview, this.dose1PreviewLoading, this.dose1PreviewError);
  }

  private refreshDose2Preview(): void {
    const raw = this.dose2Form.getRawValue();
    this.refreshSlotPreview(raw.dose2HospitalUid ?? '', raw.dose2Date ?? '', this.dose2SlotPreview, this.dose2PreviewLoading, this.dose2PreviewError);
  }

  // The booking's own hospital+date at the moment the edit form was opened — used to detect
  // whether the user has actually changed anything yet (see refreshEditPreview below).
  private originalEditHospitalUid = '';
  private originalEditDate = '';

  private refreshEditPreview(): void {
    const raw = this.editForm.getRawValue();
    const hospitalUid = raw.editHospitalUid ?? '';
    const date = raw.editDate ?? '';

    // Nothing has changed yet — EditBookingAsync only recomputes the slot when the hospital
    // or date actually changes, so show the booking's existing slot instead of calling the
    // preview endpoint (which would otherwise suggest a "next" slot that won't actually be saved).
    if (hospitalUid === this.originalEditHospitalUid && date === this.originalEditDate) {
      const b = this.booking();
      if (b) {
        const existingDateTime = this.editDoseNumber() === 1 ? b.dose1RequestedDateTime : b.dose2RequestedDateTime;
        const existingSlotNumber = this.editDoseNumber() === 1 ? b.dose1SlotNumber : b.dose2SlotNumber;
        this.editPreviewLoading.set(false);
        this.editPreviewError.set('');
        this.editSlotPreview.set(existingDateTime ? { slotNumber: existingSlotNumber, slotDateTime: existingDateTime } : null);
      }
      return;
    }

    this.refreshSlotPreview(hospitalUid, date, this.editSlotPreview, this.editPreviewLoading, this.editPreviewError);
  }

  // ── Edit computeds ────────────────────────────────────────────────────

  editHospital = computed(() =>
    this.hospitals().find(h => h.hospitalId === this.selectedEditHospitalId()) ?? null
  );

  canEditDose1 = computed(() => {
    const b = this.booking();
    if (!b) return false;
    return !b.isDose1Completed && !b.isD1RequestCanceled;
  });

  canEditDose2 = computed(() => {
    const b = this.booking();
    if (!b) return false;
    return b.isDose1Completed && !!b.dose2HospitalUid && !b.isDose2Completed && !b.isD2RequestCanceled;
  });

  // ── Booking state ─────────────────────────────────────────────────────

  canCancel = computed(() => {
    const b = this.booking();
    if (!b) return false;
    if (!b.isDose1Completed && !b.isD1RequestCanceled) return true;
    if (b.isDose1Completed && b.dose2HospitalUid && !b.isDose2Completed && !b.isD2RequestCanceled) return true;
    return false;
  });

  canBookDose2 = computed(() => {
    const b = this.booking();
    if (!b) return false;
    return b.isDose1Completed && !b.dose2HospitalUid && !b.isVaccinationCompleted && !b.isD1RequestCanceled;
  });

  // A cancelled/rejected Dose 1 with no Dose 2 yet is otherwise a dead end — this lets the
  // user book again instead of being stuck.
  canRebookDose1 = computed(() => {
    const b = this.booking();
    if (!b) return false;
    return b.isD1RequestCanceled && !b.isDose1Completed && !b.dose2HospitalUid;
  });

  cancelTarget = computed(() => {
    const b = this.booking();
    if (!b) return '';
    if (!b.isDose1Completed && !b.isD1RequestCanceled) return 'Dose 1';
    if (b.isDose1Completed && b.dose2HospitalUid && !b.isDose2Completed && !b.isD2RequestCanceled) return 'Dose 2';
    return '';
  });

  dose1Status = computed((): 'pending' | 'completed' | 'canceled' | 'rejected' => {
    const b = this.booking();
    if (!b) return 'pending';
    if (b.isD1RequestCanceled) return b.isD1RejectedByAdmin ? 'rejected' : 'canceled';
    if (b.isDose1Completed)    return 'completed';
    return 'pending';
  });

  dose2Status = computed((): 'none' | 'pending' | 'completed' | 'canceled' | 'rejected' => {
    const b = this.booking();
    if (!b || !b.dose2HospitalUid) return 'none';
    if (b.isD2RequestCanceled) return b.isD2RejectedByAdmin ? 'rejected' : 'canceled';
    if (b.isDose2Completed)    return 'completed';
    return 'pending';
  });

  // Minimum selectable date = tomorrow
  get minDate(): string {
    const d = new Date();
    d.setDate(d.getDate() + 1);
    return d.toISOString().split('T')[0];
  }

  ngOnInit(): void {
    this.loadHospitals();
    this.loadBooking();

    // Keep selected-hospital signals in sync with form controls, and refresh the
    // server-computed slot preview whenever hospital or date changes.
    this.dose1Form.get('dose1HospitalUid')!.valueChanges.subscribe(id => {
      this.selectedDose1HospitalId.set(id ?? '');
      this.refreshDose1Preview();
    });
    this.dose1Form.get('dose1Date')!.valueChanges.subscribe(() => this.refreshDose1Preview());

    this.dose2Form.get('dose2HospitalUid')!.valueChanges.subscribe(id => {
      this.selectedDose2HospitalId.set(id ?? '');
      this.refreshDose2Preview();
    });
    this.dose2Form.get('dose2Date')!.valueChanges.subscribe(() => this.refreshDose2Preview());

    this.editForm.get('editHospitalUid')!.valueChanges.subscribe(id => {
      this.selectedEditHospitalId.set(id ?? '');
      this.refreshEditPreview();
    });
    this.editForm.get('editDate')!.valueChanges.subscribe(() => this.refreshEditPreview());
  }

  private loadHospitals(): void {
    this.hospitalService.getAllHospitals().subscribe({
      next:  (list) => this.hospitals.set(list),
      error: ()     => this.hospitals.set([])
    });
  }

  private loadBooking(): void {
    this.view.set('loading');
    this.bookingService.getBookingsByUserId(this.userUid).subscribe({
      next: (booking) => { this.booking.set(booking); this.view.set('booking'); },
      error: () => { this.booking.set(null); this.view.set('no-booking'); }
    });
  }

  hospitalName(uid: string): string {
    return this.hospitals().find(h => h.hospitalId === uid)?.hospitalName ?? uid;
  }

  // Disabled/pending centres are hidden from new-booking pickers, but the raw hospitals()
  // list is kept unfiltered so hospitalName() above can still resolve names for historical
  // bookings made at a centre that has since been disabled or unregistered.
  bookableHospitals = computed(() => this.hospitals().filter(h => h.status === 'Active'));

  // The edit dropdown must also include the booking's *current* hospital even if it's no
  // longer Active, otherwise the pre-filled selection would render blank.
  editableHospitals = computed(() => {
    const active = this.bookableHospitals();
    const currentUid = this.originalEditHospitalUid;
    if (!currentUid || active.some(h => h.hospitalId === currentUid)) return active;
    const current = this.hospitals().find(h => h.hospitalId === currentUid);
    return current ? [current, ...active] : active;
  });

  showDose1Form(): void {
    this.actionError.set('');
    this.isRebooking.set(false);
    this.selectedDose1HospitalId.set('');
    this.dose1SlotPreview.set(null);
    this.dose1Form.reset();
    this.view.set('dose1-form');
  }

  showRebookForm(): void {
    this.actionError.set('');
    this.isRebooking.set(true);
    this.selectedDose1HospitalId.set('');
    this.dose1SlotPreview.set(null);
    this.dose1Form.reset();
    this.view.set('dose1-form');
  }

  showDose2Form(): void {
    this.actionError.set('');
    this.selectedDose2HospitalId.set('');
    this.dose2SlotPreview.set(null);
    this.dose2Form.reset();
    this.view.set('dose2-form');
  }

  backToView(): void {
    this.actionError.set('');
    this.isRebooking.set(false);
    this.view.set(this.booking() ? 'booking' : 'no-booking');
  }

  submitDose1(): void {
    if (this.dose1Form.invalid) { this.dose1Form.markAllAsTouched(); return; }

    const preview = this.dose1SlotPreview();
    if (!preview) {
      this.actionError.set('Selected hospital has no available slots for that date.');
      return;
    }

    const raw = this.dose1Form.getRawValue();

    this.actionLoading.set(true);
    this.actionError.set('');

    // userUid sourced from auth session — never from form input.
    // Slot number/time are server-computed — only the requested date is sent.
    const request: CreateBookingRequest = {
      userUid:                this.userUid,
      dose1HospitalUid:       raw.dose1HospitalUid!,
      dose1RequestedDateTime: `${raw.dose1Date}T00:00:00`,
    };

    this.bookingService.createBooking(request).subscribe({
      next:  () => { this.actionLoading.set(false); this.loadBooking(); },
      error: (err) => {
        this.actionLoading.set(false);
        this.actionError.set(err.error?.message ?? 'Booking failed. Please try again.');
      }
    });
  }

  submitRebook(): void {
    const booking = this.booking();
    if (!booking || this.dose1Form.invalid) { this.dose1Form.markAllAsTouched(); return; }

    const preview = this.dose1SlotPreview();
    if (!preview) {
      this.actionError.set('Selected hospital has no available slots for that date.');
      return;
    }

    const raw = this.dose1Form.getRawValue();

    this.actionLoading.set(true);
    this.actionError.set('');

    const request: RebookDose1Request = {
      bookingId:            booking.bookingId,
      userUid:              this.userUid,
      newHospitalUid:       raw.dose1HospitalUid!,
      newRequestedDateTime: `${raw.dose1Date}T00:00:00`,
    };

    this.bookingService.rebookDose1(request).subscribe({
      next: (updated) => {
        this.booking.set(updated);
        this.actionLoading.set(false);
        this.isRebooking.set(false);
        this.actionSuccess.set('Booking rescheduled successfully.');
        this.view.set('booking');
        setTimeout(() => this.actionSuccess.set(''), 5000);
      },
      error: (err) => {
        this.actionLoading.set(false);
        this.actionError.set(err.error?.message ?? 'Rebooking failed. Please try again.');
      }
    });
  }

  submitDose2(): void {
    const booking = this.booking();
    if (!booking || this.dose2Form.invalid) { this.dose2Form.markAllAsTouched(); return; }

    const preview = this.dose2SlotPreview();
    if (!preview) {
      this.actionError.set('Selected hospital has no available slots for that date.');
      return;
    }

    const raw = this.dose2Form.getRawValue();

    this.actionLoading.set(true);
    this.actionError.set('');

    // userUid sourced from auth session — never from form input
    const request: BookDose2Request = {
      bookingId:               booking.bookingId,
      userUid:                 this.userUid,
      dose2HospitalUid:        raw.dose2HospitalUid!,
      dose2RequestedDateTime:  `${raw.dose2Date}T00:00:00`,
    };

    this.bookingService.bookDose2(request).subscribe({
      next:  () => { this.actionLoading.set(false); this.loadBooking(); },
      error: (err) => {
        this.actionLoading.set(false);
        this.actionError.set(err.error?.message ?? 'Dose 2 booking failed. Please try again.');
      }
    });
  }

  showEditForm(doseNumber: 1 | 2): void {
    const b = this.booking();
    if (!b) return;
    this.actionError.set('');
    this.editDoseNumber.set(doseNumber);

    const currentHospitalUid = doseNumber === 1 ? b.dose1HospitalUid : b.dose2HospitalUid;
    const currentDate = doseNumber === 1
      ? b.dose1RequestedDateTime.split('T')[0]
      : (b.dose2RequestedDateTime ?? '').split('T')[0];

    // Pre-populate form with current booking values
    this.originalEditHospitalUid = currentHospitalUid;
    this.originalEditDate = currentDate;
    this.editForm.reset({ editHospitalUid: currentHospitalUid, editDate: currentDate });
    this.selectedEditHospitalId.set(currentHospitalUid);
    this.view.set('edit-form');
  }

  submitEdit(): void {
    const booking = this.booking();
    if (!booking || this.editForm.invalid) { this.editForm.markAllAsTouched(); return; }

    const preview = this.editSlotPreview();
    if (!preview) {
      this.actionError.set('Selected hospital has no available slots for that date.');
      return;
    }

    const raw = this.editForm.getRawValue();

    this.actionLoading.set(true);
    this.actionError.set('');

    const request: EditBookingRequest = {
      bookingId:            booking.bookingId,
      userUid:              this.userUid,
      doseNumber:           this.editDoseNumber(),
      newHospitalUid:       raw.editHospitalUid!,
      newRequestedDateTime: `${raw.editDate}T00:00:00`,
    };

    this.bookingService.editBooking(request).subscribe({
      next: (updated) => {
        this.booking.set(updated);
        this.actionLoading.set(false);
        this.actionSuccess.set('Booking updated successfully.');
        this.view.set('booking');
        setTimeout(() => this.actionSuccess.set(''), 5000);
      },
      error: (err) => {
        this.actionLoading.set(false);
        this.actionError.set(err.error?.message ?? 'Edit failed. Please try again.');
      }
    });
  }

  confirmCancel(): void { this.showCancelConfirm.set(true); }
  dismissCancel(): void { this.showCancelConfirm.set(false); }

  executeCancel(): void {
    const booking = this.booking();
    if (!booking) return;
    this.actionLoading.set(true);
    this.actionError.set('');
    this.showCancelConfirm.set(false);

    // Only bookingId is sent — backend validates ownership via JWT sub claim
    this.bookingService.cancelBooking(booking.bookingId).subscribe({
      next: (updated) => {
        this.booking.set(updated);
        this.actionLoading.set(false);
        this.actionSuccess.set('Booking cancelled successfully.');
        setTimeout(() => this.actionSuccess.set(''), 5000);
      },
      error: (err) => {
        this.actionLoading.set(false);
        this.actionError.set(err.error?.message ?? 'Cancellation failed. Please try again.');
      }
    });
  }
}
