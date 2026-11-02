import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ErrorLogStatus } from '../models/error-log';
import { ErrorLogsApi } from './error-logs-api';

describe('ErrorLogsApi', () => {
  let api: ErrorLogsApi;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    api = TestBed.inject(ErrorLogsApi);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('lists the error logs with all the filters', () => {
    api
      .list({
        page: 3,
        quantity: 20,
        projectId: 'p1',
        status: ErrorLogStatus.Recurring,
        query: 'null',
      })
      .subscribe();

    const request = http.expectOne((r) => r.url === '/api/ErrorLogs');
    expect(request.request.params.get('Page')).toBe('3');
    expect(request.request.params.get('Quantity')).toBe('20');
    expect(request.request.params.get('ProjectId')).toBe('p1');
    expect(request.request.params.get('Status')).toBe('2');
    expect(request.request.params.get('Query')).toBe('null');
    request.flush({ items: [], count: 0 });
  });

  it('omits the empty filters', () => {
    api.list({ page: 1, quantity: 10, projectId: null, status: null, query: '' }).subscribe();

    const request = http.expectOne((r) => r.url === '/api/ErrorLogs');
    expect(request.request.params.keys()).toEqual(['Page', 'Quantity']);
    request.flush({ items: [], count: 0 });
  });

  it('reads the details and resolves an error', () => {
    api.get('e1').subscribe();
    http.expectOne({ method: 'GET', url: '/api/ErrorLogs/e1' }).flush({ publicId: 'e1' });

    api.resolve('e1').subscribe();
    http
      .expectOne({ method: 'PATCH', url: '/api/ErrorLogs/e1/Resolve' })
      .flush(null, { status: 202, statusText: 'Accepted' });
  });
});
