import { Service, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface PermissionClaim {
  module: string;
  action: string;
}

export interface RoleItem {
  name: string;
  claims: PermissionClaim[];
}

export interface PermissionModule {
  module: string;
  actions: string[];
}

@Service()
export class Roles {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/system/identity/roles`;

  getRoles(): Observable<RoleItem[]> {
    return this.http.get<RoleItem[]>(this.apiUrl);
  }

  getPermissionCatalog(): Observable<PermissionModule[]> {
    return this.http.get<PermissionModule[]>(`${this.apiUrl}/permissions`);
  }

  createRole(name: string): Observable<void> {
    return this.http.post<void>(this.apiUrl, { name });
  }

  deleteRole(name: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${name}`);
  }

  assignClaims(name: string, claims: PermissionClaim[]): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${name}/claims`, { claims });
  }

  restoreDefaults(): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/restore-defaults`, {});
  }
}
