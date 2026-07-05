import { Component, OnInit, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { DatePipe } from '@angular/common';
import { interval } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
import { AppNotification } from '../../../core/models/notification.model';

const POLL_INTERVAL_MS = 30000;

@Component({
  selector: 'app-nav-bar',
  imports: [RouterLink, RouterLinkActive, DatePipe],
  templateUrl: './nav-bar.html',
})
export class NavBarComponent implements OnInit {
  readonly authService = inject(AuthService);
  private notificationService = inject(NotificationService);
  private router = inject(Router);

  unreadCount = signal(0);
  showDropdown = signal(false);
  notifications = signal<AppNotification[]>([]);
  notificationsLoading = signal(false);

  ngOnInit(): void {
    this.refreshUnreadCount();
    interval(POLL_INTERVAL_MS).subscribe(() => this.refreshUnreadCount());
  }

  private refreshUnreadCount(): void {
    if (!this.authService.isLoggedIn()) return;
    this.notificationService.getUnreadCount().subscribe({
      next: (count) => this.unreadCount.set(count),
      error: () => {}
    });
  }

  toggleDropdown(): void {
    this.showDropdown.set(!this.showDropdown());
    if (this.showDropdown()) this.loadNotifications();
  }

  private loadNotifications(): void {
    this.notificationsLoading.set(true);
    this.notificationService.getMyNotifications().subscribe({
      next: (list) => { this.notifications.set(list); this.notificationsLoading.set(false); },
      error: () => { this.notifications.set([]); this.notificationsLoading.set(false); }
    });
  }

  openNotification(n: AppNotification): void {
    this.showDropdown.set(false);
    if (!n.isRead) {
      this.notificationService.markAsRead(n.id).subscribe({
        next: () => this.refreshUnreadCount(),
        error: () => {}
      });
    }
    if (n.linkPath) this.router.navigateByUrl(n.linkPath);
  }

  markAllAsRead(): void {
    this.notificationService.markAllAsRead().subscribe({
      next: () => {
        this.notifications.set(this.notifications().map(n => ({ ...n, isRead: true })));
        this.unreadCount.set(0);
      },
      error: () => {}
    });
  }

  logout(): void {
    this.authService.logout().subscribe({
      next:  () => this.router.navigate(['/auth/login']),
      error: () => {
        this.authService.clearSession();
        this.router.navigate(['/auth/login']);
      }
    });
  }
}
