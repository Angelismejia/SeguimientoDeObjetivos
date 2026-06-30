import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable } from 'rxjs';
import { tap } from 'rxjs/operators';
import { LoginRequest, LoginResponse, RegisterRequest } from '../models/user.model';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private url = `${environment.apiUrl}/auth`;

  constructor(private http: HttpClient, private router: Router) {}

    return this.http.post<LoginResponse>(`${this.url}/login`, data).pipe(
      tap(res => {
        localStorage.setItem('token', res.accessToken);
        localStorage.setItem('userId', res.user.id.toString());
        localStorage.setItem('username', res.user.username);
        localStorage.setItem('name', res.user.name);
      })
    );
  }

  register(data: RegisterRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.url}/register`, data);
  }

  logout(): void {
    localStorage.clear();
    this.router.navigate(['/login']);
  }

  getToken(): string | null {
    return localStorage.getItem('token');
  }

  isLoggedIn(): boolean {
    return !!this.getToken();ok p
  }

  getUserId(): number {
    return Number(localStorage.getItem('userId'));
  }

  getName(): string {
    return localStorage.getItem('name') ?? localStorage.getItem('username') ?? '';
  }
}
