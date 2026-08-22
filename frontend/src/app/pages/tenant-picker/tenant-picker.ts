import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { ReactiveFormsModule, FormBuilder } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatListModule } from '@angular/material/list';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { debounceTime, distinctUntilChanged, switchMap, of } from 'rxjs';
import { Auth, TenantSummary } from '../../services/auth';
import { TenantManagement } from '../../services/tenant-management';

const MIN_SEARCH_LENGTH = 2;

@Component({
  selector: 'app-tenant-picker',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatListModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
    MatProgressBarModule,
    TranslocoModule
  ],
  templateUrl: './tenant-picker.html',
  styleUrl: './tenant-picker.scss',
})
export class TenantPicker implements OnInit {
  private auth = inject(Auth);
  private tenantManagement = inject(TenantManagement);
  private router = inject(Router);
  private transloco = inject(TranslocoService);
  private fb = inject(FormBuilder);

  readonly minSearchLength = MIN_SEARCH_LENGTH;

  myTenants = signal<TenantSummary[]>([]);
  joinableTenants = signal<TenantSummary[]>([]);
  showJoin = signal(false);
  searching = signal(false);
  loading = signal(true);
  errorMessage = signal<string | null>(null);

  searchControl = this.fb.control('');

  ngOnInit(): void {
    this.auth.getAvailableTenants().subscribe({
      next: (tenants) => {
        this.myTenants.set(tenants);
        this.loading.set(false);
        if (tenants.length === 0) this.showJoin.set(true);
      },
      error: (err) => {
        this.loading.set(false);
        this.errorMessage.set(err.error || this.transloco.translate('common.loadError'));
      }
    });

    // Search-to-reveal rather than loading every active tenant up front — nothing is
    // returned below the minimum length, so a tenant is only found by (partly) knowing its name.
    this.searchControl.valueChanges.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      switchMap((term) => {
        const query = (term || '').trim();
        if (query.length < MIN_SEARCH_LENGTH) {
          this.joinableTenants.set([]);
          this.searching.set(false);
          return of(null);
        }
        this.searching.set(true);
        return this.tenantManagement.getActiveTenants(query);
      })
    ).subscribe({
      next: (tenants) => {
        this.searching.set(false);
        if (tenants) this.joinableTenants.set(tenants);
      },
      error: (err) => {
        this.searching.set(false);
        this.errorMessage.set(err.error || this.transloco.translate('common.loadError'));
      }
    });
  }

  toggleJoin(): void {
    this.showJoin.set(!this.showJoin());
  }

  select(tenantId: number): void {
    this.auth.selectTenant(tenantId).subscribe({
      next: () => this.router.navigate(['/']),
      error: (err) => this.errorMessage.set(err.error || this.transloco.translate('common.saveError'))
    });
  }

  join(tenantId: number): void {
    this.auth.joinTenant(tenantId).subscribe({
      next: () => this.router.navigate(['/']),
      error: (err) => this.errorMessage.set(err.error || this.transloco.translate('common.saveError'))
    });
  }
}
