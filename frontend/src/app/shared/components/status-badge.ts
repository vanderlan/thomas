import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { ErrorLogStatus, errorLogStatusLabel } from '../../core/models/error-log';

const CLASS_BY_STATUS: Record<ErrorLogStatus, string> = {
  [ErrorLogStatus.New]: 'badge--new',
  [ErrorLogStatus.Recurring]: 'badge--recurring',
  [ErrorLogStatus.Resolved]: 'badge--resolved',
  [ErrorLogStatus.Reopened]: 'badge--reopened',
};

@Component({
  selector: 'app-status-badge',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<span class="badge" [class]="'badge ' + cssClass()">{{ label() }}</span>`,
})
export class StatusBadge {
  readonly status = input.required<ErrorLogStatus>();

  readonly label = computed(() => errorLogStatusLabel(this.status()));
  readonly cssClass = computed(() => CLASS_BY_STATUS[this.status()] ?? '');
}
