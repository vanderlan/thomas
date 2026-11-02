import { TimeAgo } from './time-ago';

describe('TimeAgo', () => {
  const now = Date.UTC(2025, 5, 1, 12, 0, 0);
  const pipe = new TimeAgo();

  it('formats relative dates', () => {
    expect(pipe.transform(new Date(now - 5 * 60_000).toISOString(), now)).toBe('5 minutes ago');
    expect(pipe.transform(new Date(now - 3 * 3_600_000).toISOString(), now)).toBe('3 hours ago');
    expect(pipe.transform(new Date(now - 2 * 86_400_000).toISOString(), now)).toBe('2 days ago');
    expect(pipe.transform(new Date(now + 86_400_000).toISOString(), now)).toBe('tomorrow');
  });

  it('handles the edge cases', () => {
    expect(pipe.transform(null)).toBe('never');
    expect(pipe.transform(undefined)).toBe('never');
    expect(pipe.transform('not a date')).toBe('');
    expect(pipe.transform(new Date(now - 3_000), now)).toBe('just now');
  });
});
