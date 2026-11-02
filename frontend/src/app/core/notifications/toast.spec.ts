import { Toast } from './toast';

describe('Toast', () => {
  beforeEach(() => vi.useFakeTimers());
  afterEach(() => vi.useRealTimers());

  it('queues and auto dismisses the messages', () => {
    const toast = new Toast();

    toast.success('saved');
    toast.error('failed');

    expect(toast.messages().map((m) => [m.kind, m.text])).toEqual([
      ['success', 'saved'],
      ['error', 'failed'],
    ]);

    vi.advanceTimersByTime(4000);
    expect(toast.messages().map((m) => m.text)).toEqual(['failed']);

    vi.advanceTimersByTime(4000);
    expect(toast.messages()).toEqual([]);
  });

  it('dismisses a message on demand', () => {
    const toast = new Toast();

    toast.info('hello');
    toast.dismiss(toast.messages()[0].id);

    expect(toast.messages()).toEqual([]);
  });
});
