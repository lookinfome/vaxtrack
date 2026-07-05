import { Component, inject, signal, OnInit, computed } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { UserService } from '../../core/services/user.service';
import { HospitalService } from '../../core/services/hospital.service';
import { RoleMappingService } from '../../core/services/role-mapping.service';
import { Hospital } from '../../core/models/hospital.model';
import { UserRequest } from '../../core/models/role-mapping.model';
import { ConfirmActionModalComponent } from '../../shared/components/confirm-action-modal/confirm-action-modal';
import { FooterComponent } from '../../shared/components/footer/footer';

type ActiveView = 'menu' | 'delete-reason' | 'delete-confirm' | 'hospital-admin-application';

const DELETE_REASONS = [
  'No longer need the service',
  'Privacy concerns',
  'Created a duplicate account',
  'Not satisfied with the experience',
];

type PendingRequestAction = { request: UserRequest; approve: boolean };

@Component({
  selector: 'app-support',
  imports: [ReactiveFormsModule, RouterLink, ConfirmActionModalComponent, FooterComponent],
  templateUrl: './support.html',
})
export class Support implements OnInit {
  readonly authService       = inject(AuthService);
  private userService        = inject(UserService);
  private hospitalService    = inject(HospitalService);
  private roleMappingService = inject(RoleMappingService);
  private router             = inject(Router);
  private fb                 = inject(FormBuilder);

  readonly deleteReasons = DELETE_REASONS;

  activeView = signal<ActiveView>('menu');
  loading = signal(false);
  error = signal('');

  hospitals = signal<Hospital[]>([]);

  // ── delete account ───────────────────────────────────────────────────────

  selectedReasons = signal<Set<string>>(new Set());
  otherReasonChecked = signal(false);
  otherReasonText = signal('');
  deletePassword = signal('');

  combinedDeleteReason = computed(() => {
    const parts = [...this.selectedReasons()];
    if (this.otherReasonChecked() && this.otherReasonText().trim()) parts.push(this.otherReasonText().trim());
    return parts.join('; ');
  });

  // ── hospital-admin application ───────────────────────────────────────────

  applicationForm = this.fb.group({
    hospitalId: ['', Validators.required],
    comment: [''],
  });
  applicationSubmitted = signal(false);

  // ── pending requests (platform admin only) ───────────────────────────────

  pendingRequests = signal<UserRequest[]>([]);
  pendingRequestsLoading = signal(false);
  pendingAction = signal<PendingRequestAction | null>(null);

  ngOnInit(): void {
    this.hospitalService.getAllHospitals().subscribe({
      next: (list) => this.hospitals.set(list),
      error: () => {}
    });

    if (this.authService.isAdmin()) this.loadPendingRequests();
  }

  private loadPendingRequests(): void {
    this.pendingRequestsLoading.set(true);
    this.roleMappingService.getPendingRequests().subscribe({
      next: (list) => { this.pendingRequests.set(list); this.pendingRequestsLoading.set(false); },
      error: () => { this.pendingRequests.set([]); this.pendingRequestsLoading.set(false); }
    });
  }

  hospitalName(hospitalId: string | null): string {
    if (!hospitalId) return '—';
    return this.hospitals().find(h => h.hospitalId === hospitalId)?.hospitalName ?? hospitalId;
  }

  // ── navigation between cards ──────────────────────────────────────────────

  showDeleteReasonForm(): void {
    this.error.set('');
    this.selectedReasons.set(new Set());
    this.otherReasonChecked.set(false);
    this.otherReasonText.set('');
    this.activeView.set('delete-reason');
  }

  showHospitalAdminApplicationForm(): void {
    this.error.set('');
    this.applicationSubmitted.set(false);
    this.applicationForm.reset();
    this.activeView.set('hospital-admin-application');
  }

  backToMenu(): void {
    this.activeView.set('menu');
    this.error.set('');
  }

  // ── delete account flow ───────────────────────────────────────────────────

  toggleReason(reason: string): void {
    const next = new Set(this.selectedReasons());
    if (next.has(reason)) next.delete(reason); else next.add(reason);
    this.selectedReasons.set(next);
  }

  toggleOtherReason(): void {
    this.otherReasonChecked.set(!this.otherReasonChecked());
  }

  proceedToPasswordConfirm(): void {
    if (this.selectedReasons().size === 0 && !this.otherReasonChecked()) {
      this.error.set('Please select at least one reason.');
      return;
    }
    this.error.set('');
    this.deletePassword.set('');
    this.activeView.set('delete-confirm');
  }

  confirmDelete(): void {
    if (!this.deletePassword()) {
      this.error.set('Please enter your password to confirm.');
      return;
    }

    this.loading.set(true);
    this.error.set('');

    this.userService.deleteMyAccount({ password: this.deletePassword(), reason: this.combinedDeleteReason() }).subscribe({
      next: () => {
        this.loading.set(false);
        this.authService.clearSession();
        this.router.navigate(['/auth/login']);
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err.error?.message ?? 'Failed to delete account. Please try again.');
      }
    });
  }

  // ── hospital-admin application flow ──────────────────────────────────────

  submitApplication(): void {
    if (this.applicationForm.invalid) {
      this.applicationForm.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.error.set('');
    const raw = this.applicationForm.getRawValue();

    this.roleMappingService.submitHospitalAdminApplication(raw.hospitalId!, raw.comment || undefined).subscribe({
      next: () => {
        this.loading.set(false);
        this.applicationSubmitted.set(true);
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err.error?.message ?? 'Failed to submit application. Please try again.');
      }
    });
  }

  // ── pending requests (platform admin) ────────────────────────────────────

  requestPendingAction(request: UserRequest, approve: boolean): void {
    this.pendingAction.set({ request, approve });
  }

  dismissPendingAction(): void {
    this.pendingAction.set(null);
  }

  get pendingModalTitle(): string {
    const action = this.pendingAction();
    if (!action) return '';
    return action.approve ? 'Approve this request?' : 'Reject this request?';
  }

  get pendingModalMessage(): string {
    const action = this.pendingAction();
    if (!action) return '';
    if (action.request.requestType === 'HospitalAdminApplication') {
      return action.approve
        ? `This grants hospital-admin rights for ${this.hospitalName(action.request.targetHospitalId)} immediately.`
        : 'The applicant will be notified that their request was declined.';
    }
    return action.approve
      ? 'The account becomes Active again immediately.'
      : 'The account stays Disabled.';
  }

  confirmPendingAction(comment: string | undefined): void {
    const action = this.pendingAction();
    if (!action) return;

    this.pendingAction.set(null);
    this.loading.set(true);
    this.error.set('');

    const call$ = action.request.requestType === 'HospitalAdminApplication'
      ? (action.approve
          ? this.roleMappingService.approveHospitalAdminApplication(action.request.id, comment)
          : this.roleMappingService.rejectHospitalAdminApplication(action.request.id, comment))
      : null;

    if (!call$) {
      this.loading.set(false);
      return;
    }

    call$.subscribe({
      next: () => {
        this.loading.set(false);
        this.loadPendingRequests();
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err.error?.message ?? 'Action failed. Please try again.');
      }
    });
  }
}
