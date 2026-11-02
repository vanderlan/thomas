import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { AuthTokens } from '../models/auth';
import { authInterceptor } from './auth-interceptor';
import { AuthSession } from './auth-session';

const TOKENS: AuthTokens = { token: 'a.b.c', refreshToken: 'refresh-1', expiration: '' };

describe('authInterceptor', () => {
  let http: HttpClient;
  let controller: HttpTestingController;
  let session: AuthSession;

  beforeEach(() => {
    localStorage.clear();

    TestBed.configureTestingModule({
      providers: [
        provideRouter([{ path: 'login', children: [] }]),
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
      ],
    });

    http = TestBed.inject(HttpClient);
    controller = TestBed.inject(HttpTestingController);
    session = TestBed.inject(AuthSession);
  });

  afterEach(() => {
    controller.verify();
    localStorage.clear();
  });

  function authenticate(): void {
    session.login({ email: 'a', password: 'b' }).subscribe();
    controller.expectOne('/api/Auth/Login').flush(TOKENS);
  }

  it('adds the bearer token to the API requests', () => {
    authenticate();

    http.get('/api/Projects').subscribe();

    const request = controller.expectOne('/api/Projects');
    expect(request.request.headers.get('Authorization')).toBe('Bearer a.b.c');
    request.flush({ items: [], count: 0 });
  });

  it('does not add the token to anonymous requests nor to the auth endpoints', () => {
    http.get('/api/Projects').subscribe();
    expect(controller.expectOne('/api/Projects').request.headers.has('Authorization')).toBe(false);

    authenticate();

    http.post('/api/Auth/RefreshToken', {}).subscribe();
    expect(
      controller.expectOne('/api/Auth/RefreshToken').request.headers.has('Authorization'),
    ).toBe(false);

    http.get('https://other.example/resource').subscribe();
    expect(
      controller.expectOne('https://other.example/resource').request.headers.has('Authorization'),
    ).toBe(false);
  });

  it('refreshes the session and retries the request on 401', () => {
    authenticate();

    let body: unknown;
    http.get('/api/Projects').subscribe((response) => (body = response));

    controller.expectOne('/api/Projects').flush(null, { status: 401, statusText: 'Unauthorized' });

    const refresh = controller.expectOne('/api/Auth/RefreshToken');
    expect(refresh.request.body).toEqual({ token: 'a.b.c', refreshToken: 'refresh-1' });
    refresh.flush({ ...TOKENS, token: 'd.e.f', refreshToken: 'refresh-2' });

    const retried = controller.expectOne('/api/Projects');
    expect(retried.request.headers.get('Authorization')).toBe('Bearer d.e.f');
    retried.flush({ items: [], count: 0 });

    expect(body).toEqual({ items: [], count: 0 });
    expect(session.tokens()?.refreshToken).toBe('refresh-2');
  });

  it('logs out and redirects to the login page when the refresh fails', async () => {
    authenticate();
    const router = TestBed.inject(Router);

    let failed = false;
    http.get('/api/Projects').subscribe({ error: () => (failed = true) });

    controller.expectOne('/api/Projects').flush(null, { status: 401, statusText: 'Unauthorized' });
    controller
      .expectOne('/api/Auth/RefreshToken')
      .flush(null, { status: 401, statusText: 'Unauthorized' });

    // wait for the navigation triggered by the interceptor
    await new Promise((resolve) => setTimeout(resolve));

    expect(failed).toBe(true);
    expect(session.isAuthenticated()).toBe(false);
    expect(router.url.startsWith('/login')).toBe(true);
  });

  it('propagates the other errors untouched', () => {
    authenticate();

    let status = 0;
    http.get('/api/Projects').subscribe({ error: (error) => (status = error.status) });

    controller.expectOne('/api/Projects').flush(null, { status: 404, statusText: 'Not Found' });

    expect(status).toBe(404);
    expect(session.isAuthenticated()).toBe(true);
  });
});
