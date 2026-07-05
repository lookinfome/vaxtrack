import { Component, inject, signal, OnInit, computed } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { DragDropModule, CdkDragDrop } from '@angular/cdk/drag-drop';
import { HospitalService } from '../../../core/services/hospital.service';
import { AuthService } from '../../../core/services/auth.service';
import { RoleMappingService } from '../../../core/services/role-mapping.service';
import { UserService } from '../../../core/services/user.service';
import { Hospital } from '../../../core/models/hospital.model';
import { UserRoleMapping } from '../../../core/models/role-mapping.model';
import { User } from '../../../core/models/user.model';
import { FooterComponent } from '../../../shared/components/footer/footer';
import { HospitalAuditTrailComponent } from '../../../shared/components/hospital-audit-trail/hospital-audit-trail';
import { ConfirmActionModalComponent } from '../../../shared/components/confirm-action-modal/confirm-action-modal';
import { AuthorizeUnregisterModalComponent } from '../../../shared/components/authorize-unregister-modal/authorize-unregister-modal';

type LifecycleAction =
  | 'disable'
  | 'requestReactivation'
  | 'approveReactivation'
  | 'rejectReactivation'
  | 'requestUnregister'
  | 'withdrawUnregister'
  | 'declineUnregister';

@Component({
  selector: 'app-hospital-list',
  imports: [ReactiveFormsModule, DragDropModule, FooterComponent, HospitalAuditTrailComponent, ConfirmActionModalComponent, AuthorizeUnregisterModalComponent],
  templateUrl: './hospital-list.html',
})
export class HospitalList implements OnInit {
  private hospitalService    = inject(HospitalService);
  private roleMappingService = inject(RoleMappingService);
  private userService        = inject(UserService);
  readonly authService       = inject(AuthService);
  private fb                 = inject(FormBuilder);

  hospitals     = signal<Hospital[]>([]);
  loading       = signal(true);
  error         = signal('');
  actionLoading = signal(false);
  actionError   = signal('');
  actionSuccess = signal('');

  showCreateModal = signal(false);
  editingHospital = signal<Hospital | null>(null);

  // HospitalUids the caller manages as hospital-admin (empty for a plain platform admin)
  myHospitalUids = signal<string[]>([]);

  filterName    = signal('');
  filterPinCode = signal('');
  filterPhone   = signal('');
  filterEmail   = signal('');

  hasActiveFilters = computed(() =>
    !!this.filterName() || !!this.filterPinCode() || !!this.filterPhone() || !!this.filterEmail()
  );

  filteredHospitals = computed(() => {
    const name = this.filterName().toLowerCase().trim();
    const pinCode = this.filterPinCode().trim();
    const phone = this.filterPhone().trim();
    const email = this.filterEmail().toLowerCase().trim();
    return this.hospitals().filter(h =>
      (!name || h.hospitalName.toLowerCase().includes(name)) &&
      (!pinCode || (h.hospitalPinCode ?? '').includes(pinCode)) &&
      (!phone || (h.hospitalPhoneNumber ?? '').includes(phone)) &&
      (!email || (h.hospitalEmail ?? '').toLowerCase().includes(email))
    );
  });

  // A hospital-admin only ever sees their own hospital(s) — never the full directory.
  visibleHospitals = computed(() => {
    const list = this.filteredHospitals();
    return this.authService.isAdmin() ? list : list.filter(h => this.isMyHospital(h));
  });

  totalAvailableSlots = computed(() =>
    this.hospitals().reduce((sum, h) => sum + h.slotsAvailable, 0)
  );

  hospitalsWithSlots = computed(() =>
    this.hospitals().filter(h => h.slotsAvailable > 0).length
  );

  createForm = this.fb.group({
    hospitalName: ['', Validators.required],
  });

  editForm = this.fb.group({
    hospitalAddress:    ['', Validators.required],
    hospitalPinCode:    ['', [Validators.required, Validators.pattern(/^\d{6}$/)]],
    hospitalPhoneNumber:['', [Validators.required, Validators.pattern(/^\d{10}$/)]],
    hospitalEmail:      ['', [Validators.required, Validators.email]],
    totalSlots:         [0,  [Validators.required, Validators.min(1)]],
  });

  // ── master-detail state ──────────────────────────────────────────────────

  selectedHospital = signal<Hospital | null>(null);
  expandedHistory = signal(false);

  hospitalAdmins = signal<UserRoleMapping[]>([]);
  hospitalAdminsLoading = signal(false);

  userSearchQuery = signal('');
  userSearchResults = signal<User[]>([]);
  userSearchLoading = signal(false);

  // ── lifecycle action state ───────────────────────────────────────────────

  pendingAction = signal<{ hospital: Hospital; action: LifecycleAction } | null>(null);
  authorizingHospital = signal<Hospital | null>(null);
  authorizeLoading = signal(false);
  authorizeError = signal('');

  ngOnInit(): void {
    this.loadHospitals();

    if (!this.authService.isAdmin()) {
      const userUid = this.authService.currentUser()?.userUid ?? '';
      this.roleMappingService.getUserRoles(userUid).subscribe({
        next: (roles) => this.myHospitalUids.set(
          roles.filter(r => r.roleTag === 'hospital-admin' && r.isActive).map(r => r.contextId)
        ),
        error: () => this.myHospitalUids.set([])
      });
    }
  }

  private loadHospitals(): void {
    this.loading.set(true);
    this.hospitalService.getAllHospitals().subscribe({
      next: (data) => {
        this.hospitals.set(data);
        this.loading.set(false);

        // Keep the open detail panel in sync with fresh data after any mutating action
        const selected = this.selectedHospital();
        if (selected) {
          const refreshed = data.find(h => h.hospitalId === selected.hospitalId) ?? null;
          this.selectedHospital.set(refreshed);
        }
      },
      error: (err) => { this.error.set(err.error?.message ?? 'Failed to load hospitals.'); this.loading.set(false); }
    });
  }

  onFilterNameChange(value: string): void { this.filterName.set(value); }
  onFilterPinCodeChange(value: string): void { this.filterPinCode.set(value); }
  onFilterPhoneChange(value: string): void { this.filterPhone.set(value); }
  onFilterEmailChange(value: string): void { this.filterEmail.set(value); }

  clearFilters(): void {
    this.filterName.set('');
    this.filterPinCode.set('');
    this.filterPhone.set('');
    this.filterEmail.set('');
  }

  slotsPercent(h: Hospital): number {
    if (!h.totalSlots) return 0;
    return Math.round((h.slotsAvailable / h.totalSlots) * 100);
  }

  isMyHospital(h: Hospital): boolean {
    return this.myHospitalUids().includes(h.hospitalUid);
  }

  // ── master-detail ─────────────────────────────────────────────────────────

  selectHospital(h: Hospital): void {
    this.selectedHospital.set(h);
    this.expandedHistory.set(false);
    this.actionError.set('');
    this.userSearchQuery.set('');
    this.userSearchResults.set([]);
    if (this.authService.isAdmin()) this.loadHospitalAdmins(h);
  }

  closeDetail(): void {
    this.selectedHospital.set(null);
  }

  toggleHistory(): void {
    this.expandedHistory.set(!this.expandedHistory());
  }

  private loadHospitalAdmins(h: Hospital): void {
    this.hospitalAdminsLoading.set(true);
    this.roleMappingService.getUsersInRole('hospital-admin', h.hospitalUid).subscribe({
      next: (list) => { this.hospitalAdmins.set(list.filter(m => m.isActive)); this.hospitalAdminsLoading.set(false); },
      error: () => { this.hospitalAdmins.set([]); this.hospitalAdminsLoading.set(false); }
    });
  }

  onUserSearchChange(value: string): void {
    this.userSearchQuery.set(value);
    if (!value.trim()) { this.userSearchResults.set([]); return; }

    this.userSearchLoading.set(true);
    this.userService.getUsersPaged({ name: value, page: 1, pageSize: 10 }).subscribe({
      next: (res) => { this.userSearchResults.set(res.items); this.userSearchLoading.set(false); },
      error: () => { this.userSearchResults.set([]); this.userSearchLoading.set(false); }
    });
  }

  // Dragging a searched user into "Hospital Admins" assigns the role; dragging a hospital-admin
  // chip back out to "Available Users" revokes it. Both directions update in real time.
  // (Loosely typed: the two connected drop lists carry different item types — User[] and
  // UserRoleMapping[] — and CdkDragDrop is invariant in its container type parameter.)
  onHospitalAdminDrop(event: CdkDragDrop<any>): void {
    const hospital = this.selectedHospital();
    if (!hospital || event.previousContainer === event.container) return;

    if (event.container.id === 'hospitalAdminsDropList') {
      const user = event.previousContainer.data[event.previousIndex] as User;
      this.roleMappingService.assignRole(user.userUid, 'hospital-admin', hospital.hospitalUid).subscribe({
        next: () => {
          this.flash(`${user.userName} is now a hospital-admin for ${hospital.hospitalName}.`);
          this.loadHospitalAdmins(hospital);
        },
        error: (err) => this.actionError.set(err.error?.message ?? 'Failed to assign hospital-admin.')
      });
    } else if (event.container.id === 'availableUsersDropList') {
      const mapping = event.previousContainer.data[event.previousIndex] as UserRoleMapping;
      this.removeHospitalAdmin(mapping);
    }
  }

  removeHospitalAdmin(mapping: UserRoleMapping): void {
    const hospital = this.selectedHospital();
    if (!hospital) return;
    this.roleMappingService.revokeRole(mapping.id).subscribe({
      next: () => {
        this.flash('Hospital-admin role removed.');
        this.loadHospitalAdmins(hospital);
      },
      error: (err) => this.actionError.set(err.error?.message ?? 'Failed to remove hospital-admin.')
    });
  }

  // ── Create ────────────────────────────────────────────────────────────

  openCreate(): void {
    this.createForm.reset();
    this.actionError.set('');
    this.showCreateModal.set(true);
  }

  closeCreate(): void { this.showCreateModal.set(false); }

  submitCreate(): void {
    if (this.createForm.invalid) { this.createForm.markAllAsTouched(); return; }
    this.actionLoading.set(true);
    this.actionError.set('');
    const { hospitalName } = this.createForm.getRawValue();
    this.hospitalService.createHospital({ hospitalName: hospitalName! }).subscribe({
      next: () => {
        this.actionLoading.set(false);
        this.showCreateModal.set(false);
        this.flash('Hospital created successfully.');
        this.loadHospitals();
      },
      error: (err) => {
        this.actionLoading.set(false);
        this.actionError.set(err.error?.message ?? 'Failed to create hospital.');
      }
    });
  }

  // ── Edit ──────────────────────────────────────────────────────────────

  openEdit(h: Hospital): void {
    this.editingHospital.set(h);
    this.actionError.set('');
    this.editForm.patchValue({
      hospitalAddress:     h.hospitalAddress,
      hospitalPinCode:     h.hospitalPinCode,
      hospitalPhoneNumber: h.hospitalPhoneNumber,
      hospitalEmail:       h.hospitalEmail,
      totalSlots:          h.totalSlots,
    });
  }

  closeEdit(): void { this.editingHospital.set(null); }

  submitEdit(): void {
    const h = this.editingHospital();
    if (!h || this.editForm.invalid) { this.editForm.markAllAsTouched(); return; }
    this.actionLoading.set(true);
    this.actionError.set('');
    const raw = this.editForm.getRawValue();

    this.hospitalService.updateHospital({
      hospitalId:          h.hospitalId,
      hospitalAddress:     raw.hospitalAddress!,
      hospitalPinCode:     raw.hospitalPinCode!,
      hospitalPhoneNumber: raw.hospitalPhoneNumber!,
      hospitalEmail:       raw.hospitalEmail!,
    }).subscribe({
      next: () => {
        if (raw.totalSlots !== h.totalSlots) {
          this.hospitalService.updateTotalSlots(h.hospitalId, raw.totalSlots!).subscribe({
            next:  ()    => this.afterEditSuccess(),
            error: (err) => {
              this.actionLoading.set(false);
              this.actionError.set(err.error?.message ?? 'Info saved but slot update failed.');
            }
          });
        } else {
          this.afterEditSuccess();
        }
      },
      error: (err) => {
        this.actionLoading.set(false);
        this.actionError.set(err.error?.message ?? 'Failed to update hospital.');
      }
    });
  }

  private afterEditSuccess(): void {
    this.actionLoading.set(false);
    this.editingHospital.set(null);
    this.flash('Hospital updated successfully.');
    this.loadHospitals();
  }

  // ── Lifecycle actions ─────────────────────────────────────────────────

  requestAction(hospital: Hospital, action: LifecycleAction): void {
    this.actionError.set('');
    this.pendingAction.set({ hospital, action });
  }

  dismissAction(): void {
    this.pendingAction.set(null);
  }

  get modalTitle(): string {
    switch (this.pendingAction()?.action) {
      case 'disable':              return 'Disable this centre?';
      case 'requestReactivation':  return 'Request reactivation?';
      case 'approveReactivation':  return 'Approve reactivation?';
      case 'rejectReactivation':   return 'Reject reactivation?';
      case 'requestUnregister':    return 'Request unregistration?';
      case 'withdrawUnregister':   return 'Withdraw unregister request?';
      case 'declineUnregister':    return 'Decline unregister request?';
      default:                     return '';
    }
  }

  get modalMessage(): string {
    switch (this.pendingAction()?.action) {
      case 'disable':             return 'The centre will be hidden from new bookings. Existing appointments are unaffected. Please explain why.';
      case 'requestReactivation': return 'A platform admin will review your request before the centre goes live again.';
      case 'approveReactivation': return 'The centre becomes Active again immediately.';
      case 'rejectReactivation':  return 'The centre stays Disabled. Please explain why, if helpful.';
      case 'requestUnregister':   return 'The hospital-admin will need to authorize this before the centre is permanently removed. Please explain why.';
      case 'withdrawUnregister':  return 'Cancels the pending unregistration — the centre stays Disabled.';
      case 'declineUnregister':   return 'Declines the platform admin\'s unregister request — the centre stays Disabled.';
      default:                    return '';
    }
  }

  get modalCommentRequired(): boolean {
    const action = this.pendingAction()?.action;
    return action === 'disable' || action === 'requestUnregister';
  }

  get modalAccentColor(): 'red' | 'teal' | 'emerald' | 'amber' {
    switch (this.pendingAction()?.action) {
      case 'approveReactivation': return 'emerald';
      case 'requestReactivation': return 'teal';
      case 'rejectReactivation':
      case 'requestUnregister':   return 'red';
      default:                    return 'amber';
    }
  }

  confirmAction(comment: string | undefined): void {
    const pending = this.pendingAction();
    if (!pending) return;

    const { hospital, action } = pending;
    this.actionLoading.set(true);
    this.actionError.set('');
    this.pendingAction.set(null);

    const call$ =
      action === 'disable'             ? this.hospitalService.disableHospital(hospital.hospitalId, comment ?? '') :
      action === 'requestReactivation' ? this.hospitalService.requestReactivation(hospital.hospitalId, comment) :
      action === 'approveReactivation' ? this.hospitalService.approveReactivation(hospital.hospitalId, comment) :
      action === 'rejectReactivation'  ? this.hospitalService.rejectReactivation(hospital.hospitalId, comment) :
      action === 'requestUnregister'   ? this.hospitalService.requestUnregister(hospital.hospitalId, comment ?? '') :
      action === 'withdrawUnregister'  ? this.hospitalService.withdrawUnregisterRequest(hospital.hospitalId) :
                                          this.hospitalService.declineUnregisterRequest(hospital.hospitalId, comment);

    call$.subscribe({
      next: () => {
        this.actionLoading.set(false);
        this.flash('Action completed successfully.');
        this.loadHospitals();
      },
      error: (err) => {
        this.actionLoading.set(false);
        this.actionError.set(err.error?.message ?? 'Action failed. Please try again.');
      }
    });
  }

  // ── Authorize unregister (password re-entry) ─────────────────────────

  openAuthorize(h: Hospital): void {
    this.authorizeError.set('');
    this.authorizingHospital.set(h);
  }

  dismissAuthorize(): void {
    this.authorizingHospital.set(null);
  }

  confirmAuthorize(payload: { password: string; comment?: string }): void {
    const h = this.authorizingHospital();
    if (!h) return;

    this.authorizeLoading.set(true);
    this.authorizeError.set('');

    this.hospitalService.authorizeUnregister(h.hospitalId, payload.password, payload.comment).subscribe({
      next: () => {
        this.authorizeLoading.set(false);
        this.authorizingHospital.set(null);
        this.selectedHospital.set(null);
        this.flash('Hospital unregistered successfully.');
        this.loadHospitals();
      },
      error: (err) => {
        this.authorizeLoading.set(false);
        this.authorizeError.set(err.error?.message ?? 'Authorization failed. Please try again.');
      }
    });
  }

  private flash(msg: string): void {
    this.actionSuccess.set(msg);
    setTimeout(() => this.actionSuccess.set(''), 4000);
  }
}
