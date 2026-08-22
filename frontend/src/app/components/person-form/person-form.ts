import { Component, inject, input, output, signal, OnInit, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { HttpClient, HttpParams } from '@angular/common/http';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule, DateAdapter, MAT_DATE_FORMATS } from '@angular/material/core';
import { MatDividerModule } from '@angular/material/divider';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { Geography, CountryDto, StateDto, CityDto } from '../../services/geography';
import { PAGINATION } from '../../constants/pagination';
import { YmdDateAdapter, YMD_DATE_FORMATS } from '../../shared/ymd-date-adapter';
import { environment } from '../../../environments/environment';

interface PersonMatch {
  matchFound: boolean;
  linkable: boolean;
}

export interface PersonFormData {
  firstName: string;
  lastName: string;
  birthday?: Date | null;
  documentType?: string;
  documentId?: string;
  phone?: string;
  phoneExtension?: string;
  email?: string;
  address?: string;
  postalCode?: string;
  gender?: string;
  alternativePhone?: string;
  countryId?: number | null;
  stateId?: number | null;
  cityId?: number | null;
}

@Component({
  selector: 'app-person-form',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatDividerModule,
    MatProgressBarModule,
    TranslocoModule
  ],
  providers: [
    { provide: DateAdapter, useClass: YmdDateAdapter },
    { provide: MAT_DATE_FORMATS, useValue: YMD_DATE_FORMATS },
  ],
  templateUrl: './person-form.html',
  styleUrl: './person-form.scss',
})
export class PersonForm implements OnInit, OnChanges {
  private fb = inject(FormBuilder);
  private geography = inject(Geography);
  private transloco = inject(TranslocoService);
  private http = inject(HttpClient);

  // Inputs
  personData = input<PersonFormData | null>(null);
  readOnly = input<boolean>(false);
  showSaveButton = input<boolean>(true);
  // Enables the "this will link to an existing record" live check as the email field
  // changes — only meaningful for the self-service profile flow, where saving can match
  // and link to an existing unlinked Person by document + email.
  checkExistingOnEmail = input<boolean>(false);

  // Outputs
  saved = output<PersonFormData>();
  cancelled = output<void>();

  countries = signal<CountryDto[]>([]);
  states = signal<StateDto[]>([]);
  cities = signal<CityDto[]>([]);
  loading = signal(false);
  existingMatch = signal<PersonMatch | null>(null);
  errorMessage = signal<string | null>(null);
  ageDisplay = signal<string>('—');

  documentTypes = ['NationalId', 'Passport', 'ForeignId', 'TaxId', 'SpecialPermanentPermit', 'Other'];
  genders = ['Male', 'Female', 'NonBinary', 'PreferNotToSay', 'Other'];

  form = this.fb.group({
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    birthday: [null as Date | null],
    documentType: [''],
    documentId: [''],
    phone: [''],
    phoneExtension: [''],
    email: ['', Validators.email],
    address: [''],
    postalCode: [''],
    gender: [''],
    alternativePhone: [''],
    countryId: [null as number | null],
    stateId: [null as number | null],
    cityId: [null as number | null]
  });

  ngOnInit(): void {
    this.geography.getCountries().subscribe({
      next: (data) => this.countries.set(data)
    });

    if (this.readOnly()) {
      this.form.disable();
    }

    if (this.checkExistingOnEmail()) {
      this.form.controls.email.valueChanges.pipe(
        debounceTime(400),
        distinctUntilChanged()
      ).subscribe(() => this.checkExistingPerson());
    }

    this.form.controls.birthday.valueChanges.subscribe(v => this.ageDisplay.set(this.computeAge(v)));
  }

  private computeAge(birthday: Date | null): string {
    if (!birthday) return '—';

    const today = new Date();
    let age = today.getFullYear() - birthday.getFullYear();
    const monthDiff = today.getMonth() - birthday.getMonth();
    if (monthDiff < 0 || (monthDiff === 0 && today.getDate() < birthday.getDate())) age--;

    return age >= 0 ? String(age) : '—';
  }

  private checkExistingPerson(): void {
    const { documentType, documentId, email } = this.form.value;
    if (!documentType || !documentId || !email) {
      this.existingMatch.set(null);
      return;
    }

    const params = new HttpParams()
      .set('documentType', documentType)
      .set('documentId', documentId)
      .set('email', email);

    this.http.get<PersonMatch>(`${environment.apiUrl}/user/person/check-existing`, { params }).subscribe({
      next: (result) => this.existingMatch.set(result.matchFound ? result : null),
      error: () => this.existingMatch.set(null)
    });
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['personData']) {
      this.patchForm();
    }
  }

  private patchForm(): void {
    const data = this.personData();
    if (!data) return;

    this.form.patchValue({
      firstName: data.firstName,
      lastName: data.lastName,
      birthday: data.birthday ? new Date(data.birthday) : null,
      documentType: data.documentType || '',
      documentId: data.documentId || '',
      phone: data.phone || '',
      phoneExtension: data.phoneExtension || '',
      email: data.email || '',
      address: data.address || '',
      postalCode: data.postalCode || '',
      gender: data.gender || '',
      alternativePhone: data.alternativePhone || '',
      countryId: data.countryId || null,
      stateId: data.stateId || null,
      cityId: data.cityId || null
    });
    this.ageDisplay.set(this.computeAge(data.birthday ? new Date(data.birthday) : null));

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
    this.saved.emit(this.form.value as PersonFormData);
  }

  onCancel(): void {
    this.cancelled.emit();
  }
}