import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Follow, UserSummary } from '../models/follow.model';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class FollowService {
  private url = `${environment.apiUrl}/follows`;

  constructor(private http: HttpClient) {}

  getFollowers(userId: number): Observable<UserSummary[]> {
    return this.http.get<UserSummary[]>(`${this.url}/followers?userId=${userId}`);
  }

  getFollowing(userId: number): Observable<UserSummary[]> {
    return this.http.get<UserSummary[]>(`${this.url}/following?userId=${userId}`);
  }

  /**
   * Quien sigue sale del token en el backend. Se mantiene el parametro para no
   * tocar los llamadores, pero ya no viaja: mandarlo permitia hacer que otra
   * persona siguiera o dejara de seguir a quien fuera.
   */
  follow(_followerId: number, followingId: number): Observable<Follow> {
    return this.http.post<Follow>(this.url, { followingId });
  }

  unfollow(_followerId: number, followingId: number): Observable<void> {
    return this.http.delete<void>(`${this.url}?followingId=${followingId}`);
  }
}
