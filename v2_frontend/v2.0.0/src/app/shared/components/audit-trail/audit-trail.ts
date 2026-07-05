import { Component, OnChanges, inject, input, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { BookingService } from '../../../core/services/booking.service';
import { BookingAuditLogEntry } from '../../../core/models/booking.model';

const ACTION_LABELS: Record<string, string> = {
  Dose1Booked: 'Dose 1 Booked',
  Dose2Booked: 'Dose 2 Booked',
  Approved:    'Approved',
  Rejected:    'Rejected',
  Cancelled:   'Cancelled',
  Rebooked:    'Rebooked',
  Edited:      'Edited',
};

const ACTOR_ROLE_LABELS: Record<string, string> = {
  user:            'You',
  'hospital-admin': 'Hospital Admin',
  admin:           'Platform Admin',
};

@Component({
  selector: 'app-audit-trail',
  imports: [DatePipe],
  templateUrl: './audit-trail.html',
})
export class AuditTrailComponent implements OnChanges {
  private bookingService = inject(BookingService);

  bookingId = input.required<string>();

  entries = signal<BookingAuditLogEntry[]>([]);
  loading = signal(false);
  error   = signal('');

  ngOnChanges(): void {
    if (!this.bookingId()) return;
    this.loading.set(true);
    this.error.set('');
    this.bookingService.getBookingAuditTrail(this.bookingId()).subscribe({
      next: (entries) => { this.loading.set(false); this.entries.set(entries); },
      error: () => { this.loading.set(false); this.error.set('Unable to load history.'); }
    });
  }

  actionLabel(actionType: string): string {
    return ACTION_LABELS[actionType] ?? actionType;
  }

  actorLabel(actorRole: string): string {
    return ACTOR_ROLE_LABELS[actorRole] ?? actorRole;
  }
}
