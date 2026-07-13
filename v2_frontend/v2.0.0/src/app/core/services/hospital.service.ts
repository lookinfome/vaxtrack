import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  Hospital, HospitalAuditLogEntry,
  CreateHospitalRequest, CreateHospitalResponse,
  UpdateHospitalRequest, UpdateHospitalResponse
} from '../models/hospital.model';
import { environment } from '../../../environments/environment';

const BASE = `${environment.apiBaseUrl}/api/vaxtrack/v1/hospital`;

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

  // ── lifecycle ────────────────────────────────────────────────────────────

  disableHospital(hospitalId: string, comment: string): Observable<Hospital> {
    return this.http.put<Hospital>(`${BASE}/disableHospital/${hospitalId}`, { comment });
  }

  requestReactivation(hospitalId: string, comment?: string): Observable<Hospital> {
    return this.http.put<Hospital>(`${BASE}/requestReactivation/${hospitalId}`, { comment });
  }

  approveReactivation(hospitalId: string, comment?: string): Observable<Hospital> {
    return this.http.put<Hospital>(`${BASE}/approveReactivation/${hospitalId}`, { comment });
  }

  rejectReactivation(hospitalId: string, comment?: string): Observable<Hospital> {
    return this.http.put<Hospital>(`${BASE}/rejectReactivation/${hospitalId}`, { comment });
  }

  requestUnregister(hospitalId: string, comment: string): Observable<Hospital> {
    return this.http.put<Hospital>(`${BASE}/requestUnregister/${hospitalId}`, { comment });
  }

  withdrawUnregisterRequest(hospitalId: string): Observable<Hospital> {
    return this.http.put<Hospital>(`${BASE}/withdrawUnregisterRequest/${hospitalId}`, {});
  }

  declineUnregisterRequest(hospitalId: string, comment?: string): Observable<Hospital> {
    return this.http.put<Hospital>(`${BASE}/declineUnregisterRequest/${hospitalId}`, { comment });
  }

  authorizeUnregister(hospitalId: string, password: string, comment?: string): Observable<void> {
    return this.http.put<void>(`${BASE}/authorizeUnregister/${hospitalId}`, { password, comment });
  }

  getHospitalAuditTrail(hospitalId: string): Observable<HospitalAuditLogEntry[]> {
    return this.http.get<HospitalAuditLogEntry[]>(`${BASE}/getHospitalAuditTrail/${hospitalId}`);
  }
}
