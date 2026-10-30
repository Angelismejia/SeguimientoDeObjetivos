import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { tap } from 'rxjs/operators';
import { Chart } from 'chart.js';
import { environment } from '../../../environments/environment';

export type Theme = 'Light' | 'Dark';

export interface ThemeDto {
  theme: Theme;
}

// Copia local del tema para pintarlo antes de que responda el backend. La lee
// tambien el script de index.html, asi que el nombre de la clave va en los dos.
const CLAVE_TEMA = 'theme';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private url = `${environment.apiUrl}/users`;

  constructor(private http: HttpClient) {}

  getTheme(userId: number): Observable<ThemeDto> {
    return this.http.get<ThemeDto>(`${this.url}/${userId}/theme`).pipe(
      tap(res => this.aplicar(res.theme))
    );
  }

  setTheme(userId: number, theme: Theme): Observable<ThemeDto> {
    return this.http.put<ThemeDto>(`${this.url}/${userId}/theme`, { theme }).pipe(
      tap(res => this.aplicar(res.theme))
    );
  }

  // Pinta el ultimo tema conocido sin esperar a la red. Si nunca se guardo
  // ninguno, queda el claro.
  aplicarGuardado(): void {
    this.aplicar(this.leerGuardado());
  }

  // Al cerrar sesion la landing y el login vuelven al claro: esas pantallas
  // no tienen variables de tema.
  quitar(): void {
    document.documentElement.removeAttribute('data-theme');
    this.colorearGraficos('Light');
  }

  private aplicar(theme: Theme): void {
    if (theme === 'Dark') {
      document.documentElement.setAttribute('data-theme', 'dark');
    } else {
      document.documentElement.removeAttribute('data-theme');
    }
    try { localStorage.setItem(CLAVE_TEMA, theme); } catch { /* sin storage: solo se pierde la cache */ }
    this.colorearGraficos(theme);
  }

  private leerGuardado(): Theme {
    try {
      return localStorage.getItem(CLAVE_TEMA) === 'Dark' ? 'Dark' : 'Light';
    } catch {
      return 'Light';
    }
  }

  // Chart.js dibuja en un <canvas> y no lee CSS: los ejes, las etiquetas y la
  // grilla toman sus colores de Chart.defaults. Los graficos que ya estan en
  // pantalla no cambian; los toman la proxima vez que se construyen.
  private colorearGraficos(theme: Theme): void {
    const oscuro = theme === 'Dark';
    Chart.defaults.color = oscuro ? '#9ca3af' : '#666';
    Chart.defaults.borderColor = oscuro ? 'rgba(255,255,255,0.08)' : 'rgba(0,0,0,0.1)';
  }
}
