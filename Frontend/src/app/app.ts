import { Component, signal } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive, Router, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs/operators';
import { interval, startWith } from 'rxjs';
import { NavbarComponent } from './shared/components/navbar/navbar.component';
import { AuthService } from './core/services/auth.service';
import { NotificationService } from './core/services/notification.service';
import { UserService } from './core/services/user.service';
import { environment } from '../environments/environment';

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
  protected readonly userPhotoUrl = signal<string | null>(null);
  protected readonly unreadCount = signal(0);

  private apiOrigin = environment.apiUrl.replace('/api', '');

  constructor(
    private router: Router,
    private authService: AuthService,
    private notificationService: NotificationService,
    private userService: UserService
  ) {
    this.updateNavbarVisibility();
    this.router.events
      .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd))
      .subscribe(() => this.updateNavbarVisibility());

    interval(30000).pipe(startWith(0)).subscribe(() => this.refreshUnreadCount());
  }

  private updateNavbarVisibility() {
    const loggedIn = this.authService.isLoggedIn();
    this.showNavbar.set(loggedIn);
    this.userName.set(this.authService.getName());

    if (loggedIn) {
      this.userService.getById(this.authService.getUserId()).subscribe({
        next: user => this.userPhotoUrl.set(user.profilePhotoUrl ? this.apiOrigin + user.profilePhotoUrl : null),
        error: () => {}
      });
    } else {
      this.userPhotoUrl.set(null);
    }
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
