import { Service, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface MultiTenantSettings {
  defaultTenantLoginEnabled: boolean;
  multiTenantDisabled: boolean;
  defaultTenantId: number | null;
  defaultTenantName: string | null;
}

export interface UpdateMultiTenantSettingsRequest {
  defaultTenantLoginEnabled: boolean;
  multiTenantDisabled: boolean;
  defaultTenantId: number | null;
}

@Service()
export class MultiTenantSettingsManagement {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/system/platform/multi-tenant-settings`;

  getSettings(): Observable<MultiTenantSettings> {
    return this.http.get<MultiTenantSettings>(this.apiUrl);
  }

  updateSettings(data: UpdateMultiTenantSettingsRequest): Observable<void> {
    return this.http.put<void>(this.apiUrl, data);
  }
}
