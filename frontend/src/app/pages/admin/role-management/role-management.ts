import { Component, inject, signal, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatSortModule, MatSort } from '@angular/material/sort';
import { MatPaginatorModule, MatPaginator } from '@angular/material/paginator';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatDialog } from '@angular/material/dialog';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { Roles, RoleItem, PermissionModule, PermissionClaim } from '../../../services/role-management';
import { ConfirmDialog } from '../../../components/confirm-dialog/confirm-dialog';
import { ColumnReorder } from '../../../components/column-reorder/column-reorder';
import { DataTableController } from '../../../shared/data-table-controller';

@Component({
  selector: 'app-role-management',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatTableModule,
    MatSortModule,
    MatPaginatorModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatTooltipModule,
    MatFormFieldModule,
    MatInputModule,
    MatCardModule,
    MatCheckboxModule,
    TranslocoModule,
    ColumnReorder
  ],
  templateUrl: './role-management.html',
  styleUrl: './role-management.scss',
})
export class RoleManagement implements OnInit {
  private rolesService = inject(Roles);
  private fb = inject(FormBuilder);
  private transloco = inject(TranslocoService);
  private dialog = inject(MatDialog);

  catalog = signal<PermissionModule[]>([]);

  // Client mode: the full role list is small, so sort + paginate happen in the browser.
  table = new DataTableController<RoleItem>({
    tableKey: 'roles',
    mode: 'client',
    defaultSort: { active: 'name', direction: 'asc' },
    columns: [
      {
        key: 'name',
        header: () => this.transloco.translate('admin.roles.roleName'),
        sortable: true,
        sortValue: (r) => r.name,
        exportValue: (r) => r.name,
      },
      {
        key: 'claims',
        header: () => this.transloco.translate('admin.roles.claims'),
        sortable: true,
        sortValue: (r) => r.claims.length, // sort by how many permissions the role carries
        exportValue: (r) => r.claims.map((c) => `${c.module}.${c.action}`).join('; '),
      },
    ],
  });

  @ViewChild(MatSort) set sort(s: MatSort) { if (s) this.table.attachSort(s); }
  @ViewChild(MatPaginator) set paginator(p: MatPaginator) { if (p) this.table.attachPaginator(p); }

  showCreateForm = signal(false);
  errorMessage = signal<string | null>(null);

  editingRole = signal<string | null>(null);
  editingClaims = signal<Set<string>>(new Set());

  createForm = this.fb.group({
    name: ['', Validators.required]
  });

  ngOnInit(): void {
    this.table.restore();
    this.loadRoles();
    this.loadCatalog();
  }

  loadRoles(): void {
    this.rolesService.getRoles().subscribe({
      next: (data) => this.table.setData(data),
      error: (err) => this.errorMessage.set(err.error || this.transloco.translate('common.loadError'))
    });
  }

  loadCatalog(): void {
    this.rolesService.getPermissionCatalog().subscribe({
      next: (data) => this.catalog.set(data),
      error: (err) => this.errorMessage.set(err.error || this.transloco.translate('common.loadError'))
    });
  }

  exportRoles(): void {
    this.table.exportRows('roles.csv');
  }

  toggleCreateForm(): void {
    this.showCreateForm.set(!this.showCreateForm());
    this.createForm.reset();
  }

  restoreDefaults(): void {
    this.rolesService.restoreDefaults().subscribe({
      next: () => this.loadRoles(),
      error: (err) => this.errorMessage.set(err.error || this.transloco.translate('common.saveError'))
    });
  }

  createRole(): void {
    if (this.createForm.invalid) return;
    const name = this.createForm.value.name!;

    this.rolesService.createRole(name).subscribe({
      next: () => {
        this.loadRoles();
        this.showCreateForm.set(false);
        this.createForm.reset();
      },
      error: (err) => this.errorMessage.set(err.error || this.transloco.translate('common.saveError'))
    });
  }

  deleteRole(name: string): void {
    const dialogRef = this.dialog.open(ConfirmDialog, {
      data: {
        title: this.transloco.translate('admin.roles.deleteRoleTitle'),
        message: this.transloco.translate('admin.roles.confirmDelete', { name }),
        confirmLabel: this.transloco.translate('admin.roles.delete'),
        cancelLabel: this.transloco.translate('admin.roles.cancel')
      }
    });

    dialogRef.afterClosed().subscribe((confirmed) => {
      if (!confirmed) return;

      this.rolesService.deleteRole(name).subscribe({
        next: () => this.loadRoles(),
        error: (err) => this.errorMessage.set(err.error || this.transloco.translate('common.deleteError'))
      });
    });
  }

  openEditClaims(role: RoleItem): void {
    this.editingRole.set(role.name);
    this.editingClaims.set(new Set(role.claims.map(c => this.claimKey(c.module, c.action))));
  }

  closeEditClaims(): void {
    this.editingRole.set(null);
  }

  claimKey(module: string, action: string): string {
    return `${module}::${action}`;
  }

  isClaimChecked(module: string, action: string): boolean {
    return this.editingClaims().has(this.claimKey(module, action));
  }

  toggleClaim(module: string, action: string): void {
    const key = this.claimKey(module, action);
    const updated = new Set(this.editingClaims());
    if (updated.has(key)) updated.delete(key);
    else updated.add(key);
    this.editingClaims.set(updated);
  }

  saveClaims(): void {
    const roleName = this.editingRole();
    if (!roleName) return;

    const claims: PermissionClaim[] = Array.from(this.editingClaims()).map(key => {
      const [module, action] = key.split('::');
      return { module, action };
    });

    this.rolesService.assignClaims(roleName, claims).subscribe({
      next: () => {
        this.loadRoles();
        this.editingRole.set(null);
      },
      error: (err) => this.errorMessage.set(err.error || this.transloco.translate('common.saveError'))
    });
  }
}
