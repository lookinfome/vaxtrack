import { Component, inject, signal, OnInit, computed } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { HospitalService } from '../../../core/services/hospital.service';
import { AuthService } from '../../../core/services/auth.service';
import { Hospital } from '../../../core/models/hospital.model';
import { FooterComponent } from '../../../shared/components/footer/footer';

@Component({
  selector: 'app-hospital-list',
  imports: [RouterLink, ReactiveFormsModule, FooterComponent],
  templateUrl: './hospital-list.html',
})
export class HospitalList implements OnInit {
  private hospitalService = inject(HospitalService);
  readonly authService    = inject(AuthService);
  private fb              = inject(FormBuilder);

  hospitals     = signal<Hospital[]>([]);
  loading       = signal(true);
  error         = signal('');
  search        = signal('');
  actionLoading = signal(false);
  actionError   = signal('');
  actionSuccess = signal('');

  showCreateModal  = signal(false);
  editingHospital  = signal<Hospital | null>(null);
  deletingHospital = signal<Hospital | null>(null);

  filteredHospitals = computed(() => {
    const q = this.search().toLowerCase().trim();
    if (!q) return this.hospitals();
    return this.hospitals().filter(h =>
      h.hospitalName.toLowerCase().includes(q) ||
      (h.hospitalAddress ?? '').toLowerCase().includes(q) ||
      (h.hospitalPinCode ?? '').includes(q)
    );
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

  ngOnInit(): void { this.loadHospitals(); }

  private loadHospitals(): void {
    this.loading.set(true);
    this.hospitalService.getAllHospitals().subscribe({
      next:  (data) => { this.hospitals.set(data); this.loading.set(false); },
      error: (err)  => { this.error.set(err.error?.message ?? 'Failed to load hospitals.'); this.loading.set(false); }
    });
  }

  onSearch(value: string): void { this.search.set(value); }

  slotsPercent(h: Hospital): number {
    if (!h.totalSlots) return 0;
    return Math.round((h.slotsAvailable / h.totalSlots) * 100);
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

  // ── Delete ────────────────────────────────────────────────────────────

  confirmDelete(h: Hospital): void { this.deletingHospital.set(h); this.actionError.set(''); }
  dismissDelete(): void            { this.deletingHospital.set(null); }

  executeDelete(): void {
    const h = this.deletingHospital();
    if (!h) return;
    this.actionLoading.set(true);
    this.hospitalService.deleteHospital(h.hospitalId).subscribe({
      next: () => {
        this.actionLoading.set(false);
        this.deletingHospital.set(null);
        this.flash('Hospital removed successfully.');
        this.loadHospitals();
      },
      error: (err) => {
        this.actionLoading.set(false);
        this.actionError.set(err.error?.message ?? 'Failed to delete hospital.');
      }
    });
  }

  private flash(msg: string): void {
    this.actionSuccess.set(msg);
    setTimeout(() => this.actionSuccess.set(''), 4000);
  }
}
