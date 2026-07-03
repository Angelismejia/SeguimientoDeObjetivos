import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.css']
})
export class NavbarComponent {
  name: string = '';
  menuOpen = false;

  links = [
    { path: '/dashboard', label: 'Dashboard', icon: 'dashboard' },
    { path: '/objectives', label: 'Objetivos', icon: 'track_changes' },
    { path: '/tasks', label: 'Tareas', icon: 'task_alt' },
    { path: '/categories', label: 'Categorías', icon: 'category' },
    { path: '/diary', label: 'Diario', icon: 'menu_book' },
    { path: '/notifications', label: 'Alertas', icon: 'notifications' },
    { path: '/badges', label: 'Insignias', icon: 'workspace_premium' },
  ];

  constructor(private authService: AuthService) {
    this.name = this.authService.getName();
  }

  toggleMenu() {
    this.menuOpen = !this.menuOpen;
  }

  closeMenu() {
    this.menuOpen = false;
  }

  logout() {
    this.authService.logout();
  }
}