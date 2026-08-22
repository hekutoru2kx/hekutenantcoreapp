import { Component, inject, input, output, signal, OnInit, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatDividerModule } from '@angular/material/divider';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { TranslocoModule } from '@jsverse/transloco';
import { Geography, CountryDto, StateDto, CityDto } from '../../services/geography';

export interface TenantFormData {
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

@Component({
  selector: 'app-tenant-form',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatCheckboxModule,
    MatDividerModule,
    MatProgressBarModule,
    TranslocoModule
  ],
  templateUrl: './tenant-form.html',
  styleUrl: './tenant-form.scss',
})
export class TenantForm implements OnInit, OnChanges {
  private fb = inject(FormBuilder);
  private geography = inject(Geography);

  tenantData = input<TenantFormData | null>(null);
  showSaveButton = input<boolean>(true);

  saved = output<TenantFormData>();
  cancelled = output<void>();

  countries = signal<CountryDto[]>([]);
  states = signal<StateDto[]>([]);
  cities = signal<CityDto[]>([]);

  tenantTypes = ['Clinic', 'Hospital', 'Lab', 'Other'];

  form = this.fb.group({
    name: ['', Validators.required],
    tenantType: ['Clinic'],
    countryId: [null as number | null],
    stateId: [null as number | null],
    cityId: [null as number | null],
    phone: [''],
    urlSite: [''],
    email: ['', Validators.email],
    isActive: [true],
    attachmentRetentionDays: [null as number | null]
  });

  ngOnInit(): void {
    this.geography.getCountries().subscribe({
      next: (data) => this.countries.set(data)
    });
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['tenantData']) {
      this.patchForm();
    }
  }

  private patchForm(): void {
    const data = this.tenantData();
    if (!data) return;

    this.form.patchValue({
      name: data.name,
      tenantType: data.tenantType || 'Clinic',
      countryId: data.countryId || null,
      stateId: data.stateId || null,
      cityId: data.cityId || null,
      phone: data.phone || '',
      urlSite: data.urlSite || '',
      email: data.email || '',
      isActive: data.isActive,
      attachmentRetentionDays: data.attachmentRetentionDays ?? null
    });

    if (data.countryId) {
      this.geography.getStates(data.countryId).subscribe({
        next: (states) => {
          this.states.set(states);
          if (data.stateId) {
            this.geography.getCities(data.stateId).subscribe({
              next: (cities) => this.cities.set(cities)
            });
          }
        }
      });
    }
  }

  onCountryChange(countryId: number): void {
    this.states.set([]);
    this.cities.set([]);
    this.form.patchValue({ stateId: null, cityId: null });
    if (countryId) {
      this.geography.getStates(countryId).subscribe({
        next: (data) => this.states.set(data)
      });
    }
  }

  onStateChange(stateId: number): void {
    this.cities.set([]);
    this.form.patchValue({ cityId: null });
    if (stateId) {
      this.geography.getCities(stateId).subscribe({
        next: (data) => this.cities.set(data)
      });
    }
  }

  onSubmit(): void {
    if (this.form.invalid) return;
    this.saved.emit(this.form.value as TenantFormData);
  }

  onCancel(): void {
    this.cancelled.emit();
  }
}
