import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
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
import { exportToCsv, ExportColumn } from '../../../shared/csv-export';
import { loadColumnPreferences, saveColumnPreferences } from '../../../shared/column-preferences';

@Component({
  selector: 'app-role-management',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatTableModule,
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

  roles = signal<RoleItem[]>([]);
  catalog = signal<PermissionModule[]>([]);

  private readonly tableKey = 'roles';
  private readonly baseColumns = ['name', 'claims'];
  columnOrder = signal<string[]>([...this.baseColumns]);
  hiddenColumns = signal<Set<string>>(new Set());
  displayedColumns = computed(() => [...this.columnOrder().filter(c => !this.hiddenColumns().has(c)), 'actions']);

  showCreateForm = signal(false);
  errorMessage = signal<string | null>(null);
  showColumnMenu = signal(false);

  editingRole = signal<string | null>(null);
  editingClaims = signal<Set<string>>(new Set());

  createForm = this.fb.group({
    name: ['', Validators.required]
  });

  ngOnInit(): void {
    this.restoreColumnPreferences();
    this.loadRoles();
    this.loadCatalog();
  }

  private restoreColumnPreferences(): void {
    const saved = loadColumnPreferences(this.tableKey);
    if (!saved) return;

    const validOrder = saved.order.filter(c => this.baseColumns.includes(c));
    const missing = this.baseColumns.filter(c => !validOrder.includes(c));
    this.columnOrder.set([...validOrder, ...missing]);
    this.hiddenColumns.set(new Set(saved.hidden.filter(c => this.baseColumns.includes(c))));
  }

  private persistColumnPreferences(): void {
    saveColumnPreferences(this.tableKey, {
      order: this.columnOrder(),
      hidden: Array.from(this.hiddenColumns())
    });
  }

  loadRoles(): void {
    this.rolesService.getRoles().subscribe({
      next: (data) => this.roles.set(data),
      error: (err) => this.errorMessage.set(err.error || this.transloco.translate('common.loadError'))
    });
  }

  loadCatalog(): void {
    this.rolesService.getPermissionCatalog().subscribe({
      next: (data) => this.catalog.set(data),
      error: (err) => this.errorMessage.set(err.error || this.transloco.translate('common.loadError'))
    });
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

  exportRoles(): void {
    exportToCsv('roles.csv', this.getExportColumns(), this.roles());
  }

  private columnDefs(): Record<string, ExportColumn<RoleItem>> {
    return {
      name: {
        key: 'name',
        header: this.transloco.translate('admin.roles.roleName'),
        getValue: (r) => r.name
      },
      claims: {
        key: 'claims',
        header: this.transloco.translate('admin.roles.claims'),
        getValue: (r) => r.claims.map(c => `${c.module}.${c.action}`).join('; ')
      }
    };
  }

  private getExportColumns(): ExportColumn<RoleItem>[] {
    const defs = this.columnDefs();
    return this.displayedColumns()
      .filter(col => col !== 'actions')
      .map(col => defs[col])
      .filter((col): col is ExportColumn<RoleItem> => !!col);
  }
}
