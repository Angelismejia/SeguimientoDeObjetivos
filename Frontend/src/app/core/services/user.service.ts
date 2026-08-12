import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { User, UpdateUserDto, ChangePasswordDto, UpdateFollowPrivacyDto, DeleteAccountDto } from '../models/user.model';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class UserService {
  private url = `${environment.apiUrl}/users`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<User[]> {
    return this.http.get<User[]>(this.url);
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

  changePassword(id: number, data: ChangePasswordDto): Observable<void> {
    return this.http.put<void>(`${this.url}/${id}/password`, data);
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
