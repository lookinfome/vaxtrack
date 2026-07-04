import { Component, inject, signal, OnInit, computed } from '@angular/core';
import { AbstractControl, FormBuilder, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { BookingService } from '../../../core/services/booking.service';
import { HospitalService } from '../../../core/services/hospital.service';
import { AuthService } from '../../../core/services/auth.service';
import { Booking, CreateBookingRequest, BookDose2Request, EditBookingRequest } from '../../../core/models/booking.model';
import { Hospital } from '../../../core/models/hospital.model';
import { FooterComponent } from '../../../shared/components/footer/footer';

type PageView = 'loading' | 'no-booking' | 'booking' | 'dose1-form' | 'dose2-form' | 'edit-form';

const WORK_START_HOUR = 9;  // 9 AM
const SLOT_MINUTES    = 15;
const SLOTS_PER_DAY   = 32; // 9 AM–5 PM = 8 h = 32 × 15 min

function weekdayValidator(control: AbstractControl): ValidationErrors | null {
  if (!control.value) return null;
  // Append T12:00:00 to avoid timezone-boundary day-shift
  const day = new Date(`${control.value}T12:00:00`).getDay();
  return day === 0 || day === 6 ? { weekend: true } : null;
}

function slotTime(slotNumber: number): string {
  const mins = ((slotNumber - 1) % SLOTS_PER_DAY) * SLOT_MINUTES;
  const h = Math.floor(mins / 60) + WORK_START_HOUR;
  const m = mins % 60;
  return `${String(h).padStart(2, '0')}:${String(m).padStart(2, '0')}`;
}

@Component({
  selector: 'app-my-bookings',
  imports: [ReactiveFormsModule, RouterLink, DatePipe, FooterComponent],
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

  // ── Slot computation ──────────────────────────────────────────────────

  dose1Hospital = computed(() =>
    this.hospitals().find(h => h.hospitalId === this.selectedDose1HospitalId()) ?? null
  );

  dose1SlotNumber = computed(() => {
    const h = this.dose1Hospital();
    return h && h.slotsAvailable > 0 ? h.totalSlots - h.slotsAvailable + 1 : null;
  });

  dose1SlotTime = computed(() => {
    const s = this.dose1SlotNumber();
    return s !== null ? slotTime(s) : '';
  });

  dose1SlotTimeDisplay = computed(() => {
    const t = this.dose1SlotTime();
    if (!t) return '';
    const [h, m] = t.split(':').map(Number);
    const suffix = h >= 12 ? 'PM' : 'AM';
    const h12   = h % 12 || 12;
    return `${h12}:${String(m).padStart(2, '0')} ${suffix}`;
  });

  dose2Hospital = computed(() =>
    this.hospitals().find(h => h.hospitalId === this.selectedDose2HospitalId()) ?? null
  );

  dose2SlotNumber = computed(() => {
    const h = this.dose2Hospital();
    return h && h.slotsAvailable > 0 ? h.totalSlots - h.slotsAvailable + 1 : null;
  });

  dose2SlotTime = computed(() => {
    const s = this.dose2SlotNumber();
    return s !== null ? slotTime(s) : '';
  });

  dose2SlotTimeDisplay = computed(() => {
    const t = this.dose2SlotTime();
    if (!t) return '';
    const [h, m] = t.split(':').map(Number);
    const suffix = h >= 12 ? 'PM' : 'AM';
    const h12   = h % 12 || 12;
    return `${h12}:${String(m).padStart(2, '0')} ${suffix}`;
  });

  // ── Edit computeds ────────────────────────────────────────────────────

  editHospital = computed(() =>
    this.hospitals().find(h => h.hospitalId === this.selectedEditHospitalId()) ?? null
  );

  editSlotNumber = computed(() => {
    const b    = this.booking();
    const dose = this.editDoseNumber();
    const h    = this.editHospital();
    if (!h) return null;

    const currentHospitalUid = dose === 1 ? b?.dose1HospitalUid : b?.dose2HospitalUid;
    const currentSlotNumber  = dose === 1 ? b?.dose1SlotNumber  : b?.dose2SlotNumber;

    // Same hospital — slot already reserved, reuse existing slot number
    if (h.hospitalId === currentHospitalUid) return currentSlotNumber ?? null;

    // Different hospital — next available slot
    return h.slotsAvailable > 0 ? h.totalSlots - h.slotsAvailable + 1 : null;
  });

  editSlotTime = computed(() => {
    const s = this.editSlotNumber();
    return s !== null ? slotTime(s) : '';
  });

  editSlotTimeDisplay = computed(() => {
    const t = this.editSlotTime();
    if (!t) return '';
    const [h, m] = t.split(':').map(Number);
    const suffix = h >= 12 ? 'PM' : 'AM';
    const h12   = h % 12 || 12;
    return `${h12}:${String(m).padStart(2, '0')} ${suffix}`;
  });

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

  cancelTarget = computed(() => {
    const b = this.booking();
    if (!b) return '';
    if (!b.isDose1Completed && !b.isD1RequestCanceled) return 'Dose 1';
    if (b.isDose1Completed && b.dose2HospitalUid && !b.isDose2Completed && !b.isD2RequestCanceled) return 'Dose 2';
    return '';
  });

  dose1Status = computed((): 'pending' | 'completed' | 'canceled' => {
    const b = this.booking();
    if (!b) return 'pending';
    if (b.isD1RequestCanceled) return 'canceled';
    if (b.isDose1Completed)    return 'completed';
    return 'pending';
  });

  dose2Status = computed((): 'none' | 'pending' | 'completed' | 'canceled' => {
    const b = this.booking();
    if (!b || !b.dose2HospitalUid) return 'none';
    if (b.isD2RequestCanceled) return 'canceled';
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

    // Keep selected-hospital signals in sync with form controls
    this.dose1Form.get('dose1HospitalUid')!.valueChanges
      .subscribe(id => this.selectedDose1HospitalId.set(id ?? ''));

    this.dose2Form.get('dose2HospitalUid')!.valueChanges
      .subscribe(id => this.selectedDose2HospitalId.set(id ?? ''));

    this.editForm.get('editHospitalUid')!.valueChanges
      .subscribe(id => this.selectedEditHospitalId.set(id ?? ''));
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

  showDose1Form(): void {
    this.actionError.set('');
    this.selectedDose1HospitalId.set('');
    this.dose1Form.reset();
    this.view.set('dose1-form');
  }

  showDose2Form(): void {
    this.actionError.set('');
    this.selectedDose2HospitalId.set('');
    this.dose2Form.reset();
    this.view.set('dose2-form');
  }

  backToView(): void {
    this.actionError.set('');
    this.view.set(this.booking() ? 'booking' : 'no-booking');
  }

  submitDose1(): void {
    if (this.dose1Form.invalid) { this.dose1Form.markAllAsTouched(); return; }

    const slotNum  = this.dose1SlotNumber();
    const slotT    = this.dose1SlotTime();
    if (slotNum === null || !slotT) {
      this.actionError.set('Selected hospital has no available slots.');
      return;
    }

    const raw = this.dose1Form.getRawValue();
    const isoDateTime = new Date(`${raw.dose1Date}T${slotT}:00`).toISOString();

    this.actionLoading.set(true);
    this.actionError.set('');

    // userUid sourced from auth session — never from form input
    const request: CreateBookingRequest = {
      userUid:                  this.userUid,
      dose1HospitalUid:         raw.dose1HospitalUid!,
      dose1RequestedDateTime:   isoDateTime,
      dose1SlotNumber:          slotNum,
    };

    this.bookingService.createBooking(request).subscribe({
      next:  () => { this.actionLoading.set(false); this.loadBooking(); },
      error: (err) => {
        this.actionLoading.set(false);
        this.actionError.set(err.error?.message ?? 'Booking failed. Please try again.');
      }
    });
  }

  submitDose2(): void {
    const booking = this.booking();
    if (!booking || this.dose2Form.invalid) { this.dose2Form.markAllAsTouched(); return; }

    const slotNum = this.dose2SlotNumber();
    const slotT   = this.dose2SlotTime();
    if (slotNum === null || !slotT) {
      this.actionError.set('Selected hospital has no available slots.');
      return;
    }

    const raw = this.dose2Form.getRawValue();
    const isoDateTime = new Date(`${raw.dose2Date}T${slotT}:00`).toISOString();

    this.actionLoading.set(true);
    this.actionError.set('');

    // userUid sourced from auth session — never from form input
    const request: BookDose2Request = {
      bookingId:               booking.bookingId,
      userUid:                 this.userUid,
      dose2HospitalUid:        raw.dose2HospitalUid!,
      dose2RequestedDateTime:  isoDateTime,
      dose2SlotNumber:         slotNum,
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
    this.editForm.reset({ editHospitalUid: currentHospitalUid, editDate: currentDate });
    this.selectedEditHospitalId.set(currentHospitalUid);
    this.view.set('edit-form');
  }

  submitEdit(): void {
    const booking = this.booking();
    if (!booking || this.editForm.invalid) { this.editForm.markAllAsTouched(); return; }

    const slotNum = this.editSlotNumber();
    const slotT   = this.editSlotTime();
    if (slotNum === null || !slotT) {
      this.actionError.set('Selected hospital has no available slots.');
      return;
    }

    const raw = this.editForm.getRawValue();
    const isoDateTime = new Date(`${raw.editDate}T${slotT}:00`).toISOString();

    this.actionLoading.set(true);
    this.actionError.set('');

    const request: EditBookingRequest = {
      bookingId:            booking.bookingId,
      userUid:              this.userUid,
      doseNumber:           this.editDoseNumber(),
      newHospitalUid:       raw.editHospitalUid!,
      newSlotNumber:        slotNum,
      newRequestedDateTime: isoDateTime,
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
