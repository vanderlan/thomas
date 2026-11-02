import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Pagination } from './pagination';

@Component({
  imports: [Pagination],
  template: `<app-pagination
    [page]="page()"
    [pageSize]="10"
    [total]="total()"
    (pageChange)="page.set($event)"
  />`,
})
class Host {
  readonly page = signal(2);
  readonly total = signal(45);
}

describe('Pagination', () => {
  it('shows the range and navigates between pages', async () => {
    await TestBed.configureTestingModule({ imports: [Host] }).compileComponents();

    const fixture = TestBed.createComponent(Host);
    await fixture.whenStable();

    const element: HTMLElement = fixture.nativeElement;
    expect(element.querySelector('.pagination__summary')?.textContent).toContain('11–20 of 45');
    expect(element.querySelector('.pagination__page')?.textContent).toContain('Page 2 of 5');

    const [previous, next] = Array.from(element.querySelectorAll('button'));
    next.click();
    await fixture.whenStable();
    expect(fixture.componentInstance.page()).toBe(3);

    previous.click();
    await fixture.whenStable();
    expect(fixture.componentInstance.page()).toBe(2);
  });

  it('renders nothing without items', async () => {
    await TestBed.configureTestingModule({ imports: [Host] }).compileComponents();

    const fixture = TestBed.createComponent(Host);
    fixture.componentInstance.total.set(0);
    await fixture.whenStable();

    expect(fixture.nativeElement.querySelector('nav')).toBeNull();
  });
});
