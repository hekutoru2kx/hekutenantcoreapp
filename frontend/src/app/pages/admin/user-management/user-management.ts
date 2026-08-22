import { Component, inject, signal, computed, OnInit, ViewChild, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, MatPaginator, PageEvent } from '@angular/material/paginator';
import { MatSortModule, MatSort, Sort } from '@angular/material/sort';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatCardModule } from '@angular/material/card';
import { MatDividerModule } from '@angular/material/divider';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { Admin, UserListItem } from '../../../services/admin';
import { TenantSummary } from '../../../services/auth';
import { debounceTime, Subject } from 'rxjs';
import { PAGINATION } from '../../../constants/pagination';
import { ColumnReorder } from '../../../components/column-reorder/column-reorder';
import { exportToCsv, ExportColumn } from '../../../shared/csv-export';
import { loadColumnPreferences, saveColumnPreferences } from '../../../shared/column-preferences';

@Component({
  selector: 'app-user-management',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatTableModule,
    MatPaginatorModule,
    MatSortModule,
    MatButtonModule,
    MatIconModule,
    MatMenuModule,
    MatChipsModule,
    MatTooltipModule,
    MatSelectModule,
    MatFormFieldModule,
    MatInputModule,
    MatCardModule,
    MatDividerModule,
    MatCheckboxModule,
    TranslocoModule,
    ColumnReorder
  ],
  templateUrl: './user-management.html',
  styleUrl: './user-management.scss',
})
export class UserManagement implements OnInit {
  private adminService = inject(Admin);
  private fb = inject(FormBuilder);
  private transloco = inject(TranslocoService);

  users = signal<UserListItem[]>([]);
  totalCount = signal(0);

  private readonly tableKey = 'users';
  private readonly baseColumns = ['userName', 'email', 'superAdmin', 'status', 'createdAt'];
  columnOrder = signal<string[]>([...this.baseColumns]);
  hiddenColumns = signal<Set<string>>(new Set());
  displayedColumns = computed(() => [...this.columnOrder().filter(c => !this.hiddenColumns().has(c)), 'actions']);

  pageIndex = 0;
  pageSize = PAGINATION.defaultPageSize;
  pageSizeOptions = PAGINATION.pageSizeOptions;
  sortActive = 'userName';
  sortDirection: 'asc' | 'desc' | '' = 'asc';

  searchControl = this.fb.control('');
  statusFilterControl = this.fb.control<boolean | null>(null);

  private searchSubject = new Subject<string>();

  showCreateForm = signal(false);
  createdUserPassword = signal<string | null>(null);
  errorMessage = signal<string | null>(null);
  showColumnMenu = signal(false);

  defaultTenantMenuUser = signal<UserListItem | null>(null);
  defaultTenantMenuTenants = signal<TenantSummary[]>([]);

  createForm = this.fb.group({
    userName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    password: [''],
    makeSuperAdmin: [false]
  });

