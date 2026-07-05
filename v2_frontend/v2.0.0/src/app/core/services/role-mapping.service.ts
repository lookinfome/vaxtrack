import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { UserRoleMapping, UserRequest } from '../models/role-mapping.model';

const BASE = '/api/vaxtrack/v1/userrolemapping';

@Injectable({ providedIn: 'root' })
export class RoleMappingService {
  private http = inject(HttpClient);

  getUserRoles(userUid: string): Observable<UserRoleMapping[]> {
    return this.http.get<UserRoleMapping[]>(`${BASE}/getUserRoles/${userUid}`);
  }

  isHospitalAdmin(userUid: string): Observable<boolean> {
    return this.getUserRoles(userUid).pipe(
      map(roles => roles.some(r => r.roleTag === 'hospital-admin' && r.isActive))
    );
  }

  getUsersInRole(roleTag: string, contextId: string): Observable<UserRoleMapping[]> {
    return this.http.get<UserRoleMapping[]>(`${BASE}/getUsersInRole/${roleTag}`, { params: { contextId } });
  }

  assignRole(userUid: string, roleTag: string, contextId: string): Observable<UserRoleMapping> {
    return this.http.post<UserRoleMapping>(`${BASE}/assignRole`, { userUid, roleTag, contextId });
  }

  revokeRole(mappingId: number): Observable<void> {
    return this.http.delete<void>(`${BASE}/revokeRole/${mappingId}`);
  }

  // ── hospital-admin application ────────────────────────────────────────────

  submitHospitalAdminApplication(hospitalId: string, comment?: string): Observable<UserRequest> {
    return this.http.post<UserRequest>(`${BASE}/submitHospitalAdminApplication`, { hospitalId, comment });
  }

  approveHospitalAdminApplication(requestId: number, comment?: string): Observable<UserRequest> {
    return this.http.put<UserRequest>(`${BASE}/approveHospitalAdminApplication/${requestId}`, { comment });
  }

  rejectHospitalAdminApplication(requestId: number, comment?: string): Observable<UserRequest> {
    return this.http.put<UserRequest>(`${BASE}/rejectHospitalAdminApplication/${requestId}`, { comment });
  }

  getPendingRequests(): Observable<UserRequest[]> {
    return this.http.get<UserRequest[]>(`${BASE}/getPendingRequests`);
  }
}
