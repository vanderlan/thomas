import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { extractApiErrorMessage } from '../../../core/api/api-error';
import { ProjectsApi } from '../../../core/api/projects-api';
import { AuthSession } from '../../../core/auth/auth-session';
import { DEFAULT_PAGE_SIZE, PagedList } from '../../../core/models/paged-list';
import { Project } from '../../../core/models/project';
import { Toast } from '../../../core/notifications/toast';
import { ConfirmDialog } from '../../../shared/components/confirm-dialog';
import { Pagination } from '../../../shared/components/pagination';
import { TimeAgo } from '../../../shared/pipes/time-ago';

@Component({
  selector: 'app-project-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, DatePipe, TimeAgo, Pagination, ConfirmDialog],
  templateUrl: './project-list.html',
})
export class ProjectList {
  private readonly projectsApi = inject(ProjectsApi);
  private readonly toast = inject(Toast);
  protected readonly session = inject(AuthSession);

  protected readonly pageSize = DEFAULT_PAGE_SIZE;
  protected readonly page = signal(1);
  protected readonly query = signal('');
  protected readonly loading = signal(false);
  protected readonly result = signal<PagedList<Project>>({ items: [], count: 0 });
  protected readonly syncing = signal<string | null>(null);
  protected readonly pendingDelete = signal<Project | null>(null);
  protected readonly deleteMessage = computed(
    () =>
      `Delete "${this.pendingDelete()?.name ?? ''}" and its whole knowledge base? This cannot be undone.`,
  );

  constructor() {
    this.load();
  }

  protected search(value: string): void {
    this.query.set(value);
    this.page.set(1);
    this.load();
  }

  protected goToPage(page: number): void {
    this.page.set(page);
    this.load();
  }

  protected load(): void {
    this.loading.set(true);

    this.projectsApi
      .list({ page: this.page(), quantity: this.pageSize, query: this.query() })
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

  protected sync(project: Project): void {
    if (this.syncing()) {
      return;
    }

    this.syncing.set(project.publicId);

    this.projectsApi.syncErrors(project.publicId).subscribe({
      next: (summary) => {
        this.syncing.set(null);
        this.toast.success(
          `${project.name}: ${summary.eventsProcessed} occurrence(s) processed, ` +
            `${summary.newErrors} new and ${summary.updatedErrors} updated error(s).`,
        );
        this.load();
      },
      error: (error: unknown) => {
        this.syncing.set(null);
        this.toast.error(extractApiErrorMessage(error));
      },
    });
  }

  protected askDelete(project: Project, dialog: ConfirmDialog): void {
    this.pendingDelete.set(project);
    dialog.open();
  }

  protected confirmDelete(): void {
    const project = this.pendingDelete();

    if (!project) {
      return;
    }

    this.projectsApi.delete(project.publicId).subscribe({
      next: () => {
        this.pendingDelete.set(null);
        this.toast.success(`Project "${project.name}" and its knowledge base were removed.`);

        if (this.result().items.length === 1 && this.page() > 1) {
          this.page.update((page) => page - 1);
        }

        this.load();
      },
      error: (error: unknown) => this.toast.error(extractApiErrorMessage(error)),
    });
  }
}