  ngOnInit(): void {
    this.restoreColumnPreferences();

    this.searchSubject.pipe(debounceTime(400)).subscribe(() => {
      this.pageIndex = 0;
      this.loadUsers();
    });

    this.searchControl.valueChanges.subscribe(val => this.searchSubject.next(val || ''));
    this.statusFilterControl.valueChanges.subscribe(() => {
      this.pageIndex = 0;
      this.loadUsers();
    });

    this.loadUsers();
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

  loadUsers(): void {
    this.adminService.getUsers({
      page: this.pageIndex + 1,
      pageSize: this.pageSize,
      sortBy: this.sortActive,
      sortDirection: this.sortDirection || 'asc',
      search: this.searchControl.value || undefined,
      statusFilter: this.statusFilterControl.value ?? undefined
    }).subscribe({
      next: (data) => {
        this.users.set(data.items);
        this.totalCount.set(data.totalCount);
      },
      error: (err) => this.errorMessage.set(err.error || this.transloco.translate('common.loadError'))
    });
  }

  onPageChange(event: PageEvent): void {
    this.pageIndex = event.pageIndex;
    this.pageSize = event.pageSize;
    this.loadUsers();
  }

  onSortChange(sort: Sort): void {
    this.sortActive = sort.active;
    this.sortDirection = sort.direction;
    this.pageIndex = 0;
    this.persistColumnPreferences();
    this.loadUsers();
  }

  toggleCreateForm(): void {
    this.showCreateForm.set(!this.showCreateForm());
    this.createdUserPassword.set(null);
    this.createForm.reset({ makeSuperAdmin: false });
  }

  createUser(): void {
    if (this.createForm.invalid) return;

    const { userName, email, password, makeSuperAdmin } = this.createForm.value;

    this.adminService.createUser({
      userName: userName!,
      email: email!,
      makeSuperAdmin: makeSuperAdmin ?? false,
      password: password || undefined
    }).subscribe({
      next: (res) => {
        this.createdUserPassword.set(res.temporaryPassword);
        this.loadUsers();
        this.createForm.reset({ makeSuperAdmin: false });
      },
      error: (err) => this.errorMessage.set(err.error || this.transloco.translate('common.saveError'))
    });
  }

  toggleSuperAdmin(user: UserListItem): void {
    if (user.roles.includes('SuperAdmin')) {
      const superAdminCount = this.users().filter(u => u.roles.includes('SuperAdmin')).length;
      if (superAdminCount <= 1) {
        this.errorMessage.set(this.transloco.translate('admin.users.cannotRemoveLastSuperAdmin'));
        return;
      }
      this.adminService.revokeSuperAdmin(user.id).subscribe({
        next: () => this.loadUsers(),
        error: (err) => this.errorMessage.set(err.error || this.transloco.translate('common.saveError'))
      });
    } else {
      this.adminService.grantSuperAdmin(user.id).subscribe({
        next: () => this.loadUsers(),
        error: (err) => this.errorMessage.set(err.error || this.transloco.translate('common.saveError'))
      });
    }
  }

  toggleActive(user: UserListItem): void {
    const action = user.isActive
      ? this.adminService.deactivateUser(user.id)
      : this.adminService.activateUser(user.id);

    action.subscribe({
      next: () => this.loadUsers(),
      error: (err) => this.errorMessage.set(err.error || this.transloco.translate('common.saveError'))
    });
  }

  resetPassword(userId: string): void {
    this.adminService.resetPassword(userId).subscribe({
      next: (res) => {
        this.createdUserPassword.set(res.temporaryPassword);
        this.loadUsers();
      },
      error: (err) => this.errorMessage.set(err.error || this.transloco.translate('common.saveError'))
    });
  }

  deleteUser(userId: string): void {
    if (!confirm(this.transloco.translate('admin.users.confirmDelete'))) return;
    this.adminService.deleteUser(userId).subscribe({
      next: () => this.loadUsers(),
      error: (err) => this.errorMessage.set(err.error || this.transloco.translate('common.deleteError'))
    });
  }

  openDefaultTenantMenu(user: UserListItem): void {
    this.defaultTenantMenuUser.set(user);
    this.adminService.getUserTenants(user.id).subscribe({
      next: (tenants) => this.defaultTenantMenuTenants.set(tenants),
      error: (err) => this.errorMessage.set(err.error || this.transloco.translate('common.loadError'))
    });
  }

  setDefaultTenant(tenantId: number | null): void {
    const user = this.defaultTenantMenuUser();
    if (!user) return;

    this.adminService.setDefaultTenant(user.id, tenantId).subscribe({
      next: () => this.loadUsers(),
      error: (err) => this.errorMessage.set(err.error || this.transloco.translate('common.saveError'))
    });
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

  exportUsers(): void {
    this.adminService.getAllUsers({
      sortBy: this.sortActive,
      sortDirection: this.sortDirection || 'asc',
      search: this.searchControl.value || undefined,
      statusFilter: this.statusFilterControl.value ?? undefined
    }).subscribe({
      next: (data) => exportToCsv('users.csv', this.getExportColumns(), data),
      error: (err) => this.errorMessage.set(err.error || this.transloco.translate('common.loadError'))
    });
  }

  private columnDefs(): Record<string, ExportColumn<UserListItem>> {
    return {
      userName: {
        key: 'userName',
        header: this.transloco.translate('admin.users.name'),
        getValue: (u) => u.userName
      },
      email: {
        key: 'email',
        header: this.transloco.translate('admin.users.email'),
        getValue: (u) => u.email
      },
      superAdmin: {
        key: 'superAdmin',
        header: this.transloco.translate('admin.users.superAdmin'),
        getValue: (u) => u.roles.includes('SuperAdmin') ? this.transloco.translate('common.yes') : this.transloco.translate('common.no')
      },
      status: {
        key: 'status',
        header: this.transloco.translate('admin.users.status'),
        getValue: (u) => u.isActive ? this.transloco.translate('common.yes') : this.transloco.translate('common.no')
      },
      createdAt: {
        key: 'createdAt',
        header: this.transloco.translate('admin.users.createdAt'),
        getValue: (u) => new Date(u.createdAt).toLocaleString()
      }
    };
  }

  private getExportColumns(): ExportColumn<UserListItem>[] {
    const defs = this.columnDefs();
    return this.displayedColumns()
      .filter(col => col !== 'actions')
      .map(col => defs[col])
      .filter((col): col is ExportColumn<UserListItem> => !!col);
  }
}