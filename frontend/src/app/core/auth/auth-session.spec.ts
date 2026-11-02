import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AuthTokens } from '../models/auth';
import { AuthSession } from './auth-session';

function createJwt(payload: Record<string, unknown>): string {
  const encode = (value: unknown) => btoa(JSON.stringify(value)).replace(/=+$/, '');
  return `${encode({ alg: 'HS512' })}.${encode(payload)}.sig`;
}

const TOKENS: AuthTokens = {
  token: createJwt({ sid: '1', given_name: 'Admin', email: 'adm@thomas.com', role: 'admin' }),
  refreshToken: 'refresh-1',
  expiration: '2030-01-01T00:00:00Z',
};

describe('AuthSession', () => {
  let session: AuthSession;
  let http: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();

    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });

    session = TestBed.inject(AuthSession);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    localStorage.clear();
  });

  it('starts anonymous', () => {
    expect(session.isAuthenticated()).toBe(false);
    expect(session.user()).toBeNull();
    expect(session.isAdmin()).toBe(false);
  });

  it('stores the tokens and exposes the user after the login', () => {
    session.login({ email: 'adm@thomas.com', password: '123' }).subscribe();

    const request = http.expectOne('/api/Auth/Login');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ email: 'adm@thomas.com', password: '123' });
    request.flush(TOKENS);

    expect(session.isAuthenticated()).toBe(true);
    expect(session.isAdmin()).toBe(true);
    expect(session.user()?.email).toBe('adm@thomas.com');
    expect(session.accessToken).toBe(TOKENS.token);
    expect(JSON.parse(localStorage.getItem('thomas.auth') ?? 'null')).toEqual(TOKENS);
  });

  it('restores the session from the storage', () => {
    localStorage.setItem('thomas.auth', JSON.stringify(TOKENS));

    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });

    const restored = TestBed.inject(AuthSession);
    http = TestBed.inject(HttpTestingController);

    expect(restored.isAuthenticated()).toBe(true);
    expect(restored.user()?.name).toBe('Admin');
  });

  it('rotates the refresh token', () => {
    session.login({ email: 'adm@thomas.com', password: '123' }).subscribe();
    http.expectOne('/api/Auth/Login').flush(TOKENS);

    const rotated: AuthTokens = { ...TOKENS, refreshToken: 'refresh-2' };

    session.refresh().subscribe();

    const request = http.expectOne('/api/Auth/RefreshToken');
    expect(request.request.body).toEqual({ token: TOKENS.token, refreshToken: 'refresh-1' });
    request.flush(rotated);

    expect(session.tokens()?.refreshToken).toBe('refresh-2');
  });

  it('cannot refresh without a session', () => {
    expect(() => session.refresh()).toThrowError('There is no session to refresh.');
  });

  it('clears everything on logout', () => {
    session.login({ email: 'adm@thomas.com', password: '123' }).subscribe();
    http.expectOne('/api/Auth/Login').flush(TOKENS);

    session.logout();

    expect(session.isAuthenticated()).toBe(false);
    expect(localStorage.getItem('thomas.auth')).toBeNull();
  });
});
