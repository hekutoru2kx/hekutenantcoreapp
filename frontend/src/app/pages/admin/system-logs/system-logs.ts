import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatExpansionModule } from '@angular/material/expansion';
import { TranslocoModule } from '@jsverse/transloco';
import { SystemLogsManagement, SystemLogEntry } from '../../../services/system-logs-management';

const CATEGORIES = ['Http', 'Database', 'Security', 'Integration', 'Business', 'System'];
const LEVELS = ['Trace', 'Debug', 'Information', 'Warning', 'Error', 'Critical'];

@Component({
  selector: 'app-system-logs',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatTableModule,
    MatPaginatorModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatProgressBarModule,
    MatExpansionModule,
    TranslocoModule
  ],
  templateUrl: './system-logs.html',
  styleUrl: './system-logs.scss',
})
export class SystemLogsPage implements OnInit {
  private service = inject(SystemLogsManagement);
  private fb = inject(FormBuilder);

  categories = CATEGORIES;
  levels = LEVELS;
  displayedColumns = ['timestamp', 'level', 'category', 'message', 'tenantId', 'userId'];

  loading = signal(false);
  errorMessage = signal<string | null>(null);
  items = signal<SystemLogEntry[]>([]);
  totalCount = signal(0);
  pageIndex = signal(0);
  pageSize = signal(50);

  filters = this.fb.group({
    tenantId: [null as number | null],
    category: [null as string | null],
    level: [null as string | null],
    from: [null as string | null],
    to: [null as string | null]
  });

  ngOnInit(): void {
    this.load();
  }

  applyFilters(): void {
    this.pageIndex.set(0);
    this.load();
  }

  onPage(event: PageEvent): void {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
    this.load();
  }

  private load(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    const value = this.filters.value;
    this.service.query({
      tenantId: value.tenantId,
      category: value.category,
      level: value.level,
      from: value.from ? new Date(value.from).toISOString() : null,
      to: value.to ? new Date(value.to).toISOString() : null,
      page: this.pageIndex() + 1,
      pageSize: this.pageSize()
    }).subscribe({
      next: (page) => {
        this.items.set(page.items);
        this.totalCount.set(page.totalCount);
        this.loading.set(false);
      },
      error: (err) => {
        this.errorMessage.set(err.error ?? 'Failed to load logs');
        this.loading.set(false);
      }
    });
  }
}
