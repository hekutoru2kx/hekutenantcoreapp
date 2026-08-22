import { Service, inject, signal } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { TenantSummary } from './auth';

export interface UserListItem {
  id: string;
  userName: string;
  email: string;
  roles: string[];
  isActive: boolean;
  mustChangePassword: boolean;
  createdAt: string;
  defaultTenantId: number | null;
}

export interface PagedUserResult {
  items: UserListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface UserListQueryParams {
  page: number;
  pageSize: number;
  sortBy?: string;
  sortDirection?: string;
  search?: string;
  roleFilter?: string;
  statusFilter?: boolean;
}

export interface UserExportQueryParams {
  sortBy?: string;
  sortDirection?: string;
  search?: string;
  roleFilter?: string;
  statusFilter?: boolean;
}

export interface CreateUserRequest {
  userName: string;
  email: string;
  password?: string;
  makeSuperAdmin?: boolean;
}

export interface CreateUserResponse {
  userId: string;
  email: string;
  userName: string;
  temporaryPassword: string;
}

@Service()
export class Admin {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/system/identity`;

  getUsers(query: UserListQueryParams): Observable<PagedUserResult> {
    let params = new HttpParams()
      .set('page', query.page)
      .set('pageSize', query.pageSize);

    if (query.sortBy) params = params.set('sortBy', query.sortBy);
    if (query.sortDirection) params = params.set('sortDirection', query.sortDirection);
    if (query.search) params = params.set('search', query.search);
    if (query.roleFilter) params = params.set('roleFilter', query.roleFilter);
    if (query.statusFilter !== undefined && query.statusFilter !== null) {
      params = params.set('statusFilter', query.statusFilter);
    }

    return this.http.get<PagedUserResult>(`${this.apiUrl}/users`, { params });
  }

  getAllUsers(query: UserExportQueryParams): Observable<UserListItem[]> {
    let params = new HttpParams();
    if (query.sortBy) params = params.set('sortBy', query.sortBy);
    if (query.sortDirection) params = params.set('sortDirection', query.sortDirection);
    if (query.search) params = params.set('search', query.search);
    if (query.roleFilter) params = params.set('roleFilter', query.roleFilter);
    if (query.statusFilter !== undefined && query.statusFilter !== null) {
      params = params.set('statusFilter', query.statusFilter);
    }

    return this.http.get<UserListItem[]>(`${this.apiUrl}/users/export`, { params });
  }

  createUser(data: CreateUserRequest): Observable<CreateUserResponse> {
    return this.http.post<CreateUserResponse>(`${this.apiUrl}/users`, {
      userName: data.userName,
      email: data.email,
      password: data.password,
      role: data.makeSuperAdmin ? 'SuperAdmin' : undefined
    });
  }

  grantSuperAdmin(userId: string): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/users/${userId}/grant-superadmin`, {});
  }

  revokeSuperAdmin(userId: string): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/users/${userId}/revoke-superadmin`, {});
  }

  deactivateUser(userId: string): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/users/${userId}/deactivate`, {});
  }

  activateUser(userId: string): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/users/${userId}/activate`, {});
  }

  resetPassword(userId: string, newPassword?: string): Observable<{ temporaryPassword: string }> {
    return this.http.put<{ temporaryPassword: string }>(`${this.apiUrl}/users/${userId}/reset-password`, { newPassword });
  }

  deleteUser(userId: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/users/${userId}`);
  }

  getUserTenants(userId: string): Observable<TenantSummary[]> {
    return this.http.get<TenantSummary[]>(`${this.apiUrl}/users/${userId}/tenants`);
  }

  setDefaultTenant(userId: string, tenantId: number | null): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/users/${userId}/default-tenant`, { tenantId });
  }
}