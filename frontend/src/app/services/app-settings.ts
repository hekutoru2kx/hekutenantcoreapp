import { Service, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface AppSettings {
  requireEmailConfirmation: boolean;
}

@Service()
export class AppSettingsService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/admin/app-settings`;

  getSettings(): Observable<AppSettings> {
    return this.http.get<AppSettings>(this.apiUrl);
  }

  updateSettings(data: AppSettings): Observable<void> {
    return this.http.put<void>(this.apiUrl, data);
  }
}
