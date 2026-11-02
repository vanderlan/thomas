import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  input,
  OnInit,
  signal,
} from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { extractApiErrors } from '../../../core/api/api-error';
import { ProjectsApi } from '../../../core/api/projects-api';
import { Project, ProjectCreateRequest, ProjectUpdateRequest } from '../../../core/models/project';
import { Toast } from '../../../core/notifications/toast';

/** Mirrors the API rule: the repository must be informed as "owner/repository". */
export const GITHUB_REPOSITORY_PATTERN = /^[A-Za-z0-9_.-]+\/[A-Za-z0-9_.-]+$/;

@Component({
  selector: 'app-project-form',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './project-form.html',
})
export class ProjectForm implements OnInit {
  private readonly formBuilder = inject(FormBuilder);
  private readonly projectsApi = inject(ProjectsApi);
  private readonly router = inject(Router);
  private readonly toast = inject(Toast);

  /** Bound from the route (":id"); undefined on creation. */
  readonly id = input<string>();

  protected readonly isEdit = computed(() => !!this.id());
  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  protected readonly errors = signal<string[]>([]);
  protected readonly project = signal<Project | null>(null);

  protected readonly form = this.formBuilder.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    description: [''],
    newRelicId: ['', Validators.required],
    gitHubRepository: ['', [Validators.required, Validators.pattern(GITHUB_REPOSITORY_PATTERN)]],
    priorityBranch: ['main', Validators.required],
    accessToken: ['', Validators.required],
    privateKey: [''],
  });

  /** The route input (":id") is bound before ngOnInit. */
  ngOnInit(): void {
    const id = this.id();

    if (!id) {
      return;
    }

    // On edit the keys are optional: when left blank the current ones are kept.
    this.form.controls.accessToken.clearValidators();
    this.form.controls.accessToken.updateValueAndValidity();

    this.loading.set(true);

    this.projectsApi.get(id).subscribe({
      next: (project) => {
        this.project.set(project);
        this.form.patchValue({
          name: project.name,
          description: project.description ?? '',
          newRelicId: project.newRelicId,
          gitHubRepository: project.gitHubRepository,
          priorityBranch: project.priorityBranch,
        });
        this.loading.set(false);
      },
      error: (error: unknown) => {
        this.loading.set(false);
        this.errors.set(extractApiErrors(error));
      },
    });
  }

  protected invalid(control: keyof typeof this.form.controls): boolean {
    const field = this.form.controls[control];
    return field.touched && field.invalid;
  }

  protected submit(): void {
    if (this.form.invalid || this.saving()) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.errors.set([]);

    const value = this.form.getRawValue();
    const keys = value.accessToken.trim()
      ? { accessToken: value.accessToken.trim(), privateKey: value.privateKey.trim() || null }
      : null;

    const id = this.id();

    if (id) {
      const request: ProjectUpdateRequest = {
        name: value.name,
        description: value.description || null,
        newRelicId: value.newRelicId,
        gitHubRepository: value.gitHubRepository,
        priorityBranch: value.priorityBranch,
        gitHubKeys: keys,
      };

      this.projectsApi.update(id, request).subscribe({
        next: () => {
          this.toast.success(`Project "${value.name}" updated.`);
          void this.router.navigate(['/projects', id]);
        },
        error: (error: unknown) => this.fail(error),
      });

      return;
    }

    const request: ProjectCreateRequest = {
      name: value.name,
      description: value.description || null,
      newRelicId: value.newRelicId,
      gitHubRepository: value.gitHubRepository,
      priorityBranch: value.priorityBranch,
      gitHubKeys: keys ?? { accessToken: '' },
    };

    this.projectsApi.create(request).subscribe({
      next: (project) => {
        this.toast.success(`Project "${project.name}" created.`);
        void this.router.navigate(['/projects', project.publicId]);
      },
      error: (error: unknown) => this.fail(error),
    });
  }

  private fail(error: unknown): void {
    this.saving.set(false);
    this.errors.set(extractApiErrors(error));
  }
}
