import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

@Component({
  selector: 'app-pagination',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (total() > 0) {
      <nav class="pagination" aria-label="Pagination">
        <span class="pagination__summary">{{ from() }}–{{ to() }} of {{ total() }}</span>
        <div class="pagination__controls">
          <button
            type="button"
            class="btn btn--ghost btn--sm"
            [disabled]="page() <= 1"
            (click)="pageChange.emit(page() - 1)"
            aria-label="Previous page"
          >
            ‹ Previous
          </button>
          <span class="pagination__page">Page {{ page() }} of {{ pages() }}</span>
          <button
            type="button"
            class="btn btn--ghost btn--sm"
            [disabled]="page() >= pages()"
            (click)="pageChange.emit(page() + 1)"
            aria-label="Next page"
          >
            Next ›
          </button>
        </div>
      </nav>
    }
  `,
})
export class Pagination {
  readonly page = input.required<number>();
  readonly pageSize = input.required<number>();
  readonly total = input.required<number>();
  readonly pageChange = output<number>();

  readonly pages = computed(() => Math.max(1, Math.ceil(this.total() / this.pageSize())));
  readonly from = computed(() => (this.page() - 1) * this.pageSize() + 1);
  readonly to = computed(() => Math.min(this.page() * this.pageSize(), this.total()));
}
