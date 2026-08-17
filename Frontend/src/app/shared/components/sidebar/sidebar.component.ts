import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive } from '@angular/router';

// Sidebar fijo de escritorio (>=1024px, ver app.css). Reemplaza el .sidebar
// que antes vivia dentro de navbar.component. Siempre expandido — se probo
// colapsable pero no hacia falta, asi que se saco esa complejidad.
@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.css'
})
export class SidebarComponent {
  @Input() userName = '';
  @Input() userPhotoUrl: string | null = null;

  // App (app.ts) es dueño de la sesion de chat/SignalR y ya sabe como cerrarla
  // ordenadamente al hacer logout — este componente solo avisa la intencion.
  @Output() logoutClick = new EventEmitter<void>();

  navLinks = [
    { path: '/dashboard', label: 'Dashboard', icon: 'dashboard' },
    { path: '/objectives', label: 'Objetivos', icon: 'track_changes' },
    { path: '/tasks', label: 'Tareas', icon: 'task_alt' },
    { path: '/statistics', label: 'Estadísticas', icon: 'query_stats' },
  ];

  // "Comunidad" ya tiene ruta propia, con una pagina de "en construccion"
  // mientras se arma la seccion. Los amigos y seguidores siguen viviendo en
  // Perfil, y la pagina enlaza ahi.
  personalLinks = [
    { path: '/diary', label: 'Diario', icon: 'menu_book', fragment: undefined as string | undefined },
    { path: '/badges', label: 'Logros', icon: 'workspace_premium', fragment: undefined as string | undefined },
    { path: '/community', label: 'Comunidad', icon: 'group', fragment: undefined as string | undefined },
  ];
}
