import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule } from '@angular/material/paginator';
import { MatSortModule } from '@angular/material/sort';
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
import { ColumnReorder } from '../../../components/column-reorder/column-reorder';
import { DataTableController } from '../../../shared/data-table-controller';

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

  table = new DataTableController<UserListItem>({
    tableKey: 'users',
    defaultSort: { active: 'userName', direction: 'asc' },
    persistSort: true,
    onChange: () => this.loadUsers(),
    columns: [
      { key: 'userName', header: () => this.transloco.translate('admin.users.name'), sortable: true, exportValue: (u) => u.userName },
      { key: 'email', header: () => this.transloco.translate('admin.users.email'), sortable: true, exportValue: (u) => u.email },
      {
        key: 'superAdmin',
        header: () => this.transloco.translate('admin.users.superAdmin'),
        exportValue: (u) => u.roles.includes('SuperAdmin')
          ? this.transloco.translate('common.yes')
          : this.transloco.translate('common.no'),
      },
      {
        key: 'status',
        header: () => this.transloco.translate('admin.users.status'),
        exportValue: (u) => u.isActive
          ? this.transloco.translate('common.yes')
          : this.transloco.translate('common.no'),
      },
      { key: 'createdAt', header: () => this.transloco.translate('admin.users.createdAt'), sortable: true, exportValue: (u) => new Date(u.createdAt).toLocaleString() },
    ],
  });

  searchControl = this.fb.control('');
  statusFilterControl = this.fb.control<boolean | null>(null);

  private searchSubject = new Subject<string>();

  showCreateForm = signal(false);
  createdUserPassword = signal<string | null>(null);
  errorMessage = signal<string | null>(null);

  defaultTenantMenuUser = signal<UserListItem | null>(null);
  defaultTenantMenuTenants = signal<TenantSummary[]>([]);

  createForm = this.fb.group({
    userName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    password: [''],
    makeSuperAdmin: [false]
  });

  ngOnInit(): void {
    this.table.restore();

    this.searchSubject.pipe(debounceTime(400)).subscribe(() => {
      this.table.pageIndex = 0;
      this.loadUsers();
    });

    this.searchControl.valueChanges.subscribe(val => this.searchSubject.next(val || ''));
    this.statusFilterControl.valueChanges.subscribe(() => {
      this.table.pageIndex = 0;
      this.loadUsers();
    });

    this.loadUsers();
  }

  loadUsers(): void {
    this.adminService.getUsers({
      page: this.table.page,
      pageSize: this.table.pageSize,
      sortBy: this.table.sortActive,
      sortDirection: this.table.sortDirection || 'asc',
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

  exportUsers(): void {
    this.adminService.getAllUsers({
      sortBy: this.table.sortActive,
      sortDirection: this.table.sortDirection || 'asc',
      search: this.searchControl.value || undefined,
      statusFilter: this.statusFilterControl.value ?? undefined
    }).subscribe({
      next: (data) => this.table.exportRows('users.csv', data),
      error: (err) => this.errorMessage.set(err.error || this.transloco.translate('common.loadError'))
    });
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
}
