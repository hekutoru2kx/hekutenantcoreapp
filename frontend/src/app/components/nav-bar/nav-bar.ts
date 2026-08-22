import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { Theme } from '../../services/theme';
import { Auth, TenantSummary } from '../../services/auth';
import { MatDividerModule } from '@angular/material/divider';

@Component({
  selector: 'app-nav-bar',
  imports: [MatToolbarModule, MatButtonModule, MatIconModule, MatMenuModule, MatDividerModule, TranslocoModule, RouterLink],
  templateUrl: './nav-bar.html',
  styleUrl: './nav-bar.scss',
})
export class NavBar {
  theme = inject(Theme);
  auth = inject(Auth);
  private transloco = inject(TranslocoService);
  private router = inject(Router);

  switcherTenants = signal<TenantSummary[]>([]);

  get currentLang(): string {
    return this.transloco.getActiveLang().toUpperCase();
  }

  toggleLanguage(): void {
    const next = this.transloco.getActiveLang() === 'es' ? 'en' : 'es';
    this.transloco.setActiveLang(next);
  }

  logout(): void {
    this.auth.logout();
    this.router.navigate(['/']);
  }

  loadSwitcherTenants(): void {
    this.auth.getAvailableTenants().subscribe({
      next: (tenants) => this.switcherTenants.set(tenants)
    });
  }

  // Switching tenants is treated as a mini re-login: a fresh token is minted for the
  // selected tenant, then the app is fully reloaded so no component holds onto stale,
  // previous-tenant data.
  switchTenant(tenantId: number): void {
    this.auth.selectTenant(tenantId).subscribe({
      next: () => window.location.assign('/')
    });
  }
}
