import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ProjectsApi } from './projects-api';

describe('ProjectsApi', () => {
  let api: ProjectsApi;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    api = TestBed.inject(ProjectsApi);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('lists the projects with pagination and search', () => {
    api.list({ page: 2, quantity: 10, query: ' billing ' }).subscribe();

    const request = http.expectOne((r) => r.url === '/api/Projects');
    expect(request.request.method).toBe('GET');
    expect(request.request.params.get('Page')).toBe('2');
    expect(request.request.params.get('Quantity')).toBe('10');
    expect(request.request.params.get('Query')).toBe('billing');
    request.flush({ items: [], count: 0 });
  });

  it('omits the query when blank', () => {
    api.list({ page: 1, quantity: 10, query: '  ' }).subscribe();

    const request = http.expectOne((r) => r.url === '/api/Projects');
    expect(request.request.params.has('Query')).toBe(false);
    request.flush({ items: [], count: 0 });
  });

  it('creates, reads, updates and deletes a project', () => {
    const payload = {
      name: 'Billing',
      newRelicId: '123',
      gitHubRepository: 'org/billing',
      gitHubKeys: { accessToken: 'ghp_x' },
      priorityBranch: 'main',
    };

    api.create(payload).subscribe();
    const create = http.expectOne('/api/Projects');
    expect(create.request.method).toBe('POST');
    expect(create.request.body).toEqual(payload);
    create.flush({ publicId: 'p1', ...payload });

    api.get('p1').subscribe();
    http.expectOne({ method: 'GET', url: '/api/Projects/p1' }).flush({});

    api.update('p1', { ...payload, gitHubKeys: null }).subscribe();
    const update = http.expectOne({ method: 'PUT', url: '/api/Projects/p1' });
    expect(update.request.body.gitHubKeys).toBeNull();
    update.flush(null, { status: 202, statusText: 'Accepted' });

    api.delete('p1').subscribe();
    http
      .expectOne({ method: 'DELETE', url: '/api/Projects/p1' })
      .flush(null, { status: 204, statusText: 'No Content' });
  });

  it('triggers the errors synchronization', () => {
    api.syncErrors('p1').subscribe();

    const request = http.expectOne({ method: 'POST', url: '/api/Projects/p1/Errors/Sync' });
    request.flush({ projectId: 'p1', eventsProcessed: 3, newErrors: 2, updatedErrors: 1 });
  });

  it('escapes the identifiers', () => {
    api.get('a/b').subscribe();
    http.expectOne('/api/Projects/a%2Fb').flush({});
  });
});
