import {
  HttpErrorResponse,
  HttpEvent,
  HttpHandlerFn,
  HttpInterceptorFn,
  HttpRequest,
} from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, Observable, switchMap, throwError } from 'rxjs';
import { API_BASE_URL } from '../api/api-config';
import { AuthSession } from './auth-session';

function isAuthEndpoint(url: string, baseUrl: string): boolean {
  return url.startsWith(`${baseUrl}/Auth/`);
}

function withBearer(request: HttpRequest<unknown>, token: string | null): HttpRequest<unknown> {
  return token ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : request;
}

/**
 * Adds the bearer token to the API requests. When the API answers 401, the refresh token is
 * used once to renew the session and retry the request; if that also fails the user is sent
 * back to the login page.
 */
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const session = inject(AuthSession);
  const router = inject(Router);
  const baseUrl = inject(API_BASE_URL);

  if (!request.url.startsWith(baseUrl) || isAuthEndpoint(request.url, baseUrl)) {
    return next(request);
  }

  const redirectToLogin = (error: HttpErrorResponse): Observable<never> => {
    session.logout();
    void router.navigate(['/login'], { queryParams: { returnUrl: router.url } });
    return throwError(() => error);
  };

  const retryWithRefreshedToken = (
    error: HttpErrorResponse,
    handler: HttpHandlerFn,
  ): Observable<HttpEvent<unknown>> =>
    session.refresh().pipe(
      switchMap((tokens) => handler(withBearer(request, tokens.token))),
      catchError(() => redirectToLogin(error)),
    );

  return next(withBearer(request, session.accessToken)).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse && error.status === 401 && session.isAuthenticated()) {
        return retryWithRefreshedToken(error, next);
      }

      return throwError(() => error);
    }),
  );
};
