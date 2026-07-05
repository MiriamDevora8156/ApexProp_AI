import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../../core/services/auth';

export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isLoggedIn()) {
    return true;
  }

  // שמור את ה-URL המקורי כדי לחזור אליו אחרי Login
  return router.createUrlTree(['/'], {
    queryParams: { returnUrl: state.url }
  });
};
