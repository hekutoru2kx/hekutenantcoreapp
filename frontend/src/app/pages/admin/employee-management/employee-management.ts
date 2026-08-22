import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatSortModule, Sort } from '@angular/material/sort';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule, DateAdapter, MAT_DATE_FORMATS } from '@angular/material/core';
import { MatCardModule } from '@angular/material/card';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { RouterLink } from '@angular/router';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { Auth } from '../../../services/auth';
import { EmployeeManagement as EmployeeManagementService, EmployeeItem } from '../../../services/employee-management';
import { debounceTime, Subject } from 'rxjs';
import { PAGINATION } from '../../../constants/pagination';
import { YmdDateAdapter, YMD_DATE_FORMATS } from '../../../shared/ymd-date-adapter';
import { ColumnReorder } from '../../../components/column-reorder/column-reorder';
import { exportToCsv, ExportColumn } from '../../../shared/csv-export';
import { loadColumnPreferences, saveColumnPreferences } from '../../../shared/column-preferences';

@Component({
  selector: 'app-employee-management',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatTableModule,
    MatPaginatorModule,
    MatSortModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatCardModule,
    MatTooltipModule,
    MatProgressBarModule,
    TranslocoModule,
    RouterLink,
    ColumnReorder
  ],
  providers: [
    { provide: DateAdapter, useClass: YmdDateAdapter },
    { provide: MAT_DATE_FORMATS, useValue: YMD_DATE_FORMATS },
  ],
  templateUrl: './employee-management.html',
  styleUrl: './employee-management.scss',
})
export class EmployeeManagement implements OnInit {
  private employeeService = inject(EmployeeManagementService);
  protected auth = inject(Auth);
  private fb = inject(FormBuilder);
  private transloco = inject(TranslocoService);

  employees = signal<EmployeeItem[]>([]);
  totalCount = signal(0);
  loading = signal(false);
  availableRoles = signal<string[]>([]);

  private readonly tableKey = 'employees';
  private readonly baseColumns = ['userName', 'email', 'jobTitle', 'roles', 'isActive'];
  columnOrder = signal<string[]>([...this.baseColumns]);
  hiddenColumns = signal<Set<string>>(new Set());
  displayedColumns = computed(() => [...this.columnOrder().filter(c => !this.hiddenColumns().has(c)), 'actions']);

  pageSize = PAGINATION.defaultPageSize;
  pageSizeOptions = PAGINATION.pageSizeOptions;
  pageIndex = 0;
  sortActive = 'jobTitle';
  sortDirection: 'asc' | 'desc' | '' = 'asc';

  searchControl = this.fb.control('');
  private searchSubject = new Subject<string>();

  showInviteForm = signal(false);
  createdPassword = signal<string | null>(null);
  errorMessage = signal<string | null>(null);
  showColumnMenu = signal(false);

  inviteForm = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    userName: [''],
    roleName: ['', Validators.required],
    roleExpiresAt: [null as Date | null],
    jobTitle: ['']
  });

  ngOnInit(): void {
    this.restoreColumnPreferences();

    this.searchSubject.pipe(debounceTime(400)).subscribe(() => {
      this.pageIndex = 0;
      this.loadEmployees();
    });

    this.searchControl.valueChanges.subscribe(val => this.searchSubject.next(val || ''));
    this.loadEmployees();
    this.loadAvailableRoles();
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

  loadEmployees(): void {
    this.loading.set(true);
    this.employeeService.getEmployees({
      page: this.pageIndex + 1,
      pageSize: this.pageSize,
      sortBy: this.sortActive,
      sortDirection: this.sortDirection || 'asc',
      search: this.searchControl.value || undefined
    }).subscribe({
      next: (data) => {
        this.employees.set(data.items);
        this.totalCount.set(data.totalCount);
        this.loading.set(false);
      },
      error: (err) => {
        this.errorMessage.set(err.error || this.transloco.translate('common.loadError'));
        this.loading.set(false);
      }
    });
  }

  loadAvailableRoles(): void {
    // Uses the tenant-scoped assignable-roles endpoint (EmployeesPermission), not the
    // System role catalog (RolesPermission, SuperAdmin-only) — a tenant Admin doesn't
    // have RolesPermission, so that call would 403 and leave this list empty.
    this.employeeService.getAssignableRoles().subscribe({
      next: (names) => this.availableRoles.set(names),
      error: (err) => this.errorMessage.set(err.error || this.transloco.translate('common.loadError'))
    });
  }

  onPageChange(event: PageEvent): void {
    this.pageIndex = event.pageIndex;
    this.pageSize = event.pageSize;
    this.loadEmployees();
  }

  onSortChange(sort: Sort): void {
    this.sortActive = sort.active;
    this.sortDirection = sort.direction;
    this.pageIndex = 0;
    this.persistColumnPreferences();
    this.loadEmployees();
  }

  toggleInviteForm(): void {
    this.showInviteForm.set(!this.showInviteForm());
    this.createdPassword.set(null);
    this.inviteForm.reset();
  }

  invite(): void {
    if (this.inviteForm.invalid) {
      // Without this, an invalid submit (e.g. no role picked) just silently no-ops —
      // Material only renders mat-error once a control is touched.
      this.inviteForm.markAllAsTouched();
      return;
    }

    const { email, userName, roleName, roleExpiresAt, jobTitle } = this.inviteForm.value;
    this.employeeService.inviteEmployee({
      email: email!,
      userName: userName || undefined,
      roleName: roleName!,
      roleExpiresAt: roleExpiresAt ? roleExpiresAt.toISOString() : undefined,
      jobTitle: jobTitle || undefined
    }).subscribe({
      next: () => {
        this.loadEmployees();
        this.inviteForm.reset();
      },
      error: (err) => this.errorMessage.set(err.error || this.transloco.translate('common.saveError'))
    });
  }

  toggleActive(employee: EmployeeItem): void {
    const action = employee.isActive
      ? this.employeeService.deactivate(employee.id)
      : this.employeeService.activate(employee.id);

    action.subscribe({
      next: () => this.loadEmployees(),
      error: (err) => this.errorMessage.set(err.error || this.transloco.translate('common.saveError'))
    });
  }

  canCreateEmployee(): boolean {
    return this.auth.hasClaim('EmployeesPermission', 'Create');
  }

  canUpdateEmployee(): boolean {
    return this.auth.hasClaim('EmployeesPermission', 'Update');
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

  exportEmployees(): void {
    this.employeeService.getAllEmployees({
      sortBy: this.sortActive,
      sortDirection: this.sortDirection || 'asc',
      search: this.searchControl.value || undefined
    }).subscribe({
      next: (data) => exportToCsv('employees.csv', this.getExportColumns(), data),
      error: (err) => this.errorMessage.set(err.error || this.transloco.translate('common.loadError'))
    });
  }

  private columnDefs(): Record<string, ExportColumn<EmployeeItem>> {
    return {
      userName: {
        key: 'userName',
        header: this.transloco.translate('admin.users.name'),
        getValue: (e) => e.userName
      },
      email: {
        key: 'email',
        header: this.transloco.translate('admin.users.email'),
        getValue: (e) => e.email
      },
      jobTitle: {
        key: 'jobTitle',
        header: this.transloco.translate('admin.employees.jobTitle'),
        getValue: (e) => e.jobTitle || ''
      },
      roles: {
        key: 'roles',
        header: this.transloco.translate('admin.users.roles'),
        getValue: (e) => e.roles.join('; ')
      },
      isActive: {
        key: 'isActive',
        header: this.transloco.translate('admin.users.status'),
        getValue: (e) => e.isActive ? this.transloco.translate('common.yes') : this.transloco.translate('common.no')
      }
    };
  }

  private getExportColumns(): ExportColumn<EmployeeItem>[] {
    const defs = this.columnDefs();
    return this.displayedColumns()
      .filter(col => col !== 'actions')
      .map(col => defs[col])
      .filter((col): col is ExportColumn<EmployeeItem> => !!col);
  }
}
