import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive } from '@angular/router';

// Barra inferior de movil/tablet (<1024px). Reemplaza el .tab-bar morado que
// vivia dentro de navbar.component — mismo destino final (los 5 accesos
// frecuentes), pero fondo blanco y estado activo como "pill" morado, siguiendo
// el mockup nuevo en vez del look morado pesado anterior.
@Component({
  selector: 'app-bottom-nav',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  templateUrl: './bottom-nav.component.html',
  styleUrl: './bottom-nav.component.css'
})
export class BottomNavComponent {
  links = [
    { path: '/dashboard', label: 'Dashboard', icon: 'dashboard' },
    { path: '/objectives', label: 'Objetivos', icon: 'track_changes' },
    { path: '/tasks', label: 'Tareas', icon: 'task_alt' },
    { path: '/statistics', label: 'Estadísticas', icon: 'query_stats' },
    { path: '/profile', label: 'Perfil', icon: 'account_circle' },
  ];
}
