import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule } from '@angular/material/paginator';
import { MatSortModule } from '@angular/material/sort';
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
import { YmdDateAdapter, YMD_DATE_FORMATS } from '../../../shared/ymd-date-adapter';
import { ColumnReorder } from '../../../components/column-reorder/column-reorder';
import { DataTableController } from '../../../shared/data-table-controller';

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

  table = new DataTableController<EmployeeItem>({
    tableKey: 'employees',
    defaultSort: { active: 'jobTitle', direction: 'asc' },
    persistSort: true,
    onChange: () => this.loadEmployees(),
    columns: [
      { key: 'userName', header: () => this.transloco.translate('admin.users.name'), exportValue: (e) => e.userName },
      { key: 'email', header: () => this.transloco.translate('admin.users.email'), exportValue: (e) => e.email },
      { key: 'jobTitle', header: () => this.transloco.translate('admin.employees.jobTitle'), sortable: true, exportValue: (e) => e.jobTitle || '' },
      { key: 'roles', header: () => this.transloco.translate('admin.users.roles'), exportValue: (e) => e.roles.join('; ') },
      {
        key: 'isActive',
        header: () => this.transloco.translate('admin.users.status'),
        exportValue: (e) => e.isActive ? this.transloco.translate('common.yes') : this.transloco.translate('common.no'),
      },
    ],
  });

  searchControl = this.fb.control('');
  private searchSubject = new Subject<string>();

  showInviteForm = signal(false);
  createdPassword = signal<string | null>(null);
  errorMessage = signal<string | null>(null);

  inviteForm = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    userName: [''],
    roleName: ['', Validators.required],
    roleExpiresAt: [null as Date | null],
    jobTitle: ['']
  });

  ngOnInit(): void {
    this.table.restore();

    this.searchSubject.pipe(debounceTime(400)).subscribe(() => {
      this.table.pageIndex = 0;
      this.loadEmployees();
    });

    this.searchControl.valueChanges.subscribe(val => this.searchSubject.next(val || ''));
    this.loadEmployees();
    this.loadAvailableRoles();
  }

  loadEmployees(): void {
    this.loading.set(true);
    this.employeeService.getEmployees({
      page: this.table.page,
      pageSize: this.table.pageSize,
      sortBy: this.table.sortActive,
      sortDirection: this.table.sortDirection || 'asc',
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

  exportEmployees(): void {
    this.employeeService.getAllEmployees({
      sortBy: this.table.sortActive,
      sortDirection: this.table.sortDirection || 'asc',
      search: this.searchControl.value || undefined
    }).subscribe({
      next: (data) => this.table.exportRows('employees.csv', data),
      error: (err) => this.errorMessage.set(err.error || this.transloco.translate('common.loadError'))
    });
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
}
