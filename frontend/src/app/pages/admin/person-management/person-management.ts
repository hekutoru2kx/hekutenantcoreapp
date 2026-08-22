import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatSortModule, Sort } from '@angular/material/sort';
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
import { PAGINATION } from '../../../constants/pagination';
import { PersonForm, PersonFormData } from '../../../components/person-form/person-form';
import { ColumnReorder } from '../../../components/column-reorder/column-reorder';
import { exportToCsv, ExportColumn } from '../../../shared/csv-export';
import { loadColumnPreferences, saveColumnPreferences } from '../../../shared/column-preferences';

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
    ColumnReorder
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

  private readonly tableKey = 'persons';
  private readonly baseColumns = ['lastName', 'firstName', 'email', 'document', 'phone', 'location'];
  columnOrder = signal<string[]>([...this.baseColumns]);
  hiddenColumns = signal<Set<string>>(new Set());
  displayedColumns = computed(() => [...this.columnOrder().filter(c => !this.hiddenColumns().has(c)), 'actions']);

  pageSize = PAGINATION.defaultPageSize;
  pageSizeOptions = PAGINATION.pageSizeOptions;
  pageIndex = 0;
  sortActive = 'lastName';
  sortDirection: 'asc' | 'desc' | '' = 'asc';

  searchControl = this.fb.control('');
  private searchSubject = new Subject<string>();

  showForm = signal(false);
  editingPerson = signal<PersonItem | null>(null);
  errorMessage = signal<string | null>(null);
  showColumnMenu = signal(false);

  ngOnInit(): void {
    this.restoreColumnPreferences();

    this.searchSubject.pipe(debounceTime(400)).subscribe(() => {
      this.pageIndex = 0;
      this.loadPersons();
    });

    this.searchControl.valueChanges.subscribe(val => this.searchSubject.next(val || ''));
    this.loadPersons();
  }

  private restoreColumnPreferences(): void {
    const saved = loadColumnPreferences(this.tableKey);
    if (!saved) return;

    const validOrder = saved.order.filter(c => this.baseColumns.includes(c));
    const missing = this.baseColumns.filter(c => !validOrder.includes(c));
    this.columnOrder.set([...validOrder, ...missing]);
    this.hiddenColumns.set(new Set(saved.hidden.filter(c => this.baseColumns.includes(c))));
    if (saved.sortActive) this.sortActive = saved.sortActive;
    if (saved.sortDirection !== undefined) this.sortDirection = saved.sortDirection as 'asc' | 'desc' | '';
  }

  private persistColumnPreferences(): void {
    saveColumnPreferences(this.tableKey, {
      order: this.columnOrder(),
      hidden: Array.from(this.hiddenColumns()),
      sortActive: this.sortActive,
      sortDirection: this.sortDirection
    });
  }

  loadPersons(): void {
    this.loading.set(true);
    let params = new HttpParams()
      .set('page', this.pageIndex + 1)
      .set('pageSize', this.pageSize)
      .set('sortBy', this.sortActive)
      .set('sortDirection', this.sortDirection || 'asc');

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

  onPageChange(event: PageEvent): void {
    this.pageIndex = event.pageIndex;
    this.pageSize = event.pageSize;
    this.loadPersons();
  }

  onSortChange(sort: Sort): void {
    this.sortActive = sort.active;
    this.sortDirection = sort.direction;
    this.pageIndex = 0;
    this.persistColumnPreferences();
    this.loadPersons();
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

  onColumnsReordered(newOrder: string[]): void {
    this.columnOrder.set(newOrder);
    this.persistColumnPreferences();
  }

  onColumnVisibilityToggled(key: string): void {
    const updated = new Set(this.hiddenColumns());
    if (updated.has(key)) updated.delete(key); else updated.add(key);
    this.hiddenColumns.set(updated);
    this.persistColumnPreferences();
  }

  reorderableColumns = computed(() => {
    const defs = this.columnDefs();
    const hidden = this.hiddenColumns();
    return this.columnOrder().map(col => ({
      key: col,
      label: defs[col]?.header ?? col,
      hidden: hidden.has(col)
    }));
  });

  exportPersons(): void {
    let params = new HttpParams()
      .set('sortBy', this.sortActive)
      .set('sortDirection', this.sortDirection || 'asc');

    if (this.searchControl.value)
      params = params.set('search', this.searchControl.value);

    this.http.get<PersonItem[]>(`${environment.apiUrl}/admin/organization/persons/export`, { params }).subscribe({
      next: (data) => exportToCsv('persons.csv', this.getExportColumns(), data),
      error: (err) => this.errorMessage.set(err.error || this.transloco.translate('common.loadError'))
    });
  }

  private columnDefs(): Record<string, ExportColumn<PersonItem>> {
    return {
      lastName: {
        key: 'lastName',
        header: this.transloco.translate('persons.lastName'),
        getValue: (p) => p.lastName
      },
      firstName: {
        key: 'firstName',
        header: this.transloco.translate('persons.firstName'),
        getValue: (p) => p.firstName
      },
      email: {
        key: 'email',
        header: this.transloco.translate('persons.email'),
        getValue: (p) => p.email || ''
      },
      document: {
        key: 'document',
        header: this.transloco.translate('persons.document'),
        getValue: (p) => p.documentType && p.documentId
          ? `${this.transloco.translate('persons.docType_' + p.documentType + '_code')}: ${p.documentId}`
          : ''
      },
      phone: {
        key: 'phone',
        header: this.transloco.translate('persons.phone'),
        getValue: (p) => `${p.phone || ''}${p.phoneExtension ? ' ext. ' + p.phoneExtension : ''}`
      },
      location: {
        key: 'location',
        header: this.transloco.translate('profile.location'),
        getValue: (p) => [p.countryName, p.stateName, p.cityName].filter(Boolean).join(', ')
      }
    };
  }

  private getExportColumns(): ExportColumn<PersonItem>[] {
    const defs = this.columnDefs();
    return this.displayedColumns()
      .filter(col => col !== 'actions')
      .map(col => defs[col])
      .filter((col): col is ExportColumn<PersonItem> => !!col);
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