import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { DatePipe } from '@angular/common';
import { DragDropModule, CdkDragDrop } from '@angular/cdk/drag-drop';
import { UserService } from '../../../core/services/user.service';
import { HospitalService } from '../../../core/services/hospital.service';
import { RoleMappingService } from '../../../core/services/role-mapping.service';
import { User } from '../../../core/models/user.model';
import { Hospital } from '../../../core/models/hospital.model';
import { UserRoleMapping } from '../../../core/models/role-mapping.model';
import { FooterComponent } from '../../../shared/components/footer/footer';
import { ConfirmActionModalComponent } from '../../../shared/components/confirm-action-modal/confirm-action-modal';

type PendingAction = 'disable' | 'approveReactivation' | 'rejectReactivation';

const PAGE_SIZE = 12;

@Component({
  selector: 'app-users-management',
  imports: [DatePipe, DragDropModule, FooterComponent, ConfirmActionModalComponent],
  templateUrl: './users-management.html',
})
export class UsersManagement implements OnInit {
  private userService        = inject(UserService);
  private hospitalService    = inject(HospitalService);
  private roleMappingService = inject(RoleMappingService);

  users      = signal<User[]>([]);
  totalCount = signal(0);
  page       = signal(1);
  loading    = signal(true);
  error      = signal('');
  actionLoading = signal(false);
  actionError   = signal('');
  actionSuccess = signal('');

  filterName    = signal('');
  filterPhone   = signal('');
  filterUserId  = signal('');
  filterUserUid = signal('');
  filterRole    = signal<'' | 'admin' | 'hospitalAdmin' | 'member'>('');
  filterVaccinationStatus = signal<'' | 'NotVaccinated' | 'Pending' | 'PartiallyVaccinated' | 'Vaccinated' | 'Rejected'>('');

  sortBy  = signal<'name' | 'registered' | 'status' | 'vaccination'>('name');
  sortDir = signal<'asc' | 'desc'>('asc');

  totalPages = computed(() => Math.max(1, Math.ceil(this.totalCount() / PAGE_SIZE)));

  hospitals = signal<Hospital[]>([]);

  // ── master-detail state ──────────────────────────────────────────────────

  selectedUser = signal<User | null>(null);
  userRoles = signal<UserRoleMapping[]>([]);
  userRolesLoading = signal(false);

  // When a "Hospital Admin" chip is dropped, hold here until a hospital is picked + confirmed
  pendingHospitalAdminDrop = signal(false);
  pickedHospitalId = signal('');

  pendingAction = signal<{ user: User; action: PendingAction } | null>(null);

  hospitalAdminMappings = computed(() => this.userRoles().filter(r => r.roleTag === 'hospital-admin' && r.isActive));

  // Combined drop-list data for the "Current Roles" zone (Angular templates can't spread arrays)
  currentRolesData = computed<(string | UserRoleMapping)[]>(() => {
    const mappings = this.hospitalAdminMappings();
    const user = this.selectedUser();
    return user?.userRole ? ['admin-chip' as string, ...mappings] : mappings;
  });

  // Available-roles drop-list data — "admin" chip only shown if not already an admin
  availableRolesData = computed<string[]>(() => {
    const user = this.selectedUser();
    return user?.userRole ? ['hospital-admin'] : ['admin', 'hospital-admin'];
  });

  ngOnInit(): void {
    this.loadUsers();
    this.hospitalService.getAllHospitals().subscribe({
      next: (list) => this.hospitals.set(list),
      error: () => {}
    });
  }

  private loadUsers(): void {
    this.loading.set(true);
    this.error.set('');
    this.userService.getUsersPaged({
      name: this.filterName() || undefined,
      phone: this.filterPhone() || undefined,
      userId: this.filterUserId() || undefined,
      userUid: this.filterUserUid() || undefined,
      role: this.filterRole() || undefined,
      vaccinationStatus: this.filterVaccinationStatus() || undefined,
      sortBy: this.sortBy(),
      sortDir: this.sortDir(),
      page: this.page(),
      pageSize: PAGE_SIZE
    }).subscribe({
      next: (res) => {
        this.users.set(res.items);
        this.totalCount.set(res.totalCount);
        this.loading.set(false);

        const selected = this.selectedUser();
        if (selected) {
          const refreshed = res.items.find(u => u.userId === selected.userId) ?? null;
          this.selectedUser.set(refreshed);
        }
      },
      error: (err) => {
        this.error.set(err.error?.message ?? 'Failed to load users.');
        this.loading.set(false);
      }
    });
  }

  applyFilters(): void {
    this.page.set(1);
    this.loadUsers();
  }

  onFilterNameChange(value: string): void { this.filterName.set(value); this.applyFilters(); }
  onFilterPhoneChange(value: string): void { this.filterPhone.set(value); this.applyFilters(); }
  onFilterUserIdChange(value: string): void { this.filterUserId.set(value); this.applyFilters(); }
  onFilterUserUidChange(value: string): void { this.filterUserUid.set(value); this.applyFilters(); }
  onFilterRoleChange(value: '' | 'admin' | 'hospitalAdmin' | 'member'): void { this.filterRole.set(value); this.applyFilters(); }
  onFilterVaccinationStatusChange(value: '' | 'NotVaccinated' | 'Pending' | 'PartiallyVaccinated' | 'Vaccinated' | 'Rejected'): void {
    this.filterVaccinationStatus.set(value); this.applyFilters();
  }

  onSortByChange(value: 'name' | 'registered' | 'status' | 'vaccination'): void { this.sortBy.set(value); this.applyFilters(); }
  toggleSortDir(): void { this.sortDir.set(this.sortDir() === 'asc' ? 'desc' : 'asc'); this.applyFilters(); }

  hasActiveFilters(): boolean {
    return !!this.filterName() || !!this.filterPhone() || !!this.filterUserId() || !!this.filterUserUid()
      || !!this.filterRole() || !!this.filterVaccinationStatus();
  }

  clearFilters(): void {
    this.filterName.set('');
    this.filterPhone.set('');
    this.filterUserId.set('');
    this.filterUserUid.set('');
    this.filterRole.set('');
    this.filterVaccinationStatus.set('');
    this.applyFilters();
  }

  // Icon + hover-tooltip labeling for the list card's vaccination-status indicator
  vaccinationStatusMeta(status: string): { icon: string; colorClass: string; label: string } {
    switch (status) {
      case 'Vaccinated':
        return { icon: '✓', colorClass: 'bg-emerald-100 text-emerald-700', label: 'Fully Vaccinated' };
      case 'PartiallyVaccinated':
        return { icon: '◐', colorClass: 'bg-blue-100 text-blue-700', label: 'Partially Vaccinated' };
      case 'Pending':
        return { icon: '⏳', colorClass: 'bg-amber-100 text-amber-700', label: 'Vaccination Pending' };
      case 'Rejected':
        return { icon: '✕', colorClass: 'bg-red-100 text-red-700', label: 'Vaccination Rejected' };
      default:
        return { icon: '–', colorClass: 'bg-slate-100 text-slate-500', label: 'Not Vaccinated' };
    }
  }

  goToPage(p: number): void {
    if (p < 1 || p > this.totalPages()) return;
    this.page.set(p);
    this.loadUsers();
  }

  hospitalName(hospitalUid: string): string {
    return this.hospitals().find(h => h.hospitalUid === hospitalUid)?.hospitalName ?? hospitalUid;
  }

  // ── master-detail ─────────────────────────────────────────────────────────

  selectUser(u: User): void {
    this.selectedUser.set(u);
    this.actionError.set('');
    this.pendingHospitalAdminDrop.set(false);
    this.pickedHospitalId.set('');
    this.loadUserRoles(u);
  }

  closeDetail(): void {
    this.selectedUser.set(null);
  }

  private loadUserRoles(u: User): void {
    this.userRolesLoading.set(true);
    this.roleMappingService.getUserRoles(u.userUid).subscribe({
      next: (roles) => { this.userRoles.set(roles); this.userRolesLoading.set(false); },
      error: () => { this.userRoles.set([]); this.userRolesLoading.set(false); }
    });
  }

  // ── drag-and-drop role assignment ────────────────────────────────────────

