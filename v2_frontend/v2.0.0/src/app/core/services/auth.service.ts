import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import {
  LoginRequest, LoginResponse,
  ForgotPasswordRequest, ForgotPasswordResponse,
  ResetForgottenPasswordRequest, ResetForgottenPasswordResponse,
  RequestAccountReactivationRequest
} from '../models/auth.model';
import { RoleMappingService } from './role-mapping.service';

const BASE = '/api/vaxtrack/v1/auth';
const TOKEN_KEY = 'vaxtrack_token';
const USER_KEY = 'vaxtrack_user';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  private roleMappingService = inject(RoleMappingService);

  private _isLoggedIn = signal<boolean>(this.hasValidSession());
  private _currentUser = signal<LoginResponse | null>(this.getStoredUser());
  private _isHospitalAdmin = signal<boolean>(false);

  readonly isLoggedIn = this._isLoggedIn.asReadonly();
  readonly currentUser = this._currentUser.asReadonly();
  readonly isHospitalAdmin = this._isHospitalAdmin.asReadonly();

  constructor() {
    // Recover hospital-admin status on a page refresh — it's never stored in the JWT/session,
    // only derivable by asking the server, so it has to be re-fetched whenever the app boots.
    if (this.hasValidSession()) this.refreshRoleContext();
  }

  login(request: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${BASE}/login`, request).pipe(
      tap(response => this.saveSession(response))
    );
  }

  // Re-derives isHospitalAdmin from the user's own role mappings. Safe no-op for a platform
  // admin or a plain user (mappings come back empty), so it's fine to call unconditionally.
  refreshRoleContext(): void {
    const userUid = this._currentUser()?.userUid;
    if (!userUid) return;

    this.roleMappingService.isHospitalAdmin(userUid).subscribe({
      next:  (result) => this._isHospitalAdmin.set(result),
      error: ()       => this._isHospitalAdmin.set(false)
    });
  }

  logout(): Observable<void> {
    return this.http.post<void>(`${BASE}/logout`, {}).pipe(
      tap(() => this.clearSession())
    );
  }

  forgotPassword(request: ForgotPasswordRequest): Observable<ForgotPasswordResponse> {
    return this.http.post<ForgotPasswordResponse>(`${BASE}/forgotPassword`, request);
  }

  resetForgottenPassword(request: ResetForgottenPasswordRequest): Observable<ResetForgottenPasswordResponse> {
    return this.http.post<ResetForgottenPasswordResponse>(`${BASE}/resetForgottenPassword`, request);
  }

  requestAccountReactivation(request: RequestAccountReactivationRequest): Observable<ForgotPasswordResponse> {
    return this.http.post<ForgotPasswordResponse>(`${BASE}/requestAccountReactivation`, request);
  }

  getToken(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  }

  isAdmin(): boolean {
    return this._currentUser()?.userRole ?? false;
  }

  clearSession(): void {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    this._isLoggedIn.set(false);
    this._currentUser.set(null);
    this._isHospitalAdmin.set(false);
  }

  private saveSession(response: LoginResponse): void {
    try {
      localStorage.setItem(TOKEN_KEY, response.token);
      localStorage.setItem(USER_KEY, JSON.stringify(response));
    } catch (err) {
      console.error('[AuthService] Failed to save session to localStorage:', err);
    }
    this._isLoggedIn.set(true);
    this._currentUser.set(response);
    this.refreshRoleContext();
  }

  private hasValidSession(): boolean {
    const storedUser = this.getStoredUser();
    if (!storedUser) return false;
    return new Date(storedUser.expiresAt) > new Date();
  }

  private getStoredUser(): LoginResponse | null {
    const raw = localStorage.getItem(USER_KEY);
    if (!raw) return null;
    try {
      return JSON.parse(raw);
    } catch {
      console.error('[AuthService] Corrupted session data found in localStorage — clearing');
      localStorage.removeItem(TOKEN_KEY);
      localStorage.removeItem(USER_KEY);
      return null;
    }
  }
}
