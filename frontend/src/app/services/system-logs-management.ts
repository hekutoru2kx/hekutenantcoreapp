import { Service, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface SystemLogEntry {
  id: number;
  timestamp: string;
  level: string;
  category: string;
  message: string;
  exception: string | null;
  tenantId: number | null;
  userId: string | null;
  traceId: string | null;
}

export interface SystemLogsPage {
  items: SystemLogEntry[];
  totalCount: number;
}

export interface SystemLogsQuery {
  tenantId?: number | null;
  category?: string | null;
  level?: string | null;
  from?: string | null;
  to?: string | null;
  page: number;
  pageSize: number;
}

@Service()
export class SystemLogsManagement {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/system/platform/logs`;

  query(query: SystemLogsQuery): Observable<SystemLogsPage> {
    let params = new HttpParams().set('page', query.page).set('pageSize', query.pageSize);
    if (query.tenantId != null) params = params.set('tenantId', query.tenantId);
    if (query.category) params = params.set('category', query.category);
    if (query.level) params = params.set('level', query.level);
    if (query.from) params = params.set('from', query.from);
    if (query.to) params = params.set('to', query.to);

    return this.http.get<SystemLogsPage>(this.apiUrl, { params });
  }
}
