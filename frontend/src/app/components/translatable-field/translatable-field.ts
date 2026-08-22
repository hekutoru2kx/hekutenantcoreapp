import { Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { AVAILABLE_LANGUAGES, BASE_LANGUAGE } from '../../constants/languages';

// Generic per-language override editor for a single free-text field on any entity.
// Not tied to any one entity or scope — reused wherever a base value needs translations. Loops
// over AVAILABLE_LANGUAGES minus the base language, so adding a language there is enough for
// this component to grow another input, with no template changes needed here.
@Component({
  selector: 'app-translatable-field',
  imports: [CommonModule, MatFormFieldModule, MatInputModule],
  templateUrl: './translatable-field.html',
  styleUrl: './translatable-field.scss',
})
export class TranslatableField {
  label = input.required<string>();
  translations = input<Record<string, string>>({});
  translationsChange = output<Record<string, string>>();

  readonly otherLanguages = AVAILABLE_LANGUAGES.filter((l) => l !== BASE_LANGUAGE);

  onValueChange(lang: string, value: string): void {
    const updated = { ...this.translations() };
    if (value) {
      updated[lang] = value;
    } else {
      delete updated[lang];
    }
    this.translationsChange.emit(updated);
  }
}
