import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive } from '@angular/router';

// Header global unico para mobile y desktop (antes habia dos: el .topbar de
// app.html para escritorio y el .mobile-topbar dentro de navbar.component
// para movil). Solo trae acciones globales (notificaciones, chat) — avatar y
// cerrar sesion se mudaron a Sidebar/Drawer, que es donde vive "la cuenta".
@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  templateUrl: './app-header.component.html',
  styleUrl: './app-header.component.css'
})
export class AppHeaderComponent {
  @Input() unreadNotifications = 0;
  @Input() unreadChat = 0;
  @Input() menuOpen = false;

  @Output() menuClick = new EventEmitter<void>();
}
