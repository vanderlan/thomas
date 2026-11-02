import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { ErrorLogStatus } from '../../../core/models/error-log';
import { ErrorLogDetail } from './error-log-detail';

const ADMIN_JWT = `x.${btoa(JSON.stringify({ role: 'admin' })).replace(/=+$/, '')}.y`;

const ERROR_LOG = {
  publicId: 'e1',
  projectId: 'p1',
  fingerprint: 'fp',
  errorClass: 'System.TimeoutException',
  message: 'Timed out',
  stackTrace: '   at Thomas.Api.Orders.Get()',
  status: ErrorLogStatus.New,
  occurrences: 7,
  firstSeenAt: '2025-01-01T00:00:00Z',
  lastSeenAt: '2025-01-01T01:00:00Z',
  lastUpdated: '2025-01-01T01:00:00Z',
  createdAt: '2025-01-01T00:00:00Z',
};

describe('ErrorLogDetail', () => {
  let http: HttpTestingController;

  beforeEach(async () => {
    localStorage.setItem(
      'thomas.auth',
      JSON.stringify({ token: ADMIN_JWT, refreshToken: 'r', expiration: '' }),
    );

    await TestBed.configureTestingModule({
      imports: [ErrorLogDetail],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    localStorage.clear();
  });

  function render() {
    const fixture = TestBed.createComponent(ErrorLogDetail);
    fixture.componentRef.setInput('id', 'e1');
    return fixture;
  }

  it('shows the error with its stack trace and project', async () => {
    const fixture = render();
    await fixture.whenStable();

    http.expectOne('/api/ErrorLogs/e1').flush(ERROR_LOG);
    http.expectOne('/api/Projects/p1').flush({ publicId: 'p1', name: 'Billing' });
    await fixture.whenStable();

    const element: HTMLElement = fixture.nativeElement;
    expect(element.querySelector('h1')?.textContent).toContain('System.TimeoutException');
    expect(element.querySelector('.stack-trace')?.textContent).toContain('Thomas.Api.Orders.Get');
    expect(element.querySelector('.breadcrumb')?.textContent).toContain('Billing');
    expect(element.querySelector('.stat')?.textContent).toContain('7');
  });

  it('resolves the error and reloads it', async () => {
    const fixture = render();
    await fixture.whenStable();
    http.expectOne('/api/ErrorLogs/e1').flush(ERROR_LOG);
    http.expectOne('/api/Projects/p1').flush({ publicId: 'p1', name: 'Billing' });
    await fixture.whenStable();

    const resolveButton = Array.from(
      fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>,
    ).find((button) => button.textContent?.includes('Mark as resolved'))!;
    resolveButton.click();
    await fixture.whenStable();

    const confirm = Array.from(
      fixture.nativeElement.querySelectorAll('.dialog button') as NodeListOf<HTMLButtonElement>,
    ).find((button) => button.textContent?.trim() === 'Resolve')!;
    confirm.click();
    await fixture.whenStable();

    http
      .expectOne({ method: 'PATCH', url: '/api/ErrorLogs/e1/Resolve' })
      .flush(null, { status: 202, statusText: 'Accepted' });
    await fixture.whenStable();

    http
      .expectOne('/api/ErrorLogs/e1')
      .flush({ ...ERROR_LOG, status: ErrorLogStatus.Resolved, resolvedAt: '2025-01-02T00:00:00Z' });
    await fixture.whenStable();

    expect(fixture.nativeElement.querySelector('.badge')?.textContent).toContain('Resolved');
    expect(fixture.nativeElement.textContent).not.toContain('Mark as resolved');
  });

  it('shows the API error when the error log does not exist', async () => {
    const fixture = render();
    await fixture.whenStable();

    http.expectOne('/api/ErrorLogs/e1').flush(null, { status: 404, statusText: 'Not Found' });
    await fixture.whenStable();

    expect(fixture.nativeElement.querySelector('.alert--error')?.textContent).toContain(
      'not found',
    );
  });
});
