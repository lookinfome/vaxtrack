import { Component, OnChanges, inject, input, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { HospitalService } from '../../../core/services/hospital.service';
import { HospitalAuditLogEntry } from '../../../core/models/hospital.model';

const ACTION_LABELS: Record<string, string> = {
  Disabled:               'Disabled',
  ReactivationRequested:  'Reactivation Requested',
  ReactivationApproved:   'Reactivation Approved',
  ReactivationRejected:   'Reactivation Rejected',
  UnregisterRequested:    'Unregister Requested',
  UnregisterWithdrawn:    'Unregister Request Withdrawn',
  UnregisterDeclined:     'Unregister Request Declined',
  Unregistered:           'Unregistered',
};

const ACTOR_ROLE_LABELS: Record<string, string> = {
  'hospital-admin': 'Hospital Admin',
  admin:            'Platform Admin',
};

@Component({
  selector: 'app-hospital-audit-trail',
  imports: [DatePipe],
  templateUrl: './hospital-audit-trail.html',
})
export class HospitalAuditTrailComponent implements OnChanges {
  private hospitalService = inject(HospitalService);

  hospitalId = input.required<string>();

  entries = signal<HospitalAuditLogEntry[]>([]);
  loading = signal(false);
  error   = signal('');

  ngOnChanges(): void {
    if (!this.hospitalId()) return;
    this.loading.set(true);
    this.error.set('');
    this.hospitalService.getHospitalAuditTrail(this.hospitalId()).subscribe({
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
