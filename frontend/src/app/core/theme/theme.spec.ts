import { TestBed } from '@angular/core/testing';
import { Theme } from './theme';

describe('Theme', () => {
  beforeEach(() => {
    localStorage.clear();
    document.documentElement.removeAttribute('data-theme');
  });

  afterEach(() => localStorage.clear());

  function createTheme(): Theme {
    TestBed.resetTestingModule();
    const theme = TestBed.inject(Theme);
    TestBed.tick();
    return theme;
  }

  it('uses the dark mode by default', () => {
    const theme = createTheme();

    expect(theme.mode()).toBe('dark');
    expect(theme.isDark()).toBe(true);
    expect(document.documentElement.getAttribute('data-theme')).toBe('dark');
  });

  it('toggles and persists the preference', () => {
    const theme = createTheme();

    theme.toggle();
    TestBed.tick();

    expect(theme.mode()).toBe('light');
    expect(document.documentElement.getAttribute('data-theme')).toBe('light');
    expect(localStorage.getItem('thomas.theme')).toBe('light');

    theme.toggle();
    TestBed.tick();

    expect(theme.mode()).toBe('dark');
  });

  it('restores the persisted preference', () => {
    localStorage.setItem('thomas.theme', 'light');

    expect(createTheme().mode()).toBe('light');
  });
});
