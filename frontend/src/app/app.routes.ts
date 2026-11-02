import { Routes } from '@angular/router';
import { anonymousGuard, authGuard } from './core/auth/auth-guard';

export const routes: Routes = [
  {
    path: 'login',
    canActivate: [anonymousGuard],
    loadComponent: () => import('./features/auth/login/login').then((m) => m.Login),
    title: 'Sign in · Thomas',
  },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./features/shell/shell').then((m) => m.Shell),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'projects' },
      {
        path: 'projects',
        title: 'Projects · Thomas',
        loadComponent: () =>
          import('./features/projects/project-list/project-list').then((m) => m.ProjectList),
      },
      {
        path: 'projects/new',
        title: 'New project · Thomas',
        loadComponent: () =>
          import('./features/projects/project-form/project-form').then((m) => m.ProjectForm),
      },
      {
        path: 'projects/:id',
        title: 'Project · Thomas',
        loadComponent: () =>
          import('./features/projects/project-detail/project-detail').then((m) => m.ProjectDetail),
      },
      {
        path: 'projects/:id/edit',
        title: 'Edit project · Thomas',
        loadComponent: () =>
          import('./features/projects/project-form/project-form').then((m) => m.ProjectForm),
      },
      {
        path: 'errors',
        title: 'Error logs · Thomas',
        loadComponent: () =>
          import('./features/error-logs/error-log-list/error-log-list').then((m) => m.ErrorLogList),
      },
      {
        path: 'errors/:id',
        title: 'Error log · Thomas',
        loadComponent: () =>
          import('./features/error-logs/error-log-detail/error-log-detail').then(
            (m) => m.ErrorLogDetail,
          ),
      },
    ],
  },
  { path: '**', redirectTo: 'projects' },
];
