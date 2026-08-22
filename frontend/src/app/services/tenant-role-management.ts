import { Service, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { PermissionClaim } from './role-management';

export interface TenantRoleItem {
  name: string;
  claims: PermissionClaim[];
}

@Service()
export class TenantRoles {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/admin/identity/roles`;

  getRoles(): Observable<TenantRoleItem[]> {
    return this.http.get<TenantRoleItem[]>(this.apiUrl);
  }
}
