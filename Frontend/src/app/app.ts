import { Component, signal } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive, Router, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs/operators';
import { interval, startWith } from 'rxjs';
import { NavbarComponent } from './shared/components/navbar/navbar.component';
import { AuthService } from './core/services/auth.service';
import { NotificationService } from './core/services/notification.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, NavbarComponent],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  protected readonly title = signal('Frontend');
  protected readonly showNavbar = signal(false);
  protected readonly userName = signal('');
  protected readonly unreadCount = signal(0);

  constructor(
    private router: Router,
    private authService: AuthService,
    private notificationService: NotificationService
  ) {
    this.updateNavbarVisibility();
    this.router.events
      .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd))
      .subscribe(() => this.updateNavbarVisibility());

    interval(30000).pipe(startWith(0)).subscribe(() => this.refreshUnreadCount());
  }

  private updateNavbarVisibility() {
    this.showNavbar.set(this.authService.isLoggedIn());
    this.userName.set(this.authService.getName());
  }

  private refreshUnreadCount(): void {
    if (!this.authService.isLoggedIn()) {
      this.unreadCount.set(0);
      return;
    }
    this.notificationService.getUnread(this.authService.getUserId()).subscribe({
      next: list => this.unreadCount.set(list.length),
      error: () => {}
    });
  }

  logout(): void {
    this.authService.logout();
  }
}
