import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule, DateAdapter, MAT_DATE_FORMATS } from '@angular/material/core';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog } from '@angular/material/dialog';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { forkJoin } from 'rxjs';
import { EmployeeManagement, RoleAssignmentInput } from '../../../services/employee-management';
import { AddRoleDialog, AddRoleDialogData } from '../../../components/add-role-dialog/add-role-dialog';
import { YmdDateAdapter, YMD_DATE_FORMATS } from '../../../shared/ymd-date-adapter';

type RowStatus = 'new' | 'markedForRemoval' | 'pending' | 'expired' | 'active';

interface TableRoleRow {
  roleName: string;
  startsAt: Date | null;
  expiresAt: Date | null;
  isPending: boolean;
  isExpired: boolean;
  createdAt: Date | null;
  createdByName: string;
  updatedAt: Date | null;
  updatedByName: string;
  markedForRemoval: boolean;
  isNew: boolean;
}

@Component({
  selector: 'app-employee-role-assignment',
  imports: [
    CommonModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatFormFieldModule,
    MatInputModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatProgressBarModule,
    MatTooltipModule,
    TranslocoModule,
    RouterLink
  ],
  providers: [
    { provide: DateAdapter, useClass: YmdDateAdapter },
    { provide: MAT_DATE_FORMATS, useValue: YMD_DATE_FORMATS },
  ],
  templateUrl: './employee-role-assignment.html',
  styleUrl: './employee-role-assignment.scss',
})
export class EmployeeRoleAssignment implements OnInit {
  private employeeService = inject(EmployeeManagement);
  private transloco = inject(TranslocoService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private dialog = inject(MatDialog);

  private employeeId = 0;
  employeeName = signal('');

  loading = signal(true);
  saving = signal(false);
  errorMessage = signal<string | null>(null);

  rows = signal<TableRoleRow[]>([]);
  availableRoles = signal<string[]>([]);
  displayedColumns = ['roleName', 'startsAt', 'expiresAt', 'createdAt', 'createdBy', 'updatedAt', 'updatedBy', 'status', 'actions'];

  ngOnInit(): void {
    this.employeeId = Number(this.route.snapshot.paramMap.get('id'));

    forkJoin({
      employee: this.employeeService.getEmployee(this.employeeId),
      assignments: this.employeeService.getRoleAssignments(this.employeeId),
      assignableRoles: this.employeeService.getAssignableRoles()
    }).subscribe({
      next: ({ employee, assignments, assignableRoles }) => {
        this.employeeName.set(employee.userName);

        const rows: TableRoleRow[] = assignments.map(a => ({
          roleName: a.roleName,
          startsAt: a.startsAt ? new Date(a.startsAt) : null,
          expiresAt: a.expiresAt ? new Date(a.expiresAt) : null,
          isPending: a.isPending,
          isExpired: a.isExpired,
          createdAt: new Date(a.createdAt),
          createdByName: a.createdByName,
          updatedAt: new Date(a.updatedAt),
          updatedByName: a.updatedByName,
          markedForRemoval: false,
          isNew: false
        }));
        this.rows.set(rows);

        const heldNames = new Set(rows.map(r => r.roleName));
        this.availableRoles.set(assignableRoles.filter(r => !heldNames.has(r)).sort());

        this.loading.set(false);
      },
      error: (err) => {
        this.errorMessage.set(err.error || this.transloco.translate('common.loadError'));
        this.loading.set(false);
      }
    });
  }

  rowStatus(row: TableRoleRow): RowStatus {
    if (row.markedForRemoval) return 'markedForRemoval';
    if (row.isNew) return 'new';
    if (row.isPending) return 'pending';
    if (row.isExpired) return 'expired';
    return 'active';
  }

  openAddRole(): void {
    const dialogRef = this.dialog.open<AddRoleDialog, AddRoleDialogData, string[] | null>(AddRoleDialog, {
      data: { availableRoles: this.availableRoles() }
    });

    dialogRef.afterClosed().subscribe((selected) => {
      if (!selected || selected.length === 0) return;

      const newRows: TableRoleRow[] = selected.map(roleName => ({
        roleName,
        startsAt: null,
        expiresAt: null,
        isPending: false,
        isExpired: false,
        createdAt: null,
        createdByName: '',
        updatedAt: null,
        updatedByName: '',
        markedForRemoval: false,
        isNew: true
      }));

      this.rows.set([...this.rows(), ...newRows]);
      this.availableRoles.set(this.availableRoles().filter(r => !selected.includes(r)));
    });
  }

  toggleRemove(row: TableRoleRow): void {
    if (row.isNew) {
      this.rows.set(this.rows().filter(r => r.roleName !== row.roleName));
      this.availableRoles.set([...this.availableRoles(), row.roleName].sort());
      return;
    }
    this.rows.set(this.rows().map(r =>
      r.roleName === row.roleName ? { ...r, markedForRemoval: !r.markedForRemoval } : r
    ));
  }

  setStartsAt(row: TableRoleRow, date: Date | null): void {
    this.rows.set(this.rows().map(r =>
      r.roleName === row.roleName
        ? { ...r, startsAt: date, isPending: !!date && date.getTime() > Date.now() }
        : r
    ));
  }

  setExpiresAt(row: TableRoleRow, date: Date | null): void {
    this.rows.set(this.rows().map(r =>
      r.roleName === row.roleName
        ? { ...r, expiresAt: date, isExpired: !!date && date.getTime() <= Date.now() }
        : r
    ));
  }

  save(): void {
    this.saving.set(true);
    const roles: RoleAssignmentInput[] = this.rows()
      .filter(r => !r.markedForRemoval)
      .map(r => ({
        roleName: r.roleName,
        startsAt: r.startsAt ? r.startsAt.toISOString() : null,
        expiresAt: r.expiresAt ? r.expiresAt.toISOString() : null
      }));

    this.employeeService.assignRoles(this.employeeId, roles).subscribe({
      next: () => this.router.navigate(['/admin/organization/employees']),
      error: (err) => {
        this.errorMessage.set(err.error || this.transloco.translate('common.saveError'));
        this.saving.set(false);
      }
    });
  }

}
