import { Service, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface LoggingCategorySetting {
  category: string;
  minimumLevel: string;
}

export interface LoggingSettings {
  categories: LoggingCategorySetting[];
  retentionDays: number;
}

export interface UpdateLoggingSettingsRequest {
  categories: LoggingCategorySetting[];
  retentionDays: number;
}

@Service()
export class LoggingSettingsManagement {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/system/platform/logging-settings`;

  getSettings(): Observable<LoggingSettings> {
    return this.http.get<LoggingSettings>(this.apiUrl);
  }

  updateSettings(data: UpdateLoggingSettingsRequest): Observable<void> {
    return this.http.put<void>(this.apiUrl, data);
  }
}
