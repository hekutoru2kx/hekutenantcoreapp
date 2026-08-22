import { Service, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface EmployeeItem {
  id: number;
  userId: string;
  userName: string;
  email: string;
  jobTitle?: string;
  hireDate?: string;
  isActive: boolean;
  roles: string[];
}

export interface PagedEmployeeResult {
  items: EmployeeItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface EmployeeListQueryParams {
  page: number;
  pageSize: number;
  sortBy?: string;
  sortDirection?: string;
  search?: string;
}

export interface EmployeeExportQueryParams {
  sortBy?: string;
  sortDirection?: string;
  search?: string;
}

export interface InviteEmployeeRequest {
  email: string;
  userName?: string;
  roleName: string;
  roleExpiresAt?: Date | string | null;
  jobTitle?: string;
  hireDate?: Date | string | null;
}

export interface UpsertEmployeeRequest {
  jobTitle?: string;
  hireDate?: Date | string | null;
  isActive: boolean;
}

export interface RoleAssignment {
  roleName: string;
  startsAt?: string | null;
  expiresAt?: string | null;
  isPending: boolean;
  isExpired: boolean;
  createdAt: string;
  createdByName: string;
  updatedAt: string;
  updatedByName: string;
}

export interface RoleAssignmentInput {
  roleName: string;
  startsAt?: string | null;
  expiresAt?: string | null;
}

@Service()
export class EmployeeManagement {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/admin/organization/employees`;
  // Role assignment is an Identity-context concern, kept as a separate base since the API
  // route for it lives under /admin/identity/employees rather than this controller's own
  // /admin/organization/employees prefix.
  private identityApiUrl = `${environment.apiUrl}/admin/identity/employees`;

  getAssignableRoles(): Observable<string[]> {
    return this.http.get<string[]>(`${this.apiUrl}/assignable-roles`);
  }

  getEmployee(id: number): Observable<EmployeeItem> {
    return this.http.get<EmployeeItem>(`${this.apiUrl}/${id}`);
  }

  getEmployees(query: EmployeeListQueryParams): Observable<PagedEmployeeResult> {
    let params = new HttpParams()
      .set('page', query.page)
      .set('pageSize', query.pageSize);

    if (query.sortBy) params = params.set('sortBy', query.sortBy);
    if (query.sortDirection) params = params.set('sortDirection', query.sortDirection);
    if (query.search) params = params.set('search', query.search);

    return this.http.get<PagedEmployeeResult>(this.apiUrl, { params });
  }

  getAllEmployees(query: EmployeeExportQueryParams): Observable<EmployeeItem[]> {
    let params = new HttpParams();
    if (query.sortBy) params = params.set('sortBy', query.sortBy);
    if (query.sortDirection) params = params.set('sortDirection', query.sortDirection);
    if (query.search) params = params.set('search', query.search);

    return this.http.get<EmployeeItem[]>(`${this.apiUrl}/export`, { params });
  }

  inviteEmployee(data: InviteEmployeeRequest): Observable<EmployeeItem> {
    return this.http.post<EmployeeItem>(`${this.apiUrl}/invite`, data);
  }

  updateEmployee(id: number, data: UpsertEmployeeRequest): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, data);
  }

  getRoleAssignments(id: number): Observable<RoleAssignment[]> {
    return this.http.get<RoleAssignment[]>(`${this.identityApiUrl}/${id}/roles`);
  }

  assignRoles(id: number, roles: RoleAssignmentInput[]): Observable<void> {
    return this.http.put<void>(`${this.identityApiUrl}/${id}/roles`, { roles });
  }

  activate(id: number): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}/activate`, {});
  }

  deactivate(id: number): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}/deactivate`, {});
  }
}
