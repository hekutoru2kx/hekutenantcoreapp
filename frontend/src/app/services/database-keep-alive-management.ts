import { Service, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface DatabaseKeepAliveSettings {
  isEnabled: boolean;
  intervalAmount: number;
  intervalUnit: string;
  gmtOffsetHours: number;
  activeStartTime: string;
  activeEndTime: string;
  lastPingAt: string | null;
}

export interface UpdateDatabaseKeepAliveSettingsRequest {
  isEnabled: boolean;
  intervalAmount: number;
  intervalUnit: string;
  gmtOffsetHours: number;
  activeStartTime: string;
  activeEndTime: string;
}

@Service()
export class DatabaseKeepAliveManagement {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/system/platform/database-keep-alive`;

  getSettings(): Observable<DatabaseKeepAliveSettings> {
    return this.http.get<DatabaseKeepAliveSettings>(this.apiUrl);
  }

  updateSettings(data: UpdateDatabaseKeepAliveSettingsRequest): Observable<void> {
    return this.http.put<void>(this.apiUrl, data);
  }
}
