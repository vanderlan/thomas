import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, input, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { extractApiErrorMessage } from '../../../core/api/api-error';
import { ErrorLogsApi } from '../../../core/api/error-logs-api';
import { ProjectsApi } from '../../../core/api/projects-api';
import { ERROR_LOG_STATUSES, ErrorLog, ErrorLogStatus } from '../../../core/models/error-log';
import { DEFAULT_PAGE_SIZE, PagedList } from '../../../core/models/paged-list';
import { Project } from '../../../core/models/project';
import { Toast } from '../../../core/notifications/toast';
import { Pagination } from '../../../shared/components/pagination';
import { StatusBadge } from '../../../shared/components/status-badge';
import { TimeAgo } from '../../../shared/pipes/time-ago';

/**
 * The knowledge base listing. Used as a page (/errors, with the project filter) and embedded
 * on the project details (fixed project, no project filter).
 */
@Component({
  selector: 'app-error-log-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, DatePipe, TimeAgo, Pagination, StatusBadge],
  templateUrl: './error-log-list.html',
})
export class ErrorLogList implements OnInit {
  private readonly errorLogsApi = inject(ErrorLogsApi);
  private readonly projectsApi = inject(ProjectsApi);
  private readonly toast = inject(Toast);

  /** When informed, the listing is fixed to this project. */
  readonly projectId = input<string | null>(null);
  readonly embedded = input(false);

  protected readonly statuses = ERROR_LOG_STATUSES;
  protected readonly pageSize = DEFAULT_PAGE_SIZE;
  protected readonly page = signal(1);
  protected readonly query = signal('');
  protected readonly status = signal<ErrorLogStatus | null>(null);
  protected readonly selectedProjectId = signal<string | null>(null);
  protected readonly projects = signal<Project[]>([]);
  protected readonly loading = signal(false);
  protected readonly result = signal<PagedList<ErrorLog>>({ items: [], count: 0 });

  ngOnInit(): void {
    this.selectedProjectId.set(this.projectId());

    if (!this.embedded()) {
      this.loadProjects();
    }

    this.reload();
  }

  reload(): void {
    this.loading.set(true);

    this.errorLogsApi
      .list({
        page: this.page(),
        quantity: this.pageSize,
        query: this.query(),
        status: this.status(),
        projectId: this.selectedProjectId(),
      })
      .subscribe({
        next: (result) => {
          this.result.set(result);
          this.loading.set(false);
        },
        error: (error: unknown) => {
          this.loading.set(false);
          this.toast.error(extractApiErrorMessage(error));
        },
      });
  }

  protected search(value: string): void {
    this.query.set(value);
    this.resetAndReload();
  }

  protected changeStatus(value: string): void {
    this.status.set(value ? (Number(value) as ErrorLogStatus) : null);
    this.resetAndReload();
  }

  protected changeProject(value: string): void {
    this.selectedProjectId.set(value || null);
    this.resetAndReload();
  }

  protected goToPage(page: number): void {
    this.page.set(page);
    this.reload();
  }

  protected projectName(projectId: string): string {
    return this.projects().find((project) => project.publicId === projectId)?.name ?? '';
  }

  private resetAndReload(): void {
    this.page.set(1);
    this.reload();
  }

  private loadProjects(): void {
    // The project filter lists the first 100 projects of the portfolio.
    this.projectsApi.list({ page: 1, quantity: 100 }).subscribe({
      next: (result) => this.projects.set(result.items),
      error: (error: unknown) => this.toast.error(extractApiErrorMessage(error)),
    });
  }
}
