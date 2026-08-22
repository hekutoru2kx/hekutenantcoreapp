import { Service, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface CountryDto {
  id: number;
  name: string;
  iso2: string;
  phoneCode?: string;
}

export interface StateDto {
  id: number;
  name: string;
  stateCode?: string;
}

export interface CityDto {
  id: number;
  name: string;
}

@Service()
export class Geography {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/geography`;

  getCountries(): Observable<CountryDto[]> {
    return this.http.get<CountryDto[]>(`${this.apiUrl}/countries`);
  }

  getStates(countryId: number): Observable<StateDto[]> {
    return this.http.get<StateDto[]>(`${this.apiUrl}/countries/${countryId}/states`);
  }

  getCities(stateId: number): Observable<CityDto[]> {
    return this.http.get<CityDto[]>(`${this.apiUrl}/states/${stateId}/cities`);
  }
}