import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthStore } from '../auth/auth.store';

/** Protects routes like "My Bookings" — redirects to /auth/login, preserving the intended destination. */
export const authGuard: CanActivateFn = (_route, state) => {
  const authStore = inject(AuthStore);
  const router = inject(Router);

  if (authStore.isAuthenticated()) return true;

  return router.createUrlTree(['/auth/login'], { queryParams: { redirectTo: state.url } });
};
