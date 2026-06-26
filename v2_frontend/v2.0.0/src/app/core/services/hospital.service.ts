import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  Hospital,
  CreateHospitalRequest, CreateHospitalResponse,
  UpdateHospitalRequest, UpdateHospitalResponse
} from '../models/hospital.model';

const BASE = '/api/vaxtrack/v1/hospital';

@Injectable({ providedIn: 'root' })
export class HospitalService {
  private http = inject(HttpClient);

  createHospital(request: CreateHospitalRequest): Observable<CreateHospitalResponse> {
    return this.http.post<CreateHospitalResponse>(`${BASE}/createHospital`, request);
  }

  updateHospital(request: UpdateHospitalRequest): Observable<UpdateHospitalResponse> {
    return this.http.put<UpdateHospitalResponse>(`${BASE}/updateHospital`, request);
  }

  updateTotalSlots(hospitalId: string, totalSlots: number): Observable<number> {
    return this.http.put<number>(`${BASE}/updateTotalSlots/${hospitalId}/${totalSlots}`, {});
  }

  updateAvailableSlots(hospitalId: string, slotsToUpdate: number): Observable<number> {
    return this.http.put<number>(`${BASE}/updateAvailableSlots/${hospitalId}/${slotsToUpdate}`, {});
  }

  getHospitalById(hospitalId: string): Observable<Hospital> {
    return this.http.get<Hospital>(`${BASE}/getHospitalById/${hospitalId}`);
  }

  getAllHospitals(): Observable<Hospital[]> {
    return this.http.get<Hospital[]>(`${BASE}/getAllHospitals`);
  }

  deleteHospital(hospitalId: string): Observable<void> {
    return this.http.delete<void>(`${BASE}/deleteHospital/${hospitalId}`);
  }
}
