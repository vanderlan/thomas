import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { AuthSession } from '../../../core/auth/auth-session';
import { Project } from '../../../core/models/project';
import { ProjectList } from './project-list';

const ADMIN_JWT = `x.${btoa(JSON.stringify({ role: 'admin', given_name: 'Admin' })).replace(/=+$/, '')}.y`;

function project(name: string): Project {
  return {
    publicId: `id-${name}`,
    name,
    newRelicId: '123',
    gitHubRepository: `org/${name}`,
    priorityBranch: 'main',
    lastErrorSyncAt: null,
    lastUpdated: '2025-01-01T00:00:00Z',
    createdAt: '2025-01-01T00:00:00Z',
  };
}

describe('ProjectList', () => {
  let http: HttpTestingController;

  beforeEach(async () => {
    localStorage.setItem(
      'thomas.auth',
      JSON.stringify({ token: ADMIN_JWT, refreshToken: 'r', expiration: '' }),
    );

    await TestBed.configureTestingModule({
      imports: [ProjectList],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    http = TestBed.inject(HttpTestingController);
    expect(TestBed.inject(AuthSession).isAdmin()).toBe(true);
  });

  afterEach(() => {
    http.verify();
    localStorage.clear();
  });

  it('lists the projects of the first page', async () => {
    const fixture = TestBed.createComponent(ProjectList);
    await fixture.whenStable();

    const request = http.expectOne((r) => r.url === '/api/Projects');
    expect(request.request.params.get('Page')).toBe('1');
    request.flush({ items: [project('billing'), project('orders')], count: 2 });
    await fixture.whenStable();

    const rows = fixture.nativeElement.querySelectorAll('tbody tr');
    expect(rows.length).toBe(2);
    expect(rows[0].textContent).toContain('billing');
    expect(rows[0].textContent).toContain('org/billing');
    expect(fixture.nativeElement.querySelector('a[href="/projects/new"]')).not.toBeNull();
  });

  it('shows the empty state', async () => {
    const fixture = TestBed.createComponent(ProjectList);
    await fixture.whenStable();

    http.expectOne((r) => r.url === '/api/Projects').flush({ items: [], count: 0 });
    await fixture.whenStable();

    expect(fixture.nativeElement.querySelector('.empty__title')?.textContent).toContain(
      'No projects found',
    );
  });

  it('searches by name from the first page', async () => {
    const fixture = TestBed.createComponent(ProjectList);
    await fixture.whenStable();
    http.expectOne((r) => r.url === '/api/Projects').flush({ items: [], count: 0 });
    await fixture.whenStable();

    const input: HTMLInputElement = fixture.nativeElement.querySelector('input[type="search"]');
    input.value = 'bill';
    input.dispatchEvent(new Event('change'));
    await fixture.whenStable();

    const request = http.expectOne((r) => r.url === '/api/Projects');
    expect(request.request.params.get('Query')).toBe('bill');
    expect(request.request.params.get('Page')).toBe('1');
    request.flush({ items: [project('billing')], count: 1 });
  });

  it('synchronizes the errors of a project and reloads the list', async () => {
    const fixture = TestBed.createComponent(ProjectList);
    await fixture.whenStable();
    http
      .expectOne((r) => r.url === '/api/Projects')
      .flush({ items: [project('billing')], count: 1 });
    await fixture.whenStable();

    const syncButton = Array.from(
      fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>,
    ).find((button) => button.textContent?.includes('Sync errors'))!;
    syncButton.click();
    await fixture.whenStable();

    http
      .expectOne({ method: 'POST', url: '/api/Projects/id-billing/Errors/Sync' })
      .flush({ projectId: 'id-billing', eventsProcessed: 5, newErrors: 2, updatedErrors: 1 });
    await fixture.whenStable();

    http
      .expectOne((r) => r.url === '/api/Projects')
      .flush({ items: [project('billing')], count: 1 });
  });
});
