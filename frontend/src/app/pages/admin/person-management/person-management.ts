import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule } from '@angular/material/paginator';
import { MatSortModule } from '@angular/material/sort';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatCardModule } from '@angular/material/card';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Auth } from '../../../services/auth';
import { environment } from '../../../../environments/environment';
import { debounceTime, Subject } from 'rxjs';
import { PersonForm, PersonFormData } from '../../../components/person-form/person-form';
import { ColumnReorder } from '../../../components/column-reorder/column-reorder';
import { DataTableController } from '../../../shared/data-table-controller';
import { AvatarUpload } from '../../../components/avatar-upload/avatar-upload';
import { ContentThumbnail } from '../../../components/content-thumbnail/content-thumbnail';

export interface PersonItem {
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
  profilePictureContentId?: number | null;
}

export interface PagedPersonResult {
  items: PersonItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

@Component({
  selector: 'app-person-management',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatTableModule,
    MatPaginatorModule,
    MatSortModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatCardModule,
    MatTooltipModule,
    MatProgressBarModule,
    TranslocoModule,
    PersonForm,
    ColumnReorder,
    AvatarUpload,
    ContentThumbnail
  ],
  templateUrl: './person-management.html',
  styleUrl: './person-management.scss',
})
export class PersonManagement implements OnInit {
  private http = inject(HttpClient);
  protected auth = inject(Auth);
  private fb = inject(FormBuilder);
  private transloco = inject(TranslocoService);

  editingPersonFormData = signal<PersonFormData | null>(null);
  persons = signal<PersonItem[]>([]);
  totalCount = signal(0);
  loading = signal(false);

  table = new DataTableController<PersonItem>({
    tableKey: 'persons',
    defaultSort: { active: 'lastName', direction: 'asc' },
    persistSort: true,
    onChange: () => this.loadPersons(),
    columns: [
      { key: 'picture', header: () => this.transloco.translate('persons.picture'), exportValue: () => '' },
      { key: 'lastName', header: () => this.transloco.translate('persons.lastName'), sortable: true, exportValue: (p) => p.lastName },
      { key: 'firstName', header: () => this.transloco.translate('persons.firstName'), sortable: true, exportValue: (p) => p.firstName },
      { key: 'email', header: () => this.transloco.translate('persons.email'), sortable: true, exportValue: (p) => p.email || '' },
      {
        key: 'document',
        header: () => this.transloco.translate('persons.document'),
        exportValue: (p) => p.documentType && p.documentId
          ? `${this.transloco.translate('persons.docType_' + p.documentType + '_code')}: ${p.documentId}`
          : '',
      },
      {
        key: 'phone',
        header: () => this.transloco.translate('persons.phone'),
        exportValue: (p) => `${p.phone || ''}${p.phoneExtension ? ' ext. ' + p.phoneExtension : ''}`,
      },
      {
        key: 'location',
        header: () => this.transloco.translate('profile.location'),
        sortable: true,
        sortKey: 'countryId',
        exportValue: (p) => [p.countryName, p.stateName, p.cityName].filter(Boolean).join(', '),
      },
    ],
  });

  searchControl = this.fb.control('');
  private searchSubject = new Subject<string>();

  showForm = signal(false);
  editingPerson = signal<PersonItem | null>(null);
  errorMessage = signal<string | null>(null);

  ngOnInit(): void {
    this.table.restore();

    this.searchSubject.pipe(debounceTime(400)).subscribe(() => {
      this.table.pageIndex = 0;
      this.loadPersons();
    });

    this.searchControl.valueChanges.subscribe(val => this.searchSubject.next(val || ''));
    this.loadPersons();
  }

  loadPersons(): void {
    this.loading.set(true);
    let params = new HttpParams()
      .set('page', this.table.page)
      .set('pageSize', this.table.pageSize)
      .set('sortBy', this.table.sortActive)
      .set('sortDirection', this.table.sortDirection || 'asc');

    if (this.searchControl.value)
      params = params.set('search', this.searchControl.value);

    this.http.get<PagedPersonResult>(`${environment.apiUrl}/admin/organization/persons`, { params }).subscribe({
      next: (data) => {
        this.persons.set(data.items);
        this.totalCount.set(data.totalCount);
        this.loading.set(false);
      },
      error: (err) => {
        this.errorMessage.set(err.error || this.transloco.translate('common.loadError'));
        this.loading.set(false);
      }
    });
  }

  exportPersons(): void {
    let params = new HttpParams()
      .set('sortBy', this.table.sortActive)
      .set('sortDirection', this.table.sortDirection || 'asc');

    if (this.searchControl.value)
      params = params.set('search', this.searchControl.value);

    this.http.get<PersonItem[]>(`${environment.apiUrl}/admin/organization/persons/export`, { params }).subscribe({
      next: (data) => this.table.exportRows('persons.csv', data),
      error: (err) => this.errorMessage.set(err.error || this.transloco.translate('common.loadError'))
    });
  }

  toggleForm(person?: PersonItem): void {
    this.editingPerson.set(person || null);
    this.editingPersonFormData.set(person ? this.toPersonFormData(person) : null);
    this.showForm.set(!this.showForm());
    this.errorMessage.set(null);
  }

  onPersonSaved(data: PersonFormData): void {
    const editing = this.editingPerson();
    const request = editing
      ? this.http.put(`${environment.apiUrl}/admin/organization/persons/${editing.id}`, data)
      : this.http.post(`${environment.apiUrl}/admin/organization/persons`, data);

    request.subscribe({
      next: () => {
        this.showForm.set(false);
        this.editingPerson.set(null);
        this.loadPersons();
      },
      error: (err) => this.errorMessage.set(err.error || this.transloco.translate('common.saveError'))
    });
  }

  onPersonCancelled(): void {
    this.showForm.set(false);
    this.editingPerson.set(null);
  }

  onPictureUploaded(contentId: number): void {
    const editing = this.editingPerson();
    if (editing) this.editingPerson.set({ ...editing, profilePictureContentId: contentId });
    this.loadPersons();
  }

  onPictureRemoved(): void {
    const editing = this.editingPerson();
    if (editing) this.editingPerson.set({ ...editing, profilePictureContentId: null });
    this.loadPersons();
  }

  toggleAccess(person: PersonItem): void {
    const suspending = person.membershipStatus !== 'Suspended';
    const request = suspending
      ? this.http.put(`${environment.apiUrl}/admin/organization/persons/${person.id}/suspend-access`, {})
      : this.http.put(`${environment.apiUrl}/admin/organization/persons/${person.id}/reactivate-access`, {});

    request.subscribe({
      next: () => this.loadPersons(),
      error: (err) => this.errorMessage.set(err.error || this.transloco.translate('common.saveError'))
    });
  }

  canCreatePerson(): boolean {
    return this.auth.hasClaim('PersonsPermission', 'Create');
  }

  canUpdatePerson(): boolean {
    return this.auth.hasClaim('PersonsPermission', 'Update');
  }

  toPersonFormData(person: PersonItem): PersonFormData {
    return {
      firstName: person.firstName,
      lastName: person.lastName,
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
    };
  }
}
