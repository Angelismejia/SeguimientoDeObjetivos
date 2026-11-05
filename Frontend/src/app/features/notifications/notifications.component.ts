import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../core/services/auth.service';
import { NotificationService } from '../../core/services/notification.service';
import { Notification } from '../../core/models/notification.model';
import { mensajeDeError } from '../../core/utils/http-error.util';
import { fechaDelBackend } from '../../core/utils/fecha.util';

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

  // Se cargan de a POR_PAGINA. El cursor es la ultima alerta que mando el
  // servidor, no la ultima de la lista: si se borra una, el cursor no cambia.
  private readonly POR_PAGINA = 20;
  private cursor: number | undefined;
  hasMore = signal(false);
  loadingMore = signal(false);
  loadMoreError = signal('');

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
    this.notificationService.getPage(this.auth.getUserId(), this.POR_PAGINA).subscribe({
      next: notifications => {
        this.notifications.set(notifications);
        this.recibirPagina(notifications);
        this.loading.set(false);
        this.marcarTodasComoVistas(notifications);
      },
      error: (e) => {
        this.loading.set(false);
        this.loadError.set(mensajeDeError(e, 'No se pudieron cargar tus alertas.'));
      }
    });
  }

  loadMore(): void {
    if (this.loadingMore() || !this.hasMore()) return;
    this.loadingMore.set(true);
    this.loadMoreError.set('');
    this.notificationService.getPage(this.auth.getUserId(), this.POR_PAGINA, this.cursor).subscribe({
      next: page => {
        this.notifications.update(current => [...current, ...page]);
        this.recibirPagina(page);
        this.loadingMore.set(false);
      },
      error: (e) => {
        this.loadingMore.set(false);
        this.loadMoreError.set(mensajeDeError(e, 'No se pudieron cargar más alertas.'));
      }
    });
  }

  // Una pagina llena significa que puede haber mas; una incompleta, que no.
  private recibirPagina(page: Notification[]): void {
    if (page.length > 0) this.cursor = page[page.length - 1].id;
    this.hasMore.set(page.length === this.POR_PAGINA);
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

  // createdAt es un instante, no un dia: pasa por fechaDelBackend para que la
  // hora no quede corrida si el JSON llega sin la Z. El backend ya la manda, pero
  // el helper lo deja igual, asi que esto sigue siendo correcto en ambos casos.
  cuando(notification: Notification): Date {
    return fechaDelBackend(notification.createdAt);
  }

  iconFor(notification: Notification): string {
    switch (notification.type) {
      case 'objective_completed': return 'emoji_events';
      case 'badge_awarded': return 'military_tech';
      case 'new_follower': return 'person_add';
      case 'friend_objective_completed': return 'celebration';
      case 'objective_liked': return 'favorite';
      case 'objective_comment': return 'chat_bubble';
      default: return 'notifications';
    }
  }
}
