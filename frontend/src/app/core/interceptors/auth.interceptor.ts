import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { BehaviorSubject, catchError, filter, switchMap, take, throwError, from } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { ToastService } from '../services/toast.service';
import { environment } from '../../../environments/environment';

// Shared state across all interceptor calls (module-level singletons)
let isRefreshing = false;
const refreshDone$ = new BehaviorSubject<string | null>(null);

function doLogout(auth: AuthService, router: Router, toast: ToastService): void {
  auth.clearAuth();
  toast.notify('Session expirée — veuillez vous reconnecter', 'warning');
  router.navigate(['/login']);
}

// Refresh the access token, one tab at a time (Web Locks): if another tab already refreshed it
// (the refresh token rotates), reuse its new token instead of sending the old refresh token again
function refreshAccessToken(auth: AuthService, expiredToken: string | null): Promise<string> {
  const run = async (): Promise<string> => {
    const current = auth.getToken();
    if (current && current !== expiredToken) return current;

    const r = await fetch(`${environment.apiUrl}/auth/refresh`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ accessToken: current, refreshToken: auth.getRefreshToken() }),
    });
    if (!r.ok) throw new Error('refresh_failed');
    const data = await r.json();
    auth.setAuth(data.accessToken, data.refreshToken, data.user);
    return data.accessToken;
  };
  return navigator.locks ? navigator.locks.request('gc-auth-refresh', run) : run();
}

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth  = inject(AuthService);
  const router = inject(Router);
  const toast  = inject(ToastService);

  // Skip auth header on login / refresh calls
  const isAuthCall = req.url.includes('/auth/login') || req.url.includes('/auth/refresh');

  const token = auth.getToken();
  const authReq = token && !isAuthCall
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(authReq).pipe(
    catchError(err => {
      // Only intercept 401 on authenticated routes, never on auth endpoints
      if (err.status !== 401 || isAuthCall || router.url.includes('/login')) {
        return throwError(() => err);
      }

      const refreshToken = auth.getRefreshToken();
      const accessToken  = auth.getToken();

      if (!refreshToken || !accessToken) {
        doLogout(auth, router, toast);
        return throwError(() => err);
      }

      if (isRefreshing) {
        // Another request is already refreshing — wait for it, then retry
        return refreshDone$.pipe(
          filter(t => t !== null),
          take(1),
          switchMap(newToken =>
            next(req.clone({ setHeaders: { Authorization: `Bearer ${newToken}` } }))
          )
        );
      }

      isRefreshing = true;
      refreshDone$.next(null);

      return from(refreshAccessToken(auth, token)).pipe(
        // Only a failed refresh logs out; an error from the retried request reaches the caller as usual
        catchError(refreshErr => {
          isRefreshing = false;
          refreshDone$.next(null);
          doLogout(auth, router, toast);
          return throwError(() => refreshErr);
        }),
        switchMap(newToken => {
          isRefreshing = false;
          refreshDone$.next(newToken);
          // Retry the original request with the new token
          return next(req.clone({ setHeaders: { Authorization: `Bearer ${newToken}` } }));
        })
      );
    })
  );
};
