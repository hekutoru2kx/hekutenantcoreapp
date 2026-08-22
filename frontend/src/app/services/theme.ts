import { Service, signal, effect, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';

export type ThemeMode = 'light' | 'dark';
export type ColorTheme = 'azure' | 'rose' | 'green' | 'violet' | 'orange';

@Service()
export class Theme {
  private http = inject(HttpClient);

  mode = signal<ThemeMode>('light');
  colorTheme = signal<ColorTheme>('azure');

  availableColorThemes: { value: ColorTheme; labelKey: string }[] = [
    { value: 'azure', labelKey: 'theme.azure' },
    { value: 'rose', labelKey: 'theme.rose' },
    { value: 'green', labelKey: 'theme.green' },
    { value: 'violet', labelKey: 'theme.violet' },
    { value: 'orange', labelKey: 'theme.orange' }
  ];

  private syncBodyClass = effect(() => {
    const body = document.body;
    body.classList.remove('light-theme', 'dark-theme');
    body.classList.add(`${this.mode()}-theme`);

    body.classList.remove(...this.availableColorThemes.map(t => `theme-${t.value}`));
    body.classList.add(`theme-${this.colorTheme()}`);
  });

  toggleMode(): void {
    this.mode.set(this.mode() === 'light' ? 'dark' : 'light');
  }

  setColorTheme(theme: ColorTheme): void {
    this.colorTheme.set(theme);
  }
}