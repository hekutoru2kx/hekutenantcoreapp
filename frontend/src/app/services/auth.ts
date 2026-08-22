import { Service, effect, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { jwtDecode } from 'jwt-decode';
import { environment } from '../../environments/environment';
import { Theme } from './theme';

export interface TenantSummary {
  id: number;
  name: string;
}

export interface AuthResponse {
  token: string;
  email: string;
  userName: string;
  mustChangePassword: boolean;
  preferredTheme: string;
  tenantId: number | null;
  tenantName: string | null;
  availableTenants: TenantSummary[];
  multiTenantDisabled: boolean;
}

export interface CurrentUser {
  email: string;
  userName: string;
  mustChangePassword: boolean;
  preferredTheme: string;
  tenantId: number | null;
  tenantName: string | null;
  multiTenantDisabled: boolean;
}

export interface RegistrationTenantInfo {
  tenantSelectionEnabled: boolean;
  defaultTenantId: number | null;
  defaultTenantName: string | null;
}

interface DecodedToken {
  'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'?: string | string[];
  tenant_id?: string;
  [key: string]: unknown;
}

const ROLE_CLAIM = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';
const TOKEN_KEY = 'hekutenantcoreapp_token';
const USER_KEY = 'hekutenantcoreapp_user';
const RESERVED_CLAIM_KEYS = new Set(['sub', 'email', 'userName', 'exp', 'iss', 'aud', 'nbf', 'iat', ROLE_CLAIM, 'tenant_id']);

export interface PermissionClaim {
  module: string;
  action: string;
}

@Service()
export class Auth {
  private http = inject(HttpClient);
  private theme = inject(Theme);
  private apiUrl = `${environment.apiUrl}/auth`;

  token = signal<string | null>(localStorage.getItem(TOKEN_KEY));
  currentUser = signal<CurrentUser | null>(this.loadUser());
  availableTenants = signal<TenantSummary[]>([]);

  private applyStoredTheme = effect(() => {
    const user = this.currentUser();
    if (user?.preferredTheme) {
      this.theme.setColorTheme(user.preferredTheme as any);
    }
  });

  login(email: string, password: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/login`, { email, password })
      .pipe(tap(res => this.setSession(res)));
  }

  register(userName: string, email: string, password: string, tenantId: number): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/register`, { userName, email, password, tenantId })
      .pipe(tap(res => this.setSession(res)));
  }

  loginWithGoogle(idToken: string, tenantId?: number): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/google`, { idToken, tenantId })
      .pipe(tap(res => this.setSession(res)));
  }

  selectTenant(tenantId: number): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/select-tenant`, { tenantId })
      .pipe(tap(res => this.setSession(res)));
  }

  joinTenant(tenantId: number): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/join-tenant`, { tenantId })
      .pipe(tap(res => this.setSession(res)));
  }

  getAvailableTenants(): Observable<TenantSummary[]> {
    return this.http.get<TenantSummary[]>(`${this.apiUrl}/available-tenants`);
  }

  // Pre-auth lookup for the Register page — whether it should show a tenant dropdown at all.
  getRegistrationTenantInfo(): Observable<RegistrationTenantInfo> {
    return this.http.get<RegistrationTenantInfo>(`${this.apiUrl}/registration-tenant-info`);
  }

  logout(): void {
    this.token.set(null);
    this.currentUser.set(null);
    this.availableTenants.set([]);
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
  }

  isLoggedIn(): boolean {
    return this.token() !== null;
  }

  getRoles(): string[] {
    const currentToken = this.token();
    if (!currentToken) return [];

    try {
      const decoded = jwtDecode<DecodedToken>(currentToken);
      const role = decoded[ROLE_CLAIM];
      if (!role) return [];
      return Array.isArray(role) ? role : [role];
    } catch {
      return [];
    }
  }

  hasRole(role: string): boolean {
    return this.getRoles().includes(role);
  }

  getClaims(): PermissionClaim[] {
    const currentToken = this.token();
    if (!currentToken) return [];

    try {
      const decoded = jwtDecode<DecodedToken>(currentToken);
      const claims: PermissionClaim[] = [];

      for (const [key, value] of Object.entries(decoded)) {
        if (RESERVED_CLAIM_KEYS.has(key)) continue;
        const actions = Array.isArray(value) ? value : [value];
        for (const action of actions) {
          if (typeof action === 'string') claims.push({ module: key, action });
        }
      }

      return claims;
    } catch {
      return [];
    }
  }

  hasClaim(module: string, action: string): boolean {
    return this.getClaims().some(c => c.module === module && c.action === action);
  }

  private setSession(res: AuthResponse): void {
    this.token.set(res.token);
    this.currentUser.set({
      email: res.email,
      userName: res.userName,
      mustChangePassword: res.mustChangePassword,
      preferredTheme: res.preferredTheme,
      tenantId: res.tenantId,
      tenantName: res.tenantName,
      multiTenantDisabled: res.multiTenantDisabled
    });
    this.availableTenants.set(res.availableTenants ?? []);
    localStorage.setItem(TOKEN_KEY, res.token);
    localStorage.setItem(USER_KEY, JSON.stringify({
      email: res.email,
      userName: res.userName,
      mustChangePassword: res.mustChangePassword,
      preferredTheme: res.preferredTheme,
      tenantId: res.tenantId,
      tenantName: res.tenantName,
      multiTenantDisabled: res.multiTenantDisabled
    }));
  }

  private loadUser(): CurrentUser | null {
    const stored = localStorage.getItem(USER_KEY);
    return stored ? JSON.parse(stored) : null;
  }
}
