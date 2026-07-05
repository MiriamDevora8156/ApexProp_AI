import { inject } from '@angular/core';
import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../services/auth';

export const jwtInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const token = authService.getToken();

  const authReq = token && req.url.includes('localhost:7215')
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(authReq).pipe(
    catchError((err: HttpErrorResponse) => {
      // 401 = טוקן פג תוקף או לא תקין
      // מנקים את הסשן ומחזירים למסך התחברות עם returnUrl
      if (err.status === 401 && !req.url.includes('/auth/')) {
        authService.logout();
        router.navigate(['/'], {
          queryParams: { returnUrl: router.url }
        });
      }
      return throwError(() => err);
    })
  );
};