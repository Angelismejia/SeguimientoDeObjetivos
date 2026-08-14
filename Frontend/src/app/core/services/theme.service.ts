import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface ThemeDto {
  theme: 'Light' | 'Dark';
}

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private url = `${environment.apiUrl}/users`;

  constructor(private http: HttpClient) {}

  getTheme(userId: number): Observable<ThemeDto> {
    return this.http.get<ThemeDto>(`${this.url}/${userId}/theme`);
  }

  setTheme(userId: number, theme: 'Light' | 'Dark'): Observable<ThemeDto> {
    return this.http.put<ThemeDto>(`${this.url}/${userId}/theme`, { theme });
  }
}
