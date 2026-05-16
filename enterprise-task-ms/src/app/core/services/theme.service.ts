import { DOCUMENT } from '@angular/common';
import { Injectable, inject, signal } from '@angular/core';

export type ThemeMode = 'light' | 'dark';

const THEME_STORAGE_KEY = 'etms-theme';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly documentRef = inject(DOCUMENT);

  readonly mode = signal<ThemeMode>(this.restoreTheme());

  constructor() {
    this.applyTheme(this.mode());
  }

  toggleTheme() {
    const nextMode: ThemeMode = this.mode() === 'dark' ? 'light' : 'dark';
    this.mode.set(nextMode);
    localStorage.setItem(THEME_STORAGE_KEY, nextMode);
    this.applyTheme(nextMode);
  }

  private restoreTheme(): ThemeMode {
    const storedTheme = localStorage.getItem(THEME_STORAGE_KEY);

    if (storedTheme === 'light' || storedTheme === 'dark') {
      return storedTheme;
    }

    return this.documentRef.defaultView?.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
  }

  private applyTheme(mode: ThemeMode) {
    this.documentRef.documentElement.dataset['theme'] = mode;
  }
}
