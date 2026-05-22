import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { ToastService } from '../services/toast.service';
import { environment } from '../../../environments/environment';

let sessionExpiredShown = false;

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const toast = inject(ToastService);
  const token = localStorage.getItem('gc_token');
  const authReq = token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(authReq).pipe(
    catchError(err => {
      if (err.status === 401 && !router.url.includes('/login')) {
        if (!sessionExpiredShown) {
          sessionExpiredShown = true;
          toast.notify('Session expirée — veuillez vous reconnecter', 'warning');
          setTimeout(() => { sessionExpiredShown = false; }, 5000);
          // Log session expiry as disconnection — use fetch to avoid circular dependency
          if (token) {
            fetch(`${environment.apiUrl}/auth/logout`, {
              method: 'POST',
              headers: { Authorization: `Bearer ${token}`, 'Content-Type': 'application/json' },
            }).catch(() => {});
          }
        }
        auth.clearAuth();
        router.navigate(['/login']);
      }
      return throwError(() => err);
    })
  );
};
