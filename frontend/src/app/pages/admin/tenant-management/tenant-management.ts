import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatSortModule, Sort } from '@angular/material/sort';
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
import { PAGINATION } from '../../../constants/pagination';
import { TenantForm, TenantFormData } from '../../../components/tenant-form/tenant-form';
import { ColumnReorder } from '../../../components/column-reorder/column-reorder';
import { exportToCsv, ExportColumn } from '../../../shared/csv-export';
import { loadColumnPreferences, saveColumnPreferences } from '../../../shared/column-preferences';

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

  private readonly tableKey = 'tenants';
  private readonly baseColumns = ['name', 'tenantType', 'location', 'email', 'isActive'];
  columnOrder = signal<string[]>([...this.baseColumns]);
  hiddenColumns = signal<Set<string>>(new Set());
  displayedColumns = computed(() => [...this.columnOrder().filter(c => !this.hiddenColumns().has(c)), 'actions']);

  pageSize = PAGINATION.defaultPageSize;
  pageSizeOptions = PAGINATION.pageSizeOptions;
  pageIndex = 0;
  sortActive = 'name';
  sortDirection: 'asc' | 'desc' | '' = 'asc';

  searchControl = this.fb.control('');
  private searchSubject = new Subject<string>();

  showForm = signal(false);
  editingTenant = signal<TenantItem | null>(null);
  errorMessage = signal<string | null>(null);
  showColumnMenu = signal(false);

  ngOnInit(): void {
    this.restoreColumnPreferences();

    this.searchSubject.pipe(debounceTime(400)).subscribe(() => {
      this.pageIndex = 0;
      this.loadTenants();
    });

    this.searchControl.valueChanges.subscribe(val => this.searchSubject.next(val || ''));
    this.loadTenants();
  }

  private restoreColumnPreferences(): void {
    const saved = loadColumnPreferences(this.tableKey);
    if (!saved) return;

    const validOrder = saved.order.filter(c => this.baseColumns.includes(c));
    const missing = this.baseColumns.filter(c => !validOrder.includes(c));
    this.columnOrder.set([...validOrder, ...missing]);
    this.hiddenColumns.set(new Set(saved.hidden.filter(c => this.baseColumns.includes(c))));
    if (saved.sortActive) this.sortActive = saved.sortActive;
    if (saved.sortDirection !== undefined) this.sortDirection = saved.sortDirection as 'asc' | 'desc' | '';
  }

  private persistColumnPreferences(): void {
    saveColumnPreferences(this.tableKey, {
      order: this.columnOrder(),
      hidden: Array.from(this.hiddenColumns()),
      sortActive: this.sortActive,
      sortDirection: this.sortDirection
    });
  }

  loadTenants(): void {
    this.loading.set(true);
    this.tenantService.getTenants({
      page: this.pageIndex + 1,
      pageSize: this.pageSize,
      sortBy: this.sortActive,
      sortDirection: this.sortDirection || 'asc',
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

  onPageChange(event: PageEvent): void {
    this.pageIndex = event.pageIndex;
    this.pageSize = event.pageSize;
    this.loadTenants();
  }

  onSortChange(sort: Sort): void {
    this.sortActive = sort.active;
    this.sortDirection = sort.direction;
    this.pageIndex = 0;
    this.persistColumnPreferences();
    this.loadTenants();
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

  onColumnsReordered(newOrder: string[]): void {
    this.columnOrder.set(newOrder);
    this.persistColumnPreferences();
  }

  onColumnVisibilityToggled(key: string): void {
    const updated = new Set(this.hiddenColumns());
    if (updated.has(key)) updated.delete(key); else updated.add(key);
    this.hiddenColumns.set(updated);
    this.persistColumnPreferences();
  }

  reorderableColumns = computed(() => {
    const defs = this.columnDefs();
    const hidden = this.hiddenColumns();
    return this.columnOrder().map(col => ({
      key: col,
      label: defs[col]?.header ?? col,
      hidden: hidden.has(col)
    }));
  });

  exportTenants(): void {
    this.tenantService.getAllTenants({
      sortBy: this.sortActive,
      sortDirection: this.sortDirection || 'asc',
      search: this.searchControl.value || undefined
    }).subscribe({
      next: (data) => exportToCsv('tenants.csv', this.getExportColumns(), data),
      error: (err) => this.errorMessage.set(err.error || this.transloco.translate('common.loadError'))
    });
  }

  private columnDefs(): Record<string, ExportColumn<TenantItem>> {
    return {
      name: {
        key: 'name',
        header: this.transloco.translate('tenants.name'),
        getValue: (t) => t.name
      },
      tenantType: {
        key: 'tenantType',
        header: this.transloco.translate('tenants.tenantType'),
        getValue: (t) => this.transloco.translate(`tenants.type_${t.tenantType}`)
      },
      location: {
        key: 'location',
        header: this.transloco.translate('profile.location'),
        getValue: (t) => [t.countryName, t.stateName, t.cityName].filter(Boolean).join(', ')
      },
      email: {
        key: 'email',
        header: this.transloco.translate('tenants.email'),
        getValue: (t) => t.email || ''
      },
      isActive: {
        key: 'isActive',
        header: this.transloco.translate('admin.users.status'),
        getValue: (t) => t.isActive ? this.transloco.translate('common.yes') : this.transloco.translate('common.no')
      }
    };
  }

  private getExportColumns(): ExportColumn<TenantItem>[] {
    const defs = this.columnDefs();
    return this.displayedColumns()
      .filter(col => col !== 'actions')
      .map(col => defs[col])
      .filter((col): col is ExportColumn<TenantItem> => !!col);
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
