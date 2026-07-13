import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Badge } from '../models/badge.model';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class BadgeService {
  private url = `${environment.apiUrl}/badges`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<Badge[]> {
    return this.http.get<Badge[]>(this.url);
  }

  getByUser(userId: number): Observable<Badge[]> {
    return this.http.get<Badge[]>(`${this.url}/by-user/${userId}`);
  }
}
