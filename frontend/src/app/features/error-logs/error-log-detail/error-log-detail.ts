import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, input, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { extractApiErrorMessage, extractApiErrors } from '../../../core/api/api-error';
import { ErrorLogsApi } from '../../../core/api/error-logs-api';
import { ProjectsApi } from '../../../core/api/projects-api';
import { AuthSession } from '../../../core/auth/auth-session';
import {
  ErrorLogDetail as ErrorLogDetailModel,
  ErrorLogStatus,
} from '../../../core/models/error-log';
import { Project } from '../../../core/models/project';
import { Toast } from '../../../core/notifications/toast';
import { ConfirmDialog } from '../../../shared/components/confirm-dialog';
import { StatusBadge } from '../../../shared/components/status-badge';
import { TimeAgo } from '../../../shared/pipes/time-ago';

@Component({
  selector: 'app-error-log-detail',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, DatePipe, TimeAgo, StatusBadge, ConfirmDialog],
  templateUrl: './error-log-detail.html',
})
export class ErrorLogDetail implements OnInit {
  private readonly errorLogsApi = inject(ErrorLogsApi);
  private readonly projectsApi = inject(ProjectsApi);
  private readonly toast = inject(Toast);
  protected readonly session = inject(AuthSession);

  /** Bound from the route (":id"). */
  readonly id = input.required<string>();

  protected readonly errorLog = signal<ErrorLogDetailModel | null>(null);
  protected readonly project = signal<Project | null>(null);
  protected readonly errors = signal<string[]>([]);
  protected readonly resolving = signal(false);
  protected readonly resolvedStatus = ErrorLogStatus.Resolved;

  ngOnInit(): void {
    this.load();
  }

  protected load(): void {
    this.errorLogsApi.get(this.id()).subscribe({
      next: (errorLog) => {
        this.errorLog.set(errorLog);

        if (this.project()?.publicId !== errorLog.projectId) {
          this.projectsApi.get(errorLog.projectId).subscribe({
            next: (project) => this.project.set(project),
            error: () => this.project.set(null),
          });
        }
      },
      error: (error: unknown) => this.errors.set(extractApiErrors(error)),
    });
  }

  protected resolve(): void {
    if (this.resolving()) {
      return;
    }

    this.resolving.set(true);

    this.errorLogsApi.resolve(this.id()).subscribe({
      next: () => {
        this.resolving.set(false);
        this.toast.success(
          'The error was marked as resolved. It will be reopened if it happens again.',
        );
        this.load();
      },
      error: (error: unknown) => {
        this.resolving.set(false);
        this.toast.error(extractApiErrorMessage(error));
      },
    });
  }
}
