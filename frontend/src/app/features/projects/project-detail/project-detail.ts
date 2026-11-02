import { DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  input,
  OnInit,
  signal,
  viewChild,
} from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { extractApiErrorMessage, extractApiErrors } from '../../../core/api/api-error';
import { ProjectsApi } from '../../../core/api/projects-api';
import { AuthSession } from '../../../core/auth/auth-session';
import { Project, SyncProjectErrorsResponse } from '../../../core/models/project';
import { Toast } from '../../../core/notifications/toast';
import { ConfirmDialog } from '../../../shared/components/confirm-dialog';
import { TimeAgo } from '../../../shared/pipes/time-ago';
import { ErrorLogList } from '../../error-logs/error-log-list/error-log-list';

@Component({
  selector: 'app-project-detail',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, DatePipe, TimeAgo, ConfirmDialog, ErrorLogList],
  templateUrl: './project-detail.html',
})
export class ProjectDetail implements OnInit {
  private readonly projectsApi = inject(ProjectsApi);
  private readonly router = inject(Router);
  private readonly toast = inject(Toast);
  protected readonly session = inject(AuthSession);

  /** Bound from the route (":id"). */
  readonly id = input.required<string>();

  private readonly errorLogList = viewChild(ErrorLogList);

  protected readonly project = signal<Project | null>(null);
  protected readonly errors = signal<string[]>([]);
  protected readonly syncing = signal(false);
  protected readonly lastSync = signal<SyncProjectErrorsResponse | null>(null);
  protected readonly deleteMessage = computed(
    () =>
      `Delete "${this.project()?.name ?? ''}" and its whole knowledge base? This cannot be undone.`,
  );

  ngOnInit(): void {
    this.load();
  }

  protected load(): void {
    this.projectsApi.get(this.id()).subscribe({
      next: (project) => this.project.set(project),
      error: (error: unknown) => this.errors.set(extractApiErrors(error)),
    });
  }

  protected sync(): void {
    if (this.syncing()) {
      return;
    }

    this.syncing.set(true);

    this.projectsApi.syncErrors(this.id()).subscribe({
      next: (summary) => {
        this.syncing.set(false);
        this.lastSync.set(summary);
        this.toast.success(
          `${summary.eventsProcessed} occurrence(s) processed: ${summary.newErrors} new and ${summary.updatedErrors} updated error(s).`,
        );
        this.load();
        this.errorLogList()?.reload();
      },
      error: (error: unknown) => {
        this.syncing.set(false);
        this.toast.error(extractApiErrorMessage(error));
      },
    });
  }

  protected confirmDelete(): void {
    const project = this.project();

    if (!project) {
      return;
    }

    this.projectsApi.delete(project.publicId).subscribe({
      next: () => {
        this.toast.success(`Project "${project.name}" and its knowledge base were removed.`);
        void this.router.navigate(['/projects']);
      },
      error: (error: unknown) => this.toast.error(extractApiErrorMessage(error)),
    });
  }
}
