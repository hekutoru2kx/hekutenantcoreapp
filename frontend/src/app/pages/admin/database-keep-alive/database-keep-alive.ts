import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { DatabaseKeepAliveManagement } from '../../../services/database-keep-alive-management';

@Component({
  selector: 'app-database-keep-alive',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatSlideToggleModule,
    MatButtonModule,
    MatCardModule,
    MatProgressBarModule,
    TranslocoModule
  ],
  templateUrl: './database-keep-alive.html',
  styleUrl: './database-keep-alive.scss',
})
export class DatabaseKeepAliveSettingsPage implements OnInit {
  private service = inject(DatabaseKeepAliveManagement);
  private fb = inject(FormBuilder);
  private transloco = inject(TranslocoService);

  loading = signal(false);
  saving = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  lastPingAt = signal<string | null>(null);

  intervalUnits = ['Minutes', 'Hours'];
  gmtOffsets = Array.from({ length: 27 }, (_, i) => i - 12); // -12..+14

  form = this.fb.group({
    isEnabled: [false],
    intervalAmount: [45, [Validators.required, Validators.min(1)]],
    intervalUnit: ['Minutes', Validators.required],
    gmtOffsetHours: [0, Validators.required],
    activeStartTime: ['00:00', Validators.required],
    activeEndTime: ['23:59', Validators.required]
  });

  ngOnInit(): void {
    this.load();
  }

  private load(): void {
    this.loading.set(true);
    this.service.getSettings().subscribe({
      next: (data) => {
        this.form.patchValue({
          isEnabled: data.isEnabled,
          intervalAmount: data.intervalAmount,
          intervalUnit: data.intervalUnit,
          gmtOffsetHours: data.gmtOffsetHours,
          activeStartTime: data.activeStartTime,
          activeEndTime: data.activeEndTime
        });
        this.lastPingAt.set(data.lastPingAt);
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

    this.service.updateSettings(this.form.value as any).subscribe({
      next: () => {
        this.saving.set(false);
        this.successMessage.set(this.transloco.translate('admin.databaseKeepAlive.saveSuccess'));
        this.load();
      },
      error: (err) => {
        this.errorMessage.set(err.error || this.transloco.translate('common.saveError'));
        this.saving.set(false);
      }
    });
  }
}
