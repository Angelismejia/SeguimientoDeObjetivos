import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { User } from '../models/user.model';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class UserService {
  private url = `${environment.apiUrl}/users`;

  constructor(private http: HttpClient) {}

  getById(id: number): Observable<User> {
    return this.http.get<User>(`${this.url}/${id}`);
  }

  getByUsername(username: string): Observable<User> {
    return this.http.get<User>(`${this.url}/by-username/${username}`);
  }

  uploadPhoto(id: number, file: File): Observable<User> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<User>(`${this.url}/${id}/photo`, formData);
  }
}
