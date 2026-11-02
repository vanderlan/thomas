import { Pipe, PipeTransform } from '@angular/core';

const UNITS: [Intl.RelativeTimeFormatUnit, number][] = [
  ['year', 365 * 24 * 60 * 60],
  ['month', 30 * 24 * 60 * 60],
  ['day', 24 * 60 * 60],
  ['hour', 60 * 60],
  ['minute', 60],
];

/** Formats a date as a relative time ("5 minutes ago"). */
@Pipe({ name: 'timeAgo' })
export class TimeAgo implements PipeTransform {
  private readonly formatter = new Intl.RelativeTimeFormat('en', { numeric: 'auto' });

  transform(value: string | Date | null | undefined, now: number = Date.now()): string {
    if (!value) {
      return 'never';
    }

    const date = typeof value === 'string' ? new Date(value) : value;

    if (Number.isNaN(date.getTime())) {
      return '';
    }

    const seconds = Math.round((date.getTime() - now) / 1000);

    for (const [unit, unitSeconds] of UNITS) {
      if (Math.abs(seconds) >= unitSeconds) {
        return this.formatter.format(Math.round(seconds / unitSeconds), unit);
      }
    }

    return Math.abs(seconds) < 10 ? 'just now' : this.formatter.format(seconds, 'second');
  }
}
