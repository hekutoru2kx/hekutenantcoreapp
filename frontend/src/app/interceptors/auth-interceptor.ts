import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { Auth } from '../services/auth';
import { TranslocoService } from '@jsverse/transloco';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(Auth);
  const router = inject(Router);
  const token = auth.token();
  const transloco = inject(TranslocoService);
  const lang = transloco.getActiveLang();

  const authReq = req.clone({
    headers: req.headers
      .set('Accept-Language', lang)
      .set('Authorization', token ? `Bearer ${token}` : '')
  });

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 0) {
        // Network error - backend unreachable
        console.error('Network error: backend unreachable');
      } else if (error.status === 401) {
        // Token expired or invalid
        auth.logout();
        router.navigate(['/login']);
      } else if (error.status === 403) {
        // ActiveUserMiddleware reports real deactivation/tenant-revocation with a JSON body
        // ({ message: ... }). A plain policy/role denial (missing permission for this one
        // action) comes back with an empty body and must NOT log an otherwise-valid user out.
        const message = typeof error.error === 'object' && error.error !== null ? error.error.message : null;
        if (message) {
          auth.logout();
          router.navigate(['/login'], {
            queryParams: { reason: 'deactivated' }
          });
        }
      }
      return throwError(() => error);
    })
  );
};