import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthSession } from './auth-session';

/** Protects the routes that require an authenticated user. */
export const authGuard: CanActivateFn = (_route, state) => {
  const session = inject(AuthSession);
  const router = inject(Router);

  if (session.isAuthenticated()) {
    return true;
  }

  return router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
};

/** Sends the authenticated users away from the login page. */
export const anonymousGuard: CanActivateFn = () => {
  const session = inject(AuthSession);
  const router = inject(Router);

  return session.isAuthenticated() ? router.createUrlTree(['/projects']) : true;
};
