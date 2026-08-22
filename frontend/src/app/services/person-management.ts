import { Service, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface PersonRecord {
  id: number;
  firstName: string;
  lastName: string;
  birthday?: string;
  documentType?: string;
  documentId?: string;
  phone?: string;
  phoneExtension?: string;
  email?: string;
  address?: string;
  postalCode?: string;
  gender?: string;
  alternativePhone?: string;
  countryId?: number;
  stateId?: number;
  cityId?: number;
  countryName?: string;
  stateName?: string;
  cityName?: string;
  linkedUserName?: string | null;
  membershipStatus?: string | null;
}

// Person's own CRUD lives inline in person-management.ts (the one entity page that predates
// this service-per-entity convention). This is the minimal read/update surface needed to embed
// a Person's data elsewhere without duplicating that page's full HttpClient wiring.
@Service()
export class PersonManagement {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/admin/organization/persons`;

  getPerson(id: number): Observable<PersonRecord> {
    return this.http.get<PersonRecord>(`${this.apiUrl}/${id}`);
  }

  updatePerson(id: number, data: unknown): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, data);
  }
}
