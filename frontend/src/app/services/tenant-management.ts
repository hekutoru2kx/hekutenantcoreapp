import { Service, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { TenantSummary } from './auth';

export interface TenantItem {
  id: number;
  name: string;
  tenantType?: string;
  countryId?: number | null;
  stateId?: number | null;
  cityId?: number | null;
  countryName?: string;
  stateName?: string;
  cityName?: string;
  phone?: string;
  urlSite?: string;
  email?: string;
  isActive: boolean;
  attachmentRetentionDays?: number | null;
}

export interface PagedTenantResult {
  items: TenantItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface TenantListQueryParams {
  page: number;
  pageSize: number;
  sortBy?: string;
  sortDirection?: string;
  search?: string;
}

export interface TenantExportQueryParams {
  sortBy?: string;
  sortDirection?: string;
  search?: string;
}

export interface UpsertTenantRequest {
  name: string;
  tenantType?: string;
  countryId?: number | null;
  stateId?: number | null;
  cityId?: number | null;
  phone?: string;
  urlSite?: string;
  email?: string;
  isActive: boolean;
  attachmentRetentionDays?: number | null;
}

@Service()
export class TenantManagement {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/system/platform/tenants`;

  getTenants(query: TenantListQueryParams): Observable<PagedTenantResult> {
    let params = new HttpParams()
      .set('page', query.page)
      .set('pageSize', query.pageSize);

    if (query.sortBy) params = params.set('sortBy', query.sortBy);
    if (query.sortDirection) params = params.set('sortDirection', query.sortDirection);
    if (query.search) params = params.set('search', query.search);

    return this.http.get<PagedTenantResult>(this.apiUrl, { params });
  }

  getAllTenants(query: TenantExportQueryParams): Observable<TenantItem[]> {
    let params = new HttpParams();
    if (query.sortBy) params = params.set('sortBy', query.sortBy);
    if (query.sortDirection) params = params.set('sortDirection', query.sortDirection);
    if (query.search) params = params.set('search', query.search);

    return this.http.get<TenantItem[]>(`${this.apiUrl}/export`, { params });
  }

  getActiveTenants(search?: string): Observable<TenantSummary[]> {
    let params = new HttpParams();
    if (search) params = params.set('search', search);
    return this.http.get<TenantSummary[]>(`${this.apiUrl}/active`, { params });
  }

  getTenant(id: number): Observable<TenantItem> {
    return this.http.get<TenantItem>(`${this.apiUrl}/${id}`);
  }

  createTenant(data: UpsertTenantRequest): Observable<TenantItem> {
    return this.http.post<TenantItem>(this.apiUrl, data);
  }

  updateTenant(id: number, data: UpsertTenantRequest): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, data);
  }
}
