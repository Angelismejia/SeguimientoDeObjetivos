import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { FriendStreak, FriendStreakInvitation } from '../models/friend-streak.model';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class FriendStreakService {
  private url = `${environment.apiUrl}/friendstreaks`;

  constructor(private http: HttpClient) {}

  // El backend saca la identidad del token. Se conservan los parametros para no
  // tocar los llamadores, pero ya no viajan en la URL: mandarlos permitia leer
  // las invitaciones de cualquiera y aceptar o rechazar en su nombre.

  getForUser(_userId: number): Observable<FriendStreak[]> {
    return this.http.get<FriendStreak[]>(this.url);
  }

  getReceivedInvitations(_userId: number): Observable<FriendStreakInvitation[]> {
    return this.http.get<FriendStreakInvitation[]>(`${this.url}/invitations/received`);
  }

  getSentInvitations(_userId: number): Observable<FriendStreakInvitation[]> {
    return this.http.get<FriendStreakInvitation[]>(`${this.url}/invitations/sent`);
  }

  invite(_fromUserId: number, toUserId: number): Observable<FriendStreakInvitation> {
    return this.http.post<FriendStreakInvitation>(`${this.url}/invitations`, { toUserId });
  }

  accept(invitationId: number, _userId: number): Observable<FriendStreak> {
    return this.http.post<FriendStreak>(`${this.url}/invitations/${invitationId}/accept`, {});
  }

  reject(invitationId: number, _userId: number): Observable<void> {
    return this.http.post<void>(`${this.url}/invitations/${invitationId}/reject`, {});
  }
}
