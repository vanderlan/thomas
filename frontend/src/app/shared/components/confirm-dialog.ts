import { ChangeDetectionStrategy, Component, input, output, signal } from '@angular/core';

/**
 * Minimal confirmation dialog built on the native <dialog> element.
 * Usage: <app-confirm-dialog #dialog (confirmed)="..."> then dialog.open().
 */
@Component({
  selector: 'app-confirm-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (isOpen()) {
      <div class="dialog-backdrop" (click)="cancel()" role="presentation"></div>
      <div class="dialog" role="dialog" aria-modal="true" [attr.aria-label]="title()">
        <h2 class="dialog__title">{{ title() }}</h2>
        <p class="dialog__message">{{ message() }}</p>
        <div class="dialog__actions">
          <button type="button" class="btn btn--ghost" (click)="cancel()">Cancel</button>
          <button type="button" class="btn" [class.btn--danger]="danger()" (click)="confirm()">
            {{ confirmLabel() }}
          </button>
        </div>
      </div>
    }
  `,
})
export class ConfirmDialog {
  readonly title = input('Are you sure?');
  readonly message = input('');
  readonly confirmLabel = input('Confirm');
  readonly danger = input(false);
  readonly confirmed = output<void>();

  readonly isOpen = signal(false);

  open(): void {
    this.isOpen.set(true);
  }

  cancel(): void {
    this.isOpen.set(false);
  }

  confirm(): void {
    this.isOpen.set(false);
    this.confirmed.emit();
  }
}
