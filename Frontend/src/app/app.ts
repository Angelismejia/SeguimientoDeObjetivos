import { Component, signal } from '@angular/core';
import { RouterOutlet, Router, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs/operators';
import { NavbarComponent } from './shared/components/navbar/navbar.component';
import { AuthService } from './core/services/auth.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, NavbarComponent],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  protected readonly title = signal('Frontend');
  protected readonly showNavbar = signal(false);

  constructor(private router: Router, private authService: AuthService) {
    this.updateNavbarVisibility();
    this.router.events
      .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd))
      .subscribe(() => this.updateNavbarVisibility());
  }

  private updateNavbarVisibility() {
    this.showNavbar.set(this.authService.isLoggedIn());
  }
}
