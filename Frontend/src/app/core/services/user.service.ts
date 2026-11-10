import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map, tap } from 'rxjs';
import { User, UpdateUserDto, ChangePasswordDto, UpdateFollowPrivacyDto, DeleteAccountDto } from '../models/user.model';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class UserService {
  private url = `${environment.apiUrl}/users`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<User[]> {
    return this.http.get<User[]>(this.url);
  }

  /** Busca en el servidor (solo cuentas activas, ordenadas por nombre). */
  search(query: string, limit = 20): Observable<User[]> {
    const params = new HttpParams().set('query', query).set('limit', limit);
    return this.http.get<User[]>(this.url, { params });
  }

  getById(id: number): Observable<User> {
    return this.http.get<User>(`${this.url}/${id}`);
  }

  getByUsername(username: string): Observable<User> {
    return this.http.get<User>(`${this.url}/by-username/${username}`);
  }

  update(id: number, data: UpdateUserDto): Observable<User> {
    return this.http.put<User>(`${this.url}/${id}`, data);
  }

  /**
   * Cambiar la contraseña cierra todas las sesiones abiertas, esta incluida: el
   * token lleva un sello de la contraseña con la que se emitio. La API devuelve
   * uno nuevo y se guarda aca para no sacar al usuario de la app.
   */
  changePassword(id: number, data: ChangePasswordDto): Observable<void> {
    return this.http.put<{ accessToken: string }>(`${this.url}/${id}/password`, data).pipe(
      tap(res => localStorage.setItem('token', res.accessToken)),
      map(() => undefined)
    );
  }

  updateFollowPrivacy(id: number, data: UpdateFollowPrivacyDto): Observable<User> {
    return this.http.put<User>(`${this.url}/${id}/allow-follows`, data);
  }

  exportData(id: number): Observable<unknown> {
    return this.http.get(`${this.url}/${id}/export`);
  }

  deleteAccount(id: number, data: DeleteAccountDto): Observable<void> {
    return this.http.delete<void>(`${this.url}/${id}/account`, { body: data });
  }

  uploadPhoto(id: number, file: File): Observable<User> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<User>(`${this.url}/${id}/photo`, formData);
  }
}
