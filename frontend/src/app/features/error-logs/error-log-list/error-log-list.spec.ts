import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { ErrorLog, ErrorLogStatus } from '../../../core/models/error-log';
import { ErrorLogList } from './error-log-list';

function errorLog(errorClass: string, status = ErrorLogStatus.New): ErrorLog {
  return {
    publicId: `id-${errorClass}`,
    projectId: 'p1',
    fingerprint: 'abc',
    errorClass,
    message: 'Something failed',
    status,
    occurrences: 3,
    firstSeenAt: '2025-01-01T00:00:00Z',
    lastSeenAt: '2025-01-01T01:00:00Z',
    lastUpdated: '2025-01-01T01:00:00Z',
    createdAt: '2025-01-01T00:00:00Z',
  };
}

describe('ErrorLogList', () => {
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ErrorLogList],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('as a page, loads the project filter and the first page of errors', async () => {
    const fixture = TestBed.createComponent(ErrorLogList);
    await fixture.whenStable();

    http
      .expectOne((r) => r.url === '/api/Projects')
      .flush({ items: [{ publicId: 'p1', name: 'Billing' }], count: 1 });

    const list = http.expectOne((r) => r.url === '/api/ErrorLogs');
    expect(list.request.params.has('ProjectId')).toBe(false);
    list.flush({
      items: [errorLog('System.NullReferenceException', ErrorLogStatus.Recurring)],
      count: 1,
    });
    await fixture.whenStable();

    const element: HTMLElement = fixture.nativeElement;
    expect(element.querySelector('h1')?.textContent).toContain('Error logs');
    expect(element.querySelectorAll('tbody tr').length).toBe(1);
    expect(element.querySelector('tbody tr')?.textContent).toContain(
      'System.NullReferenceException',
    );
    expect(element.querySelector('tbody tr')?.textContent).toContain('Billing');
    expect(element.querySelector('.badge')?.textContent).toContain('Recurring');
  });

  it('filters by status from the first page', async () => {
    const fixture = TestBed.createComponent(ErrorLogList);
    await fixture.whenStable();
    http.expectOne((r) => r.url === '/api/Projects').flush({ items: [], count: 0 });
    http.expectOne((r) => r.url === '/api/ErrorLogs').flush({ items: [], count: 0 });
    await fixture.whenStable();

    const select: HTMLSelectElement = fixture.nativeElement.querySelector(
      'select[aria-label="Filter by status"]',
    );
    select.value = String(ErrorLogStatus.Resolved);
    select.dispatchEvent(new Event('change'));
    await fixture.whenStable();

    const request = http.expectOne((r) => r.url === '/api/ErrorLogs');
    expect(request.request.params.get('Status')).toBe('3');
    request.flush({ items: [], count: 0 });
  });

  it('embedded on a project, fixes the project and hides the project filter', async () => {
    const fixture = TestBed.createComponent(ErrorLogList);
    fixture.componentRef.setInput('projectId', 'p1');
    fixture.componentRef.setInput('embedded', true);
    await fixture.whenStable();

    http.expectNone((r) => r.url === '/api/Projects');

    const request = http.expectOne((r) => r.url === '/api/ErrorLogs');
    expect(request.request.params.get('ProjectId')).toBe('p1');
    request.flush({ items: [], count: 0 });
    await fixture.whenStable();

    expect(fixture.nativeElement.querySelector('h1')).toBeNull();
    expect(
      fixture.nativeElement.querySelector('select[aria-label="Filter by project"]'),
    ).toBeNull();
    expect(fixture.nativeElement.querySelector('.empty__title')?.textContent).toContain(
      'No errors found',
    );
  });
});
