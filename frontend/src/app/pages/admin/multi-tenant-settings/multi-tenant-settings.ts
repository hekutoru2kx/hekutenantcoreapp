import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { MultiTenantSettingsManagement } from '../../../services/multi-tenant-settings-management';
import { TenantManagement } from '../../../services/tenant-management';
import { TenantSummary } from '../../../services/auth';

@Component({
  selector: 'app-multi-tenant-settings',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatSelectModule,
    MatSlideToggleModule,
    MatButtonModule,
    MatCardModule,
    MatProgressBarModule,
    TranslocoModule
  ],
  templateUrl: './multi-tenant-settings.html',
  styleUrl: './multi-tenant-settings.scss',
})
export class MultiTenantSettingsPage implements OnInit {
  private service = inject(MultiTenantSettingsManagement);
  private tenantManagement = inject(TenantManagement);
  private fb = inject(FormBuilder);
  private transloco = inject(TranslocoService);

  loading = signal(false);
  saving = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  tenants = signal<TenantSummary[]>([]);

  form = this.fb.group({
    defaultTenantLoginEnabled: [false],
    multiTenantDisabled: [false],
    defaultTenantId: [null as number | null]
  });

  ngOnInit(): void {
    this.tenantManagement.getActiveTenants().subscribe({
      next: (data) => this.tenants.set(data)
    });
    this.load();
  }

  private load(): void {
    this.loading.set(true);
    this.service.getSettings().subscribe({
      next: (data) => {
        this.form.patchValue({
          defaultTenantLoginEnabled: data.defaultTenantLoginEnabled,
          multiTenantDisabled: data.multiTenantDisabled,
          defaultTenantId: data.defaultTenantId
        });
        this.loading.set(false);
      },
      error: (err) => {
        this.errorMessage.set(err.error || this.transloco.translate('common.loadError'));
        this.loading.set(false);
      }
    });
  }

  onDefaultTenantLoginToggle(checked: boolean): void {
    this.form.controls.defaultTenantLoginEnabled.setValue(checked);
  }

  onSubmit(): void {
    if (this.form.invalid) return;

    this.saving.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    this.service.updateSettings(this.form.value as any).subscribe({
      next: () => {
        this.saving.set(false);
        this.successMessage.set(this.transloco.translate('admin.multiTenantSettings.saveSuccess'));
        this.load();
      },
      error: (err) => {
        this.errorMessage.set(err.error || this.transloco.translate('common.saveError'));
        this.saving.set(false);
      }
    });
  }
}
