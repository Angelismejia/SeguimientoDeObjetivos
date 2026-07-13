import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { interval, startWith } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.css']
})
export class NavbarComponent {
  name: string = '';
  unreadCount = signal(0);

  links = [
    { path: '/dashboard', label: 'Dashboard', icon: 'dashboard' },
    { path: '/objectives', label: 'Objetivos', icon: 'track_changes' },
    { path: '/diary', label: 'Diario', icon: 'menu_book' },
    { path: '/badges', label: 'Insignias', icon: 'workspace_premium' },
  ];

  constructor(
    private authService: AuthService,
    private notificationService: NotificationService
  ) {
    this.name = this.authService.getName();
    interval(30000).pipe(startWith(0)).subscribe(() => {
      this.notificationService.getUnread(this.authService.getUserId()).subscribe({
        next: list => this.unreadCount.set(list.length),
        error: () => {}
      });
    });
  }

  logout() {
    this.authService.logout();
  }
}