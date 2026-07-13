import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../core/services/auth.service';
import { NotificationService } from '../../core/services/notification.service';
import { Notification } from '../../core/models/notification.model';

@Component({
  selector: 'app-notifications',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './notifications.component.html',
  styleUrl: './notifications.component.css'
})
export class NotificationsComponent implements OnInit {
  loading = signal(true);
  loadError = signal(false);
  notifications = signal<Notification[]>([]);

  constructor(
    private auth: AuthService,
    private notificationService: NotificationService
  ) {}

  ngOnInit(): void {
    this.loadAll();
  }

  private loadAll(): void {
    this.loading.set(true);
    this.loadError.set(false);
    this.notificationService.getAll(this.auth.getUserId()).subscribe({
      next: notifications => {
        this.notifications.set(notifications);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.loadError.set(true);
      }
    });
  }

  markAsRead(notification: Notification): void {
    if (notification.isRead) return;
    this.notificationService.markAsRead(notification.id).subscribe(() => {
      this.notifications.set(
        this.notifications().map(n => n.id === notification.id ? { ...n, isRead: true } : n)
      );
    });
  }

  delete(notification: Notification): void {
    this.notificationService.delete(notification.id).subscribe(() => {
      this.notifications.set(this.notifications().filter(n => n.id !== notification.id));
    });
  }
}
