import { Component, effect, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink, RouterOutlet } from '@angular/router';
import { NavBar } from './components/nav-bar/nav-bar';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { TranslocoModule } from '@jsverse/transloco';
import { Auth } from './services/auth';
import { filter, map } from 'rxjs';

const TENANT_EXEMPT_ROUTES = ['/tenant-picker', '/login', '/register', '/'];

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, NavBar, MatIconModule, MatButtonModule, TranslocoModule],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  auth = inject(Auth);
  router = inject(Router);

  private currentUrl = toSignal(
    this.router.events.pipe(
      filter((e): e is NavigationEnd => e instanceof NavigationEnd),
      map(e => e.urlAfterRedirects)
    ),
    { initialValue: this.router.url }
  );

  // Defense-in-depth backstop: any authenticated user with no tenant selected gets
  // redirected to the picker, even on a route that forgot to apply tenantGuard.
  private redirectToTenantPicker = effect(() => {
    const user = this.auth.currentUser();
    const url = this.currentUrl();
    if (user && user.tenantId == null && !TENANT_EXEMPT_ROUTES.includes(url)) {
      this.router.navigate(['/tenant-picker']);
    }
  });

}