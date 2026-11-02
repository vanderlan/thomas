import { DOCUMENT } from '@angular/common';
import { effect, inject, Injectable, signal } from '@angular/core';

export type ThemeMode = 'dark' | 'light';

const STORAGE_KEY = 'thomas.theme';

/**
 * Dark mode is the default. The preference is persisted and applied through the
 * "data-theme" attribute of the <html> element (see styles.css).
 */
@Injectable({ providedIn: 'root' })
export class Theme {
  private readonly document = inject(DOCUMENT);

  readonly mode = signal<ThemeMode>(this.restore());
  readonly isDark = () => this.mode() === 'dark';

  constructor() {
    effect(() => {
      const mode = this.mode();
      this.document.documentElement.setAttribute('data-theme', mode);
      this.document.documentElement.style.colorScheme = mode;

      try {
        localStorage.setItem(STORAGE_KEY, mode);
      } catch {
        // storage unavailable (private mode): the preference is simply not persisted
      }
    });
  }

  toggle(): void {
    this.mode.update((mode) => (mode === 'dark' ? 'light' : 'dark'));
  }

  private restore(): ThemeMode {
    try {
      const stored = localStorage.getItem(STORAGE_KEY);
      return stored === 'light' ? 'light' : 'dark';
    } catch {
      return 'dark';
    }
  }
}
