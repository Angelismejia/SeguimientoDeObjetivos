import { Component, signal } from '@angular/core';
import { RouterOutlet, Router, NavigationEnd, NavigationError } from '@angular/router';
import { filter } from 'rxjs/operators';
import { interval, startWith } from 'rxjs';
import { AppHeaderComponent } from './shared/components/app-header/app-header.component';
import { SidebarComponent } from './shared/components/sidebar/sidebar.component';
import { MobileDrawerComponent } from './shared/components/mobile-drawer/mobile-drawer.component';
import { BottomNavComponent } from './shared/components/bottom-nav/bottom-nav.component';
import { AuthService } from './core/services/auth.service';
import { NotificationService } from './core/services/notification.service';
import { UserService } from './core/services/user.service';
import { environment } from '../environments/environment';
import { ChatService } from './core/services/chat.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, AppHeaderComponent, SidebarComponent, MobileDrawerComponent, BottomNavComponent],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  protected readonly title = signal('Frontend');
  protected readonly showNavbar = signal(false);
  protected readonly userName = signal('');
  protected readonly userPhotoUrl = signal<string | null>(null);
  protected readonly unreadCount = signal(0);
  protected readonly drawerOpen = signal(false);

  private apiOrigin = environment.apiUrl.replace('/api', '');

  constructor(
    private router: Router,
    private authService: AuthService,
    private notificationService: NotificationService,
    private userService: UserService,
    protected chatService: ChatService
  ) {
    this.updateNavbarVisibility();
    this.router.events
      .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd))
      .subscribe(() => {
        this.updateNavbarVisibility();
        this.drawerOpen.set(false);
      });

    // Si una navegación falla (ej: un chunk lazy-load que ya no existe porque se
    // redesplegó el front con nuevos hashes), recargamos la página en vez de dejar
    // la pantalla trabada indefinidamente (ej: el botón de login en "Ingresando...").
    this.router.events
      .pipe(filter((event): event is NavigationError => event instanceof NavigationError))
      .subscribe(event => window.location.href = event.url);

    interval(30000).pipe(startWith(0)).subscribe(() => {
      this.refreshUnreadCount();
      this.ensureChatConnected();
    });

    // Al marcarlas todas como leidas el contador tiene que bajar en el momento,
    // sin esperar al proximo ciclo de 30s.
    this.notificationService.unreadChanged.subscribe(() => this.refreshUnreadCount());
  }

  // Si la conexion de chat se cae (red inestable, celular viejo, etc.) y el
  // reintento automatico de SignalR se agota, esto la vuelve a levantar sola
  // en vez de dejar al usuario sin mensajes hasta que recargue la pagina.
  private ensureChatConnected(): void {
    if (this.authService.isLoggedIn() && !this.chatService.connected()) {
      this.chatService.connect();
    }
  }

  private updateNavbarVisibility() {
    const loggedIn = this.authService.isLoggedIn();
    this.showNavbar.set(loggedIn);
    this.userName.set(this.authService.getName());

    if (loggedIn) {
      this.chatService.connect();
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
    this.chatService.disconnect();
    this.authService.logout();
  }

  openDrawer(): void {
    this.drawerOpen.set(true);
  }

  closeDrawer(): void {
    this.drawerOpen.set(false);
  }
}
