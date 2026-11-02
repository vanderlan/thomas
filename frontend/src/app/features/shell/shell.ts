import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthSession } from '../../core/auth/auth-session';
import { Theme } from '../../core/theme/theme';
import { Toasts } from '../../shared/components/toasts';

/** Authenticated layout: navigation, theme toggle, user menu and the routed content. */
@Component({
  selector: 'app-shell',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, Toasts],
  templateUrl: './shell.html',
  styleUrl: './shell.css',
})
export class Shell {
  protected readonly session = inject(AuthSession);
  protected readonly theme = inject(Theme);
  private readonly router = inject(Router);

  protected logout(): void {
    this.session.logout();
    void this.router.navigate(['/login']);
  }
}
