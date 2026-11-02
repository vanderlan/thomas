import { computed, inject, Injectable, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { AuthApi } from '../api/auth-api';
import { AuthTokens, AuthUser, LoginRequest, ROLE_ADMIN } from '../models/auth';
import { isJwtExpired, userFromJwt } from './jwt';

const STORAGE_KEY = 'thomas.auth';

/**
 * Holds the authenticated session (tokens + user claims) as signals, persisted on localStorage.
 */
@Injectable({ providedIn: 'root' })
export class AuthSession {
  private readonly authApi = inject(AuthApi);

  private readonly tokensSignal = signal<AuthTokens | null>(AuthSession.restore());

  readonly tokens = this.tokensSignal.asReadonly();
  readonly user = computed<AuthUser | null>(() => {
    const tokens = this.tokensSignal();
    return tokens ? userFromJwt(tokens.token) : null;
  });
  readonly isAuthenticated = computed(() => this.tokensSignal() !== null);
  readonly isAdmin = computed(() => this.user()?.role === ROLE_ADMIN);

  get accessToken(): string | null {
    return this.tokensSignal()?.token ?? null;
  }

  login(request: LoginRequest): Observable<AuthTokens> {
    return this.authApi.login(request).pipe(tap((tokens) => this.store(tokens)));
  }

  /** Rotates the refresh token. The API revokes the old one, so a failure means the session is lost. */
  refresh(): Observable<AuthTokens> {
    const tokens = this.tokensSignal();

    if (!tokens) {
      throw new Error('There is no session to refresh.');
    }

    return this.authApi
      .refresh({ token: tokens.token, refreshToken: tokens.refreshToken })
      .pipe(tap((refreshed) => this.store(refreshed)));
  }

  logout(): void {
    this.tokensSignal.set(null);
    localStorage.removeItem(STORAGE_KEY);
  }

  /** True when the access token is present but already expired (a refresh is needed). */
  isAccessTokenExpired(): boolean {
    const token = this.accessToken;
    return token !== null && isJwtExpired(token);
  }

  private store(tokens: AuthTokens): void {
    this.tokensSignal.set(tokens);
    localStorage.setItem(STORAGE_KEY, JSON.stringify(tokens));
  }

  private static restore(): AuthTokens | null {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      const tokens = raw ? (JSON.parse(raw) as AuthTokens) : null;

      return tokens?.token && tokens.refreshToken ? tokens : null;
    } catch {
      return null;
    }
  }
}
