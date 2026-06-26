import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  User,
  CreateUserRequest, CreateUserResponse,
  UpdateUserRequest, UpdateUserResponse,
  UpdateEmailRequest, UpdateEmailResponse,
  ChangePasswordRequest, ChangePasswordResponse
} from '../models/user.model';

const BASE = '/api/vaxtrack/v1/user';

@Injectable({ providedIn: 'root' })
export class UserService {
  private http = inject(HttpClient);

  createUser(request: CreateUserRequest): Observable<CreateUserResponse> {
    return this.http.post<CreateUserResponse>(`${BASE}/createUser`, request);
  }

  updateUser(request: UpdateUserRequest): Observable<UpdateUserResponse> {
    return this.http.put<UpdateUserResponse>(`${BASE}/updateUser`, request);
  }

  updateEmail(request: UpdateEmailRequest): Observable<UpdateEmailResponse> {
    return this.http.put<UpdateEmailResponse>(`${BASE}/updateEmail`, request);
  }

  changePassword(request: ChangePasswordRequest): Observable<ChangePasswordResponse> {
    return this.http.put<ChangePasswordResponse>(`${BASE}/changePassword`, request);
  }

  getAllUsers(): Observable<User[]> {
    return this.http.get<User[]>(`${BASE}/getAllUsers`);
  }

  getUserById(userId: string): Observable<User> {
    return this.http.get<User>(`${BASE}/getUserProfileData/${userId}`);
  }

  deleteMyAccount(): Observable<void> {
    return this.http.delete<void>(`${BASE}/deleteMyAccount`);
  }

  deleteUser(userId: string): Observable<void> {
    return this.http.delete<void>(`${BASE}/deleteUser/${userId}`);
  }
}
