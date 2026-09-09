import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule } from '@angular/material/paginator';
import { MatSortModule } from '@angular/material/sort';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatCardModule } from '@angular/material/card';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { Auth } from '../../../services/auth';
import { TenantManagement as TenantManagementService, TenantItem } from '../../../services/tenant-management';
import { debounceTime, Subject } from 'rxjs';
import { TenantForm, TenantFormData } from '../../../components/tenant-form/tenant-form';
import { ColumnReorder } from '../../../components/column-reorder/column-reorder';
import { DataTableController } from '../../../shared/data-table-controller';

@Component({
  selector: 'app-tenant-management',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatTableModule,
    MatPaginatorModule,
    MatSortModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatCardModule,
    MatTooltipModule,
    MatProgressBarModule,
    TranslocoModule,
    TenantForm,
    ColumnReorder
  ],
  templateUrl: './tenant-management.html',
  styleUrl: './tenant-management.scss',
})
export class TenantManagement implements OnInit {
  private tenantService = inject(TenantManagementService);
  protected auth = inject(Auth);
  private fb = inject(FormBuilder);
  private transloco = inject(TranslocoService);

  editingTenantFormData = signal<TenantFormData | null>(null);
  tenants = signal<TenantItem[]>([]);
  totalCount = signal(0);
  loading = signal(false);

  table = new DataTableController<TenantItem>({
    tableKey: 'tenants',
    defaultSort: { active: 'name', direction: 'asc' },
    persistSort: true,
    onChange: () => this.loadTenants(),
    columns: [
      { key: 'name', header: () => this.transloco.translate('tenants.name'), sortable: true, exportValue: (t) => t.name },
      { key: 'tenantType', header: () => this.transloco.translate('tenants.tenantType'), exportValue: (t) => this.transloco.translate(`tenants.type_${t.tenantType}`) },
      { key: 'location', header: () => this.transloco.translate('profile.location'), exportValue: (t) => [t.countryName, t.stateName, t.cityName].filter(Boolean).join(', ') },
      { key: 'email', header: () => this.transloco.translate('tenants.email'), exportValue: (t) => t.email || '' },
      {
        key: 'isActive',
        header: () => this.transloco.translate('admin.users.status'),
        exportValue: (t) => t.isActive ? this.transloco.translate('common.yes') : this.transloco.translate('common.no'),
      },
    ],
  });

  searchControl = this.fb.control('');
  private searchSubject = new Subject<string>();

  showForm = signal(false);
  editingTenant = signal<TenantItem | null>(null);
  errorMessage = signal<string | null>(null);

  ngOnInit(): void {
    this.table.restore();

    this.searchSubject.pipe(debounceTime(400)).subscribe(() => {
      this.table.pageIndex = 0;
      this.loadTenants();
    });

    this.searchControl.valueChanges.subscribe(val => this.searchSubject.next(val || ''));
    this.loadTenants();
  }

  loadTenants(): void {
    this.loading.set(true);
    this.tenantService.getTenants({
      page: this.table.page,
      pageSize: this.table.pageSize,
      sortBy: this.table.sortActive,
      sortDirection: this.table.sortDirection || 'asc',
      search: this.searchControl.value || undefined
    }).subscribe({
      next: (data) => {
        this.tenants.set(data.items);
        this.totalCount.set(data.totalCount);
        this.loading.set(false);
      },
      error: (err) => {
        this.errorMessage.set(err.error || this.transloco.translate('common.loadError'));
        this.loading.set(false);
      }
    });
  }

  exportTenants(): void {
    this.tenantService.getAllTenants({
      sortBy: this.table.sortActive,
      sortDirection: this.table.sortDirection || 'asc',
      search: this.searchControl.value || undefined
    }).subscribe({
      next: (data) => this.table.exportRows('tenants.csv', data),
      error: (err) => this.errorMessage.set(err.error || this.transloco.translate('common.loadError'))
    });
  }

  toggleForm(tenant?: TenantItem): void {
    this.editingTenant.set(tenant || null);
    this.editingTenantFormData.set(tenant ? this.toFormData(tenant) : null);
    this.showForm.set(!this.showForm());
    this.errorMessage.set(null);
  }

  onTenantSaved(data: TenantFormData): void {
    const editing = this.editingTenant();
    const onSuccess = () => {
      this.showForm.set(false);
      this.editingTenant.set(null);
      this.loadTenants();
    };
    const onError = (err: any) => this.errorMessage.set(err.error || this.transloco.translate('common.saveError'));

    if (editing) {
      this.tenantService.updateTenant(editing.id, data).subscribe({ next: onSuccess, error: onError });
    } else {
      this.tenantService.createTenant(data).subscribe({ next: onSuccess, error: onError });
    }
  }

  onTenantCancelled(): void {
    this.showForm.set(false);
    this.editingTenant.set(null);
  }

  canCreateTenant(): boolean {
    return this.auth.hasClaim('TenantsPermission', 'Create');
  }

  canUpdateTenant(): boolean {
    return this.auth.hasClaim('TenantsPermission', 'Update');
  }

  toFormData(tenant: TenantItem): TenantFormData {
    return {
      name: tenant.name,
      tenantType: tenant.tenantType,
      countryId: tenant.countryId,
      stateId: tenant.stateId,
      cityId: tenant.cityId,
      phone: tenant.phone,
      urlSite: tenant.urlSite,
      email: tenant.email,
      isActive: tenant.isActive,
      attachmentRetentionDays: tenant.attachmentRetentionDays
    };
  }
}
