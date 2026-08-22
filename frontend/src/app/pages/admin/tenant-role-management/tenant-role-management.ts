import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatChipsModule } from '@angular/material/chips';
import { MatCardModule } from '@angular/material/card';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { TenantRoles, TenantRoleItem } from '../../../services/tenant-role-management';

@Component({
  selector: 'app-tenant-role-management',
  imports: [
    CommonModule,
    MatTableModule,
    MatChipsModule,
    MatCardModule,
    TranslocoModule
  ],
  templateUrl: './tenant-role-management.html',
  styleUrl: './tenant-role-management.scss',
})
export class TenantRoleManagement implements OnInit {
  private rolesService = inject(TenantRoles);
  private transloco = inject(TranslocoService);

  roles = signal<TenantRoleItem[]>([]);
  displayedColumns = ['name', 'claims'];

  errorMessage = signal<string | null>(null);

  ngOnInit(): void {
    this.loadRoles();
  }

  loadRoles(): void {
    this.rolesService.getRoles().subscribe({
      next: (data) => this.roles.set(data),
      error: (err) => this.errorMessage.set(err.error || this.transloco.translate('common.loadError'))
    });
  }
}
