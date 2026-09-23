import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormArray, FormGroup, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { LoggingSettingsManagement, LoggingCategorySetting } from '../../../services/logging-settings-management';

const ALL_LEVELS = ['Trace', 'Debug', 'Information', 'Warning', 'Error', 'Critical', 'None'];
// Security is the audit trail — the backend clamps it server-side too (never stricter than
// Warning), the dropdown just keeps a SuperAdmin from picking an option that would be clamped
// anyway.
const SECURITY_ALLOWED_LEVELS = ['Trace', 'Debug', 'Information', 'Warning'];

@Component({
  selector: 'app-logging-settings',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatCardModule,
    MatProgressBarModule,
    TranslocoModule
  ],
  templateUrl: './logging-settings.html',
  styleUrl: './logging-settings.scss',
})
export class LoggingSettingsPage implements OnInit {
  private service = inject(LoggingSettingsManagement);
  private fb = inject(FormBuilder);
  private transloco = inject(TranslocoService);

  loading = signal(false);
  saving = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);

  form = this.fb.group({
    categories: this.fb.array<FormGroup>([]),
    retentionDays: [30, [Validators.required, Validators.min(1)]]
  });

  ngOnInit(): void {
    this.load();
  }

  get categories(): FormArray {
    return this.form.get('categories') as FormArray;
  }

  levelsFor(category: string): string[] {
    return category === 'Security' ? SECURITY_ALLOWED_LEVELS : ALL_LEVELS;
  }

  private load(): void {
    this.loading.set(true);
    this.service.getSettings().subscribe({
      next: (data) => {
        this.categories.clear();
        for (const c of data.categories) {
          this.categories.push(this.fb.group({
            category: [c.category],
            minimumLevel: [c.minimumLevel, Validators.required]
          }));
        }
        this.form.patchValue({ retentionDays: data.retentionDays });
        this.loading.set(false);
      },
      error: (err) => {
        this.errorMessage.set(err.error || this.transloco.translate('common.loadError'));
        this.loading.set(false);
      }
    });
  }

  onSubmit(): void {
    if (this.form.invalid) return;

    this.saving.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    const value = this.form.getRawValue();
    const request = {
      categories: value.categories as LoggingCategorySetting[],
      retentionDays: value.retentionDays ?? 30
    };

    this.service.updateSettings(request).subscribe({
      next: () => {
        this.saving.set(false);
        this.successMessage.set(this.transloco.translate('admin.loggingSettings.saveSuccess'));
        this.load();
      },
      error: (err) => {
        this.errorMessage.set(err.error || this.transloco.translate('common.saveError'));
        this.saving.set(false);
      }
    });
  }
}
