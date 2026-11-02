import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { Login } from './login';

describe('Login', () => {
  let http: HttpTestingController;

  beforeEach(async () => {
    localStorage.clear();

    await TestBed.configureTestingModule({
      imports: [Login],
      providers: [
        provideRouter([
          { path: 'projects', children: [] },
          { path: 'login', component: Login },
        ]),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    }).compileComponents();

    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    localStorage.clear();
  });

  function fill(element: HTMLElement, id: string, value: string): void {
    const input = element.querySelector<HTMLInputElement>(`#${id}`)!;
    input.value = value;
    input.dispatchEvent(new Event('input'));
  }

  it('does not call the API with an invalid form', async () => {
    const fixture = TestBed.createComponent(Login);
    await fixture.whenStable();

    fixture.nativeElement.querySelector('form').dispatchEvent(new Event('submit'));
    await fixture.whenStable();

    http.expectNone('/api/Auth/Login');
    expect(fixture.nativeElement.querySelectorAll('.field__error').length).toBe(2);
  });

  it('signs in and navigates to the projects', async () => {
    const fixture = TestBed.createComponent(Login);
    const router = TestBed.inject(Router);
    await fixture.whenStable();

    fill(fixture.nativeElement, 'email', 'adm@thomas.com');
    fill(fixture.nativeElement, 'password', '123');
    fixture.nativeElement.querySelector('form').dispatchEvent(new Event('submit'));
    await fixture.whenStable();

    const request = http.expectOne('/api/Auth/Login');
    expect(request.request.body).toEqual({ email: 'adm@thomas.com', password: '123' });
    request.flush({ token: 'a.b.c', refreshToken: 'r', expiration: '' });

    await fixture.whenStable();
    await new Promise((resolve) => setTimeout(resolve));

    expect(router.url).toBe('/projects');
  });

  it('shows the API error', async () => {
    const fixture = TestBed.createComponent(Login);
    await fixture.whenStable();

    fill(fixture.nativeElement, 'email', 'adm@thomas.com');
    fill(fixture.nativeElement, 'password', 'wrong');
    fixture.nativeElement.querySelector('form').dispatchEvent(new Event('submit'));
    await fixture.whenStable();

    http
      .expectOne('/api/Auth/Login')
      .flush(
        { title: 'Authentication Error', errors: ['Invalid Credentials'] },
        { status: 401, statusText: 'Unauthorized' },
      );
    await fixture.whenStable();

    expect(fixture.nativeElement.querySelector('.alert--error')?.textContent).toContain(
      'Invalid Credentials',
    );
  });
});
