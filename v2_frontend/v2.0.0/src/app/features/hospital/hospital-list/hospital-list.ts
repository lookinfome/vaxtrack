import { Component, inject, signal, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { HospitalService } from '../../../core/services/hospital.service';
import { AuthService } from '../../../core/services/auth.service';
import { Hospital } from '../../../core/models/hospital.model';

@Component({
  selector: 'app-hospital-list',
  imports: [RouterLink],
  templateUrl: './hospital-list.html',
  styleUrl: './hospital-list.css'
})
export class HospitalList implements OnInit {
  private hospitalService = inject(HospitalService);
  readonly authService = inject(AuthService);

  hospitals = signal<Hospital[]>([]);
  loading = signal(true);
  error = signal('');

  ngOnInit(): void {
    this.hospitalService.getAllHospitals().subscribe({
      next: (data) => {
        this.hospitals.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(err.error?.message ?? 'Failed to load hospitals.');
        this.loading.set(false);
        console.error('[HospitalList] getAllHospitals failed:', err);
      }
    });
  }
}