  onRoleDrop(event: CdkDragDrop<any>): void {
    const user = this.selectedUser();
    if (!user || event.previousContainer === event.container) return;

    if (event.container.id === 'currentRolesDropList') {
      const roleTag = event.previousContainer.data[event.previousIndex] as string;
      if (roleTag === 'admin') {
        this.userService.promoteToAdmin(user.userId).subscribe({
          next: (updated) => { this.flash(`${updated.userName} is now a platform admin.`); this.selectUser(updated); },
          error: (err) => this.actionError.set(err.error?.message ?? 'Failed to grant admin rights.')
        });
      } else {
        // hospital-admin needs a target hospital first
        this.pendingHospitalAdminDrop.set(true);
      }
    } else if (event.container.id === 'availableRolesDropList') {
      const mapping = event.previousContainer.data[event.previousIndex];
      if (mapping === 'admin-chip') {
        this.demoteAdmin();
      } else {
        this.removeHospitalAdminMapping(mapping as UserRoleMapping);
      }
    }
  }

  confirmHospitalAdminAssignment(): void {
    const user = this.selectedUser();
    const hospital = this.hospitals().find(h => h.hospitalId === this.pickedHospitalId());
    if (!user || !hospital) return;

    this.roleMappingService.assignRole(user.userUid, 'hospital-admin', hospital.hospitalUid).subscribe({
      next: () => {
        this.flash(`${user.userName} is now a hospital-admin for ${hospital.hospitalName}.`);
        this.pendingHospitalAdminDrop.set(false);
        this.pickedHospitalId.set('');
        this.loadUserRoles(user);
      },
      error: (err) => this.actionError.set(err.error?.message ?? 'Failed to assign hospital-admin.')
    });
  }

  cancelHospitalAdminAssignment(): void {
    this.pendingHospitalAdminDrop.set(false);
    this.pickedHospitalId.set('');
  }

  demoteAdmin(): void {
    const user = this.selectedUser();
    if (!user) return;
    this.userService.demoteFromAdmin(user.userId).subscribe({
      next: (updated) => { this.flash('Admin rights revoked.'); this.selectUser(updated); },
      error: (err) => this.actionError.set(err.error?.message ?? 'Failed to revoke admin rights.')
    });
  }

  removeHospitalAdminMapping(mapping: UserRoleMapping): void {
    const user = this.selectedUser();
    if (!user) return;
    this.roleMappingService.revokeRole(mapping.id).subscribe({
      next: () => { this.flash('Hospital-admin role removed.'); this.loadUserRoles(user); },
      error: (err) => this.actionError.set(err.error?.message ?? 'Failed to remove hospital-admin role.')
    });
  }

  // ── lifecycle actions ─────────────────────────────────────────────────────

  requestAction(user: User, action: PendingAction): void {
    this.actionError.set('');
    this.pendingAction.set({ user, action });
  }

  dismissAction(): void {
    this.pendingAction.set(null);
  }

  get modalTitle(): string {
    switch (this.pendingAction()?.action) {
      case 'disable':             return 'Disable this account?';
      case 'approveReactivation': return 'Approve reactivation?';
      case 'rejectReactivation':  return 'Reject reactivation?';
      default:                    return '';
    }
  }

  get modalMessage(): string {
    switch (this.pendingAction()?.action) {
      case 'disable':             return 'The user will be logged out immediately and unable to log back in until reactivated. Please explain why.';
      case 'approveReactivation': return 'The account becomes Active again immediately.';
      case 'rejectReactivation':  return 'The account stays Disabled. Please explain why, if helpful.';
      default:                    return '';
    }
  }

  get modalCommentRequired(): boolean {
    return this.pendingAction()?.action === 'disable';
  }

  confirmAction(comment: string | undefined): void {
    const pending = this.pendingAction();
    if (!pending) return;

    const { user, action } = pending;
    this.actionLoading.set(true);
    this.actionError.set('');
    this.pendingAction.set(null);

    const call$ =
      action === 'disable'             ? this.userService.disableUser(user.userId, comment ?? '') :
      action === 'approveReactivation' ? this.userService.approveUserReactivation(user.userId, comment) :
                                          this.userService.rejectUserReactivation(user.userId, comment);

    call$.subscribe({
      next: (updated) => {
        this.actionLoading.set(false);
        this.flash('Action completed successfully.');
        this.selectedUser.set(updated);
        this.loadUsers();
      },
      error: (err) => {
        this.actionLoading.set(false);
        this.actionError.set(err.error?.message ?? 'Action failed. Please try again.');
      }
    });
  }

  private flash(msg: string): void {
    this.actionSuccess.set(msg);
    setTimeout(() => this.actionSuccess.set(''), 4000);
  }
}
