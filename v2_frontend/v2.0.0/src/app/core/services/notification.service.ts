import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AppNotification } from '../models/notification.model';

const BASE = '/api/vaxtrack/v1/notification';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private http = inject(HttpClient);

  getMyNotifications(): Observable<AppNotification[]> {
    return this.http.get<AppNotification[]>(`${BASE}/getMyNotifications`);
  }

  getUnreadCount(): Observable<number> {
    return this.http.get<number>(`${BASE}/getUnreadCount`);
  }

  markAsRead(id: number): Observable<void> {
    return this.http.put<void>(`${BASE}/markAsRead/${id}`, {});
  }

  markAllAsRead(): Observable<void> {
    return this.http.put<void>(`${BASE}/markAllAsRead`, {});
  }
}
