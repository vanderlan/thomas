import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Toast } from '../../core/notifications/toast';

@Component({
  selector: 'app-toasts',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="toasts" aria-live="polite">
      @for (message of toast.messages(); track message.id) {
        <div class="toast" [class]="'toast toast--' + message.kind" role="status">
          <span class="toast__text">{{ message.text }}</span>
          <button
            type="button"
            class="toast__close"
            (click)="toast.dismiss(message.id)"
            aria-label="Dismiss"
          >
            ×
          </button>
        </div>
      }
    </div>
  `,
})
export class Toasts {
  protected readonly toast = inject(Toast);
}
