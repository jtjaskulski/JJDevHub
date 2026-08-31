import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';

import { AuthService } from './auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const token = auth.token();

  const authorized = token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(authorized).pipe(
    catchError((err: unknown) => {
      if (err instanceof HttpErrorResponse && err.status === 401 && !isAuthAttempt(req.url)) {
        auth.logout();
      }
      return throwError(() => err);
    }),
  );
};

function isAuthAttempt(url: string): boolean {
  return url.includes('/api/auth/login') || url.includes('/api/auth/register');
}
