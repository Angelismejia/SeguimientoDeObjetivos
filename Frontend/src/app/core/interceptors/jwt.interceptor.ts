import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';

export const jwtInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const token = auth.getToken();

  if (token) {
    req = req.clone({
      setHeaders: { Authorization: `Bearer ${token}` }
    });
  }

  // Los intentos de login/registro devuelven 401/400 por credenciales inválidas,
  // no porque la sesión haya expirado: no deben disparar el redirect de acá abajo.
  const isAuthEndpoint = req.url.includes('/auth/login') || req.url.includes('/auth/register');

  return next(req).pipe(
    catchError(err => {
      // Una pantalla suele disparar varias peticiones en paralelo (forkJoin): si el token
      // ya expiró, todas fallan casi a la vez y no queremos navegar más de una vez por eso.
      if (err.status === 401 && !isAuthEndpoint && localStorage.getItem('token')) {
        localStorage.clear();
        router.navigate(['/login'], { queryParams: { sessionExpired: '1' } });
      }
      return throwError(() => err);
    })
  );
};
