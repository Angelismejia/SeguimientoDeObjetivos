import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { FriendStreak, FriendStreakInvitation } from '../models/friend-streak.model';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class FriendStreakService {
  private url = `${environment.apiUrl}/friendstreaks`;

  constructor(private http: HttpClient) {}

  getForUser(userId: number): Observable<FriendStreak[]> {
    return this.http.get<FriendStreak[]>(`${this.url}?userId=${userId}`);
  }

  getReceivedInvitations(userId: number): Observable<FriendStreakInvitation[]> {
    return this.http.get<FriendStreakInvitation[]>(`${this.url}/invitations/received?userId=${userId}`);
  }

  getSentInvitations(userId: number): Observable<FriendStreakInvitation[]> {
    return this.http.get<FriendStreakInvitation[]>(`${this.url}/invitations/sent?userId=${userId}`);
  }

  invite(fromUserId: number, toUserId: number): Observable<FriendStreakInvitation> {
    return this.http.post<FriendStreakInvitation>(`${this.url}/invitations?fromUserId=${fromUserId}`, { toUserId });
  }

  accept(invitationId: number, userId: number): Observable<FriendStreak> {
    return this.http.post<FriendStreak>(`${this.url}/invitations/${invitationId}/accept?userId=${userId}`, {});
  }

  reject(invitationId: number, userId: number): Observable<void> {
    return this.http.post<void>(`${this.url}/invitations/${invitationId}/reject?userId=${userId}`, {});
  }
}
