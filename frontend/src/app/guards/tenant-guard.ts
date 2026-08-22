import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { Auth } from '../services/auth';

export const tenantGuard: CanActivateFn = () => {
  const auth = inject(Auth);
  const router = inject(Router);

  if (auth.currentUser()?.tenantId != null) {
    return true;
  }

  router.navigate(['/tenant-picker']);
  return false;
};
