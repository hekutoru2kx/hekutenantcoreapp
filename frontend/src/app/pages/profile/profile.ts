import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatSelectModule } from '@angular/material/select';
import { MatIconModule } from '@angular/material/icon';
import { MatDividerModule } from '@angular/material/divider';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { HttpClient } from '@angular/common/http';
import { Auth, TenantSummary } from '../../services/auth';
import { Theme } from '../../services/theme';
import { environment } from '../../../environments/environment';
import { PersonForm, PersonFormData } from '../../components/person-form/person-form';

export interface UserProfile {
  id: string;
  userName: string;
  email: string;
  preferredLanguage: string;
  preferredTheme: string;
  roles: string[];
  defaultTenantId: number | null;
}

export interface PersonData {
  id?: number;
  firstName?: string;
  lastName?: string;
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
}

@Component({
  selector: 'app-profile',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatCardModule,
    MatSelectModule,
    MatIconModule,
    MatDividerModule,
    MatProgressBarModule,
    TranslocoModule,
    RouterLink,
    PersonForm
  ],
  templateUrl: './profile.html',
  styleUrl: './profile.scss',
})
export class Profile implements OnInit {
  private fb = inject(FormBuilder);
  protected auth = inject(Auth);
  protected theme = inject(Theme);
  private http = inject(HttpClient);
  private transloco = inject(TranslocoService);

  errorMessage = signal('');
  successMessage = signal('');
  personErrorMessage = signal('');
  personSuccessMessage = signal('');

  loadingPerson = signal(false);
  personFormData = signal<PersonFormData | null>(null);
  availableTenants = signal<TenantSummary[]>([]);

  availableLanguages = [
    { code: 'es', label: 'Español' },
    { code: 'en', label: 'English' }
  ];

  form = this.fb.group({
    userName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    preferredLanguage: ['en', Validators.required],
    preferredTheme: ['azure', Validators.required],
    defaultTenantId: [null as number | null]
  });

  ngOnInit(): void {
    // Load user preferences
    this.http.get<UserProfile>(`${environment.apiUrl}/user/profile`).subscribe({
      next: (profile) => {
        this.form.patchValue({
          userName: profile.userName,
          email: profile.email,
          preferredLanguage: profile.preferredLanguage,
          preferredTheme: profile.preferredTheme,
          defaultTenantId: profile.defaultTenantId
        });
        this.theme.setColorTheme(profile.preferredTheme as any);
      },
      error: (err) => this.errorMessage.set(err.error || this.transloco.translate('common.loadError'))
    });

    // Only relevant to show a picker if the user actually has more than one tenant to choose
    // from — a single-membership user has nothing to set a "default" between.
    this.auth.getAvailableTenants().subscribe({
      next: (tenants) => this.availableTenants.set(tenants)
    });

    this.loadPersonData();
  }

  private loadPersonData(): void {
    this.loadingPerson.set(true);
    this.http.get<PersonData>(`${environment.apiUrl}/user/person`).subscribe({
      next: (person) => {
        if (person) {
          this.personFormData.set({
            firstName: person.firstName || '',
            lastName: person.lastName || '',
            birthday: person.birthday ? new Date(person.birthday) : null,
            documentType: person.documentType,
            documentId: person.documentId,
            phone: person.phone,
            phoneExtension: person.phoneExtension,
            email: person.email,
            address: person.address,
            postalCode: person.postalCode,
            gender: person.gender,
            alternativePhone: person.alternativePhone,
            countryId: person.countryId,
            stateId: person.stateId,
            cityId: person.cityId
          });
        }
        this.loadingPerson.set(false);
      },
      error: (err) => {
        this.personErrorMessage.set(err.error || this.transloco.translate('common.loadError'));
        this.loadingPerson.set(false);
      }
    });
  }

  onSubmit(): void {
    if (this.form.invalid) return;

    const { userName, email, preferredLanguage, preferredTheme, defaultTenantId } = this.form.value;

    this.http.put(
      `${environment.apiUrl}/user/profile`,
      { userName, email, preferredLanguage, preferredTheme, defaultTenantId }
    ).subscribe({
      next: () => {
        const current = this.auth.currentUser();
        if (current) {
          const updated = { ...current, userName: userName!, email: email! };
          this.auth.currentUser.set(updated);
          localStorage.setItem('hekutenantcoreapp_user', JSON.stringify(updated));
        }
        this.theme.setColorTheme(preferredTheme as any);
        this.successMessage.set(this.transloco.translate('profile.updateSuccess'));
        this.errorMessage.set('');
      },
      error: (err) => {
        this.errorMessage.set(err.error || this.transloco.translate('profile.updateError'));
        this.successMessage.set('');
      }
    });
  }

  onPersonSaved(data: PersonFormData): void {
    this.http.put(`${environment.apiUrl}/user/person`, data).subscribe({
      next: () => {
        this.personSuccessMessage.set(this.transloco.translate('profile.updateSuccess'));
        this.personErrorMessage.set('');
        this.loadPersonData();
      },
      error: (err) => {
        this.personErrorMessage.set(err.error || this.transloco.translate('common.saveError'));
        this.personSuccessMessage.set('');
      }
    });
  }

}