import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { BookingService } from '../../../core/services/booking.service';
import { HospitalService } from '../../../core/services/hospital.service';
import { Booking } from '../../../core/models/booking.model';
import { Hospital } from '../../../core/models/hospital.model';
import { FooterComponent } from '../../../shared/components/footer/footer';
import { AuditTrailComponent } from '../../../shared/components/audit-trail/audit-trail';
import { ConfirmActionModalComponent } from '../../../shared/components/confirm-action-modal/confirm-action-modal';

type PendingAction = 'approve' | 'reject' | 'cancel';

@Component({
  selector: 'app-bookings-management',
  imports: [DatePipe, FooterComponent, AuditTrailComponent, ConfirmActionModalComponent],
  templateUrl: './bookings-management.html',
})
export class BookingsManagement implements OnInit {
  private bookingService = inject(BookingService);
  private hospitalService = inject(HospitalService);

  bookings = signal<Booking[]>([]);
  hospitals = signal<Hospital[]>([]);
  loading = signal(true);
  error = signal('');
  actionLoading = signal(false);
  actionError = signal('');
  actionSuccess = signal('');

  expandedBookingId = signal<string | null>(null);

  pendingAction = signal<{ bookingId: string; action: PendingAction } | null>(null);

  ngOnInit(): void {
    this.loadBookings();
    this.hospitalService.getAllHospitals().subscribe({
      next: (list) => this.hospitals.set(list),
      error: () => {}
    });
  }

  private loadBookings(): void {
    this.loading.set(true);
    this.error.set('');
    this.bookingService.getActionableBookings().subscribe({
      next: (list) => { this.loading.set(false); this.bookings.set(list); },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err.error?.message ?? 'Failed to load bookings.');
      }
    });
  }

  hospitalName(uid: string): string {
    return this.hospitals().find(h => h.hospitalId === uid)?.hospitalName ?? uid;
  }

  // Mirrors the backend's ApproveBookingsAsync priority logic — whichever dose is still
  // pending is the one any Approve/Reject/Cancel action will target.
  actionableDoseNumber(b: Booking): 1 | 2 {
    if (!b.isDose1Completed && !b.isD1RequestCanceled) return 1;
    return 2;
  }

  toggleHistory(bookingId: string): void {
    this.expandedBookingId.set(this.expandedBookingId() === bookingId ? null : bookingId);
  }

  requestAction(bookingId: string, action: PendingAction): void {
    this.actionError.set('');
    this.pendingAction.set({ bookingId, action });
  }

  dismissAction(): void {
    this.pendingAction.set(null);
  }

  get modalTitle(): string {
    const action = this.pendingAction()?.action;
    if (action === 'approve') return 'Approve this booking?';
    if (action === 'reject') return 'Reject this booking?';
    return 'Cancel this booking?';
  }

  get modalMessage(): string {
    const action = this.pendingAction()?.action;
    if (action === 'approve') return 'This confirms the dose was administered to the user.';
    if (action === 'reject') return 'The user will see this as rejected by the hospital, and can rebook. Please explain why.';
    return 'This releases the hospital slot back to the pool.';
  }

  confirmAction(comment: string | undefined): void {
    const pending = this.pendingAction();
    if (!pending) return;

    this.actionLoading.set(true);
    this.actionError.set('');
    this.pendingAction.set(null);

    const { bookingId, action } = pending;
    const call$ =
      action === 'approve' ? this.bookingService.approveBooking(bookingId, comment) :
      action === 'reject'  ? this.bookingService.rejectBooking(bookingId, comment) :
                              this.bookingService.cancelBooking(bookingId, comment);

    call$.subscribe({
      next: () => {
        this.actionLoading.set(false);
        this.actionSuccess.set('Action completed successfully.');
        setTimeout(() => this.actionSuccess.set(''), 5000);
        this.loadBookings();
      },
      error: (err) => {
        this.actionLoading.set(false);
        this.actionError.set(err.error?.message ?? 'Action failed. Please try again.');
      }
    });
  }
}
