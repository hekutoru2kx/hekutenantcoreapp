import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder } from '@angular/forms';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { AppSettings, AppSettingsService } from '../../../services/app-settings';

@Component({
  selector: 'app-app-settings',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatSlideToggleModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressBarModule,
    TranslocoModule
  ],
  templateUrl: './app-settings.html',
  styleUrl: './app-settings.scss',
})
export class AppSettingsPage implements OnInit {
  private service = inject(AppSettingsService);
  private fb = inject(FormBuilder);
  private transloco = inject(TranslocoService);

  loading = signal(false);
  saving = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);

  form = this.fb.group({
    requireEmailConfirmation: [false],
    contentMaxBytes: [5 * 1024 * 1024],
    contentAllowedContentTypes: ['image/jpeg,image/png,image/webp'],
    contentMaxImageDimension: [2048],
    contentAvatarMaxDimension: [512]
  });

  ngOnInit(): void {
    this.load();
  }

  private load(): void {
    this.loading.set(true);
    this.service.getSettings().subscribe({
      next: (data) => {
        this.form.patchValue(data);
        this.loading.set(false);
      },
      error: (err) => {
        this.errorMessage.set(err.error || this.transloco.translate('common.loadError'));
        this.loading.set(false);
      }
    });
  }

  onSubmit(): void {
    this.saving.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    this.service.updateSettings(this.form.value as AppSettings).subscribe({
      next: () => {
        this.saving.set(false);
        this.successMessage.set(this.transloco.translate('admin.appSettings.saveSuccess'));
        this.load();
      },
      error: (err) => {
        this.errorMessage.set(err.error || this.transloco.translate('common.saveError'));
        this.saving.set(false);
      }
    });
  }
}
