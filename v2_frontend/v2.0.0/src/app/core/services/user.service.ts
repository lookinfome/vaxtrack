import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  User, PagedUsersResponse, DeleteMyAccountRequest,
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

  uploadProfilePicture(userId: string, file: File): Observable<UpdateUserResponse> {
    const form = new FormData();
    form.append('userId', userId);
    form.append('file', file);
    return this.http.post<UpdateUserResponse>(`${BASE}/uploadProfilePicture`, form);
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

  deleteMyAccount(request: DeleteMyAccountRequest): Observable<void> {
    return this.http.request<void>('delete', `${BASE}/deleteMyAccount`, { body: request });
  }

  deleteUser(userId: string): Observable<void> {
    return this.http.delete<void>(`${BASE}/deleteUser/${userId}`);
  }

  getUsersPaged(filters: { name?: string; phone?: string; userId?: string; userUid?: string; page: number; pageSize: number }): Observable<PagedUsersResponse> {
    let params = new HttpParams().set('page', filters.page).set('pageSize', filters.pageSize);
    if (filters.name) params = params.set('name', filters.name);
    if (filters.phone) params = params.set('phone', filters.phone);
    if (filters.userId) params = params.set('userId', filters.userId);
    if (filters.userUid) params = params.set('userUid', filters.userUid);
    return this.http.get<PagedUsersResponse>(`${BASE}/getUsersPaged`, { params });
  }

  disableUser(userId: string, comment: string): Observable<User> {
    return this.http.put<User>(`${BASE}/disableUser/${userId}`, { comment });
  }

  approveUserReactivation(userId: string, comment?: string): Observable<User> {
    return this.http.put<User>(`${BASE}/approveUserReactivation/${userId}`, { comment });
  }

  rejectUserReactivation(userId: string, comment?: string): Observable<User> {
    return this.http.put<User>(`${BASE}/rejectUserReactivation/${userId}`, { comment });
  }

  promoteToAdmin(userId: string): Observable<User> {
    return this.http.put<User>(`${BASE}/promoteToAdmin/${userId}`, {});
  }

  demoteFromAdmin(userId: string): Observable<User> {
    return this.http.put<User>(`${BASE}/demoteFromAdmin/${userId}`, {});
  }
}
