import { Component, input, output, signal, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormControl } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatAutocompleteModule, MatAutocompleteSelectedEvent } from '@angular/material/autocomplete';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { Observable, of } from 'rxjs';
import { debounceTime, distinctUntilChanged, switchMap } from 'rxjs/operators';

export interface EntityPickerOption {
  id: number;
  label: string;
}

const MIN_SEARCH_LENGTH = 2;

// Search-as-you-type autocomplete for picking an entity by id — the first component of its
// kind in this codebase, generic over the searched type T so both a Person search and an
// Employee search can reuse it. Deliberately not a
// ControlValueAccessor (no other custom form component here implements one, e.g.
// translatable-field) — the parent just listens for `selected` and stores the id itself,
// matching that same plain input/output convention.
@Component({
  selector: 'app-entity-picker',
  imports: [CommonModule, ReactiveFormsModule, MatFormFieldModule, MatInputModule, MatAutocompleteModule, MatProgressSpinnerModule, MatButtonModule, MatIconModule],
  templateUrl: './entity-picker.html',
  styleUrl: './entity-picker.scss',
})
export class EntityPicker<T> {
  label = input.required<string>();
  search = input.required<(term: string) => Observable<T[]>>();
  toOption = input.required<(item: T) => EntityPickerOption>();
  initialValue = input<EntityPickerOption | null>(null);
  disabled = input<boolean>(false);

  selected = output<number | null>();

  readonly minSearchLength = MIN_SEARCH_LENGTH;
  searching = signal(false);
  filteredOptions = signal<EntityPickerOption[]>([]);

  // MatAutocomplete's own recipe for "select an object, display a derived string" — the
  // control's raw value oscillates between a plain string (while typing) and an
  // EntityPickerOption (right after a selection); displayFn normalizes both for what's shown.
  searchControl = new FormControl<string | EntityPickerOption | null>('');

  constructor() {
    effect(() => {
      const initial = this.initialValue();
      if (initial && this.searchControl.value !== initial) {
        this.searchControl.setValue(initial, { emitEvent: false });
      }
    });

    effect(() => {
      if (this.disabled()) {
        this.searchControl.disable({ emitEvent: false });
      } else {
        this.searchControl.enable({ emitEvent: false });
      }
    });

    this.searchControl.valueChanges.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      switchMap((term) => {
        const query = typeof term === 'string' ? term.trim() : '';
        if (query.length < MIN_SEARCH_LENGTH) {
          this.searching.set(false);
          return of([] as T[]);
        }
        this.searching.set(true);
        return this.search()(query);
      })
    ).subscribe({
      next: (items) => {
        this.searching.set(false);
        this.filteredOptions.set(items.map((item) => this.toOption()(item)));
      },
      error: () => this.searching.set(false)
    });
  }

  displayFn = (value: string | EntityPickerOption | null): string =>
    value && typeof value !== 'string' ? value.label : (value ?? '');

  onOptionSelected(event: MatAutocompleteSelectedEvent): void {
    const option = event.option.value as EntityPickerOption;
    this.selected.emit(option.id);
  }

  clear(): void {
    this.searchControl.setValue('');
    this.filteredOptions.set([]);
    this.selected.emit(null);
  }
}
