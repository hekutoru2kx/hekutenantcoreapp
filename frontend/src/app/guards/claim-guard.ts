import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { Auth } from '../services/auth';

export function claimGuard(module: string, action: string): CanActivateFn {
  return () => {
    const auth = inject(Auth);
    const router = inject(Router);

    if (auth.hasClaim(module, action)) {
      return true;
    }

    router.navigate(['/']);
    return false;
  };
}
