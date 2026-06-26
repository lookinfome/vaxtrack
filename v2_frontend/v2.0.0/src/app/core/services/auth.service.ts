import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import {
  LoginRequest, LoginResponse,
  ForgotPasswordRequest, ForgotPasswordResponse,
  ResetForgottenPasswordRequest, ResetForgottenPasswordResponse
} from '../models/auth.model';

const BASE = '/api/vaxtrack/v1/auth';
const TOKEN_KEY = 'vaxtrack_token';
const USER_KEY = 'vaxtrack_user';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);

  private _isLoggedIn = signal<boolean>(this.hasValidSession());
  private _currentUser = signal<LoginResponse | null>(this.getStoredUser());

  readonly isLoggedIn = this._isLoggedIn.asReadonly();
  readonly currentUser = this._currentUser.asReadonly();

  login(request: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${BASE}/login`, request).pipe(
      tap(response => this.saveSession(response))
    );
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
