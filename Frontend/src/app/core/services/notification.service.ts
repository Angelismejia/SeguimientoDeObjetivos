import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, Subject } from 'rxjs';
import { tap } from 'rxjs/operators';
import { Notification } from '../models/notification.model';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private url = `${environment.apiUrl}/notifications`;

  private readonly unreadChangedSubject = new Subject<void>();
  /**
   * Avisa que el conteo de no leidas cambio. El header lo refresca cada 30s por
   * su cuenta; sin esto, al volver de la pantalla de notificaciones el numerito
   * se quedaba puesto hasta medio minuto despues de haberlas visto.
   */
  readonly unreadChanged = this.unreadChangedSubject.asObservable();

  constructor(private http: HttpClient) {}

  getAll(userId: number): Observable<Notification[]> {
    return this.http.get<Notification[]>(`${this.url}?userId=${userId}`);
  }

  getUnread(userId: number): Observable<Notification[]> {
    return this.http.get<Notification[]>(`${this.url}/unread?userId=${userId}`);
  }

  markAsRead(id: number): Observable<void> {
    return this.http.patch<void>(`${this.url}/${id}/read`, {});
  }

  /** Marca todas las del usuario autenticado. El backend saca el id del token. */
  markAllAsRead(): Observable<void> {
    return this.http.patch<void>(`${this.url}/read-all`, {}).pipe(
      tap(() => this.unreadChangedSubject.next())
    );
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.url}/${id}`);
  }
}
