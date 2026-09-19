import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../core/services/auth.service';
import { NotificationService } from '../../core/services/notification.service';
import { Notification } from '../../core/models/notification.model';
import { mensajeDeError } from '../../core/utils/http-error.util';

@Component({
  selector: 'app-notifications',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './notifications.component.html',
  styleUrl: './notifications.component.css'
})
export class NotificationsComponent implements OnInit {
  loading = signal(true);
  loadError = signal('');
  // Marcar o borrar una alerta fallaba en silencio: el usuario tocaba el boton,
  // no pasaba nada y no habia forma de saber por que.
  actionError = signal('');
  notifications = signal<Notification[]>([]);

  constructor(
    private auth: AuthService,
    private notificationService: NotificationService
  ) {}

  ngOnInit(): void {
    this.loadAll();
  }

  // Abrir la pantalla cuenta como haberlas visto, asi que se marcan todas y el
  // contador del header deja de insistir. Se mantiene el estilo "sin leer" en la
  // lista que se acaba de cargar, para que se distinga cual era nueva al entrar.
  private marcarTodasComoVistas(notificaciones: Notification[]): void {
    if (!notificaciones.some(n => !n.isRead)) return;

    this.notificationService.markAllAsRead().subscribe();
  }

  loadAll(): void {
    this.loading.set(true);
    this.loadError.set('');
    this.notificationService.getAll(this.auth.getUserId()).subscribe({
      next: notifications => {
        this.notifications.set(notifications);
        this.loading.set(false);
        this.marcarTodasComoVistas(notifications);
      },
      error: (e) => {
        this.loading.set(false);
        this.loadError.set(mensajeDeError(e, 'No se pudieron cargar tus alertas.'));
      }
    });
  }

  markAsRead(notification: Notification): void {
    if (notification.isRead) return;
    this.actionError.set('');
    this.notificationService.markAsRead(notification.id).subscribe({
      next: () => {
        this.notifications.set(
          this.notifications().map(n => n.id === notification.id ? { ...n, isRead: true } : n)
        );
      },
      error: (e) => this.actionError.set(
        mensajeDeError(e, 'No se pudo marcar la alerta como leída.'))
    });
  }

  delete(notification: Notification): void {
    this.actionError.set('');
    this.notificationService.delete(notification.id).subscribe({
      next: () => {
        this.notifications.set(this.notifications().filter(n => n.id !== notification.id));
      },
      error: (e) => this.actionError.set(mensajeDeError(e, 'No se pudo borrar la alerta.'))
    });
  }

  iconFor(notification: Notification): string {
    switch (notification.type) {
      case 'objective_completed': return 'emoji_events';
      case 'badge_awarded': return 'military_tech';
      case 'new_follower': return 'person_add';
      default: return 'notifications';
    }
  }
}
