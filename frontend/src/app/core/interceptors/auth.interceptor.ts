import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const token = localStorage.getItem('gc_token');
  const authReq = token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(authReq).pipe(
    catchError(err => {
      if (err.status === 401 && !window.location.pathname.includes('/login')) {
        localStorage.removeItem('gc_token');
        localStorage.removeItem('gc_user');
        window.location.href = '/login';
      }
      return throwError(() => err);
    })
  );
};
