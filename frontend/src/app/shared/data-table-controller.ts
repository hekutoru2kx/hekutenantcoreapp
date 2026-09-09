import { computed, signal } from '@angular/core';
import { MatTableDataSource } from '@angular/material/table';
import { MatPaginator, PageEvent } from '@angular/material/paginator';
import { MatSort, Sort } from '@angular/material/sort';
import { PAGINATION } from '../constants/pagination';
import { ExportColumn, exportToCsv } from './csv-export';
import { ReorderableColumn } from '../components/column-reorder/column-reorder';
import { loadColumnPreferences, saveColumnPreferences } from './column-preferences';

export type SortDirection = 'asc' | 'desc' | '';

// One column of an admin table. `header` is a thunk so the label re-reads on language switch
// (the same reason the old hand-rolled `columnDefs()` maps called `transloco.translate` lazily).
export interface DataTableColumn<T> {
  key: string; // matColumnDef id + the id this column persists under
  header?: () => string; // already-translated label; only read by the column menu + CSV export.
  // Omit it on a table that shows neither (its <th> labels live inline in the template) — it then
  // falls back to `key`.
  sortable?: boolean; // caller still adds `mat-sort-header` in the template; informational here
  sortKey?: string; // server `sortBy` value / client accessor key (default: key)
  hideable?: boolean; // appears in the column menu (default true)
  hidden?: boolean; // initial hidden state (default false)
  exportValue?: (row: T) => string; // CSV cell; omit to leave the column out of the export
  sortValue?: (row: T) => string | number | Date; // client-mode sortingDataAccessor (default: row[sortKey])
}

export interface DataTableOptions<T> {
  tableKey: string; // localStorage suffix, via column-preferences.ts
  columns: DataTableColumn<T>[];
  mode?: 'server' | 'client'; // default 'server'
  actionsColumn?: boolean; // append 'actions' to displayedColumns (default true)
  defaultSort?: { active: string; direction: 'asc' | 'desc' };
  persistSort?: boolean; // also write sort into prefs (default false)
  pageSize?: number; // default PAGINATION.defaultPageSize
  pageSizeOptions?: number[]; // default PAGINATION.pageSizeOptions
  onChange?: () => void; // server mode: caller reloads on a sort/page change
}

const ACTIONS_COLUMN = 'actions';

// Assembles the per-table machinery every admin grid used to hand-copy: column order/visibility
// with `localStorage` persistence, the `<app-column-reorder>` projection, sort + pagination state,
// and the CSV export column set. The page keeps full control of its `<table mat-table>` markup,
// cell templates, and the actions column — this only owns state, never rendering.
//
// Instantiate as a component field: `table = new DataTableController<Row>({ ... })`. It uses only
// `signal`/`computed`, so it needs no injection context. Call `restore()` once from `ngOnInit`.
export class DataTableController<T> {
  private readonly allColumns: DataTableColumn<T>[];
  private readonly columnByKey: Map<string, DataTableColumn<T>>;
  private readonly knownKeys: string[];
  private readonly tableKey: string;
  private readonly mode: 'server' | 'client';
  private readonly withActions: boolean;
  private readonly persistSort: boolean;
  private readonly changed?: () => void;

  readonly pageSizeOptions: number[];
  pageSize: number;
  pageIndex = 0;

  readonly showColumnMenu = signal(false);
  readonly columnOrder = signal<string[]>([]);
  readonly hiddenColumns = signal<Set<string>>(new Set());
  readonly sort = signal<{ active: string; direction: SortDirection }>({ active: '', direction: '' });

  // Client mode only — bind to `<table mat-table [dataSource]="table.dataSource">`.
  readonly dataSource = new MatTableDataSource<T>([]);

  readonly displayedColumns = computed(() => {
    const hidden = this.hiddenColumns();
    const visible = this.columnOrder().filter((k) => !hidden.has(k));
    return this.withActions ? [...visible, ACTIONS_COLUMN] : visible;
  });

  readonly reorderableColumns = computed<ReorderableColumn[]>(() => {
    const hidden = this.hiddenColumns();
    return this.columnOrder()
      .map((k) => this.columnByKey.get(k))
      .filter((c): c is DataTableColumn<T> => !!c && c.hideable !== false)
      .map((c) => ({ key: c.key, label: this.headerLabel(c), hidden: hidden.has(c.key) }));
  });

  readonly exportColumns = computed<ExportColumn<T>[]>(() => {
    const hidden = this.hiddenColumns();
    return this.columnOrder()
      .filter((k) => !hidden.has(k))
      .map((k) => this.columnByKey.get(k))
      .filter(
        (c): c is DataTableColumn<T> & { exportValue: (row: T) => string } => !!c && !!c.exportValue,
      )
      .map((c) => ({ key: c.key, header: this.headerLabel(c), getValue: c.exportValue }));
  });

  private headerLabel(c: DataTableColumn<T>): string {
    return c.header ? c.header() : c.key;
  }

  constructor(options: DataTableOptions<T>) {
    this.allColumns = options.columns;
    this.columnByKey = new Map(this.allColumns.map((c) => [c.key, c]));
    this.knownKeys = this.allColumns.map((c) => c.key);
    this.tableKey = options.tableKey;
    this.mode = options.mode ?? 'server';
    this.withActions = options.actionsColumn ?? true;
    this.persistSort = options.persistSort ?? false;
    this.changed = options.onChange;
    this.pageSizeOptions = options.pageSizeOptions ?? PAGINATION.pageSizeOptions;
    this.pageSize = options.pageSize ?? PAGINATION.defaultPageSize;

    this.columnOrder.set([...this.knownKeys]);
    this.hiddenColumns.set(new Set(this.allColumns.filter((c) => c.hidden).map((c) => c.key)));
    if (options.defaultSort) {
      this.sort.set({ active: options.defaultSort.active, direction: options.defaultSort.direction });
    }

    if (this.mode === 'client') {
      this.dataSource.sortingDataAccessor = (row, columnId) => {
        const col = this.columnByKey.get(columnId);
        if (col?.sortValue) {
          const v = col.sortValue(row);
          return v instanceof Date ? v.getTime() : v;
        }
        const value = (row as Record<string, unknown>)[col?.sortKey ?? columnId];
        return typeof value === 'number' ? value : String(value ?? '');
      };
    }
  }

  // Apply persisted column order/visibility (and sort, when persistSort is on). Call from ngOnInit.
  restore(): void {
    const saved = loadColumnPreferences(this.tableKey);
    if (!saved) return;

    const validOrder = saved.order.filter((k) => this.knownKeys.includes(k));
    const missing = this.knownKeys.filter((k) => !validOrder.includes(k));
    this.columnOrder.set([...validOrder, ...missing]);
    this.hiddenColumns.set(new Set(saved.hidden.filter((k) => this.knownKeys.includes(k))));

    if (this.persistSort && saved.sortActive) {
      this.sort.set({
        active: saved.sortActive,
        direction: (saved.sortDirection ?? '') as SortDirection,
      });
    }
  }

  private persist(): void {
    saveColumnPreferences(this.tableKey, {
      order: this.columnOrder(),
      hidden: Array.from(this.hiddenColumns()),
      ...(this.persistSort
        ? { sortActive: this.sort().active, sortDirection: this.sort().direction }
        : {}),
    });
  }

  toggleColumnMenu(): void {
    this.showColumnMenu.update((v) => !v);
  }

  // `<app-column-reorder>` emits keys for the hideable columns only; any non-hideable column keeps
  // its place at the end so it still renders.
  onColumnsReordered(order: string[]): void {
    const next = order.filter((k) => this.knownKeys.includes(k));
    const missing = this.knownKeys.filter((k) => !next.includes(k));
    this.columnOrder.set([...next, ...missing]);
    this.persist();
  }

  onColumnVisibilityToggled(key: string): void {
    const next = new Set(this.hiddenColumns());
    if (next.has(key)) next.delete(key);
    else next.add(key);
    this.hiddenColumns.set(next);
    this.persist();
  }

  onSortChange(s: Sort): void {
    this.sort.set({ active: s.active, direction: s.direction });
    this.pageIndex = 0;
    if (this.persistSort) this.persist();
    if (this.mode === 'server') this.changed?.();
  }

  onPageChange(e: PageEvent): void {
    this.pageIndex = e.pageIndex;
    this.pageSize = e.pageSize;
    if (this.mode === 'server') this.changed?.();
  }

  // 1-based page number, for server query params.
  get page(): number {
    return this.pageIndex + 1;
  }

  get sortActive(): string {
    return this.sort().active;
  }

  get sortDirection(): SortDirection {
    return this.sort().direction;
  }

  // ---- client mode wiring ----

  setData(rows: T[]): void {
    this.dataSource.data = rows;
  }

  attachSort(sort: MatSort): void {
    this.dataSource.sort = sort;
  }

  attachPaginator(paginator: MatPaginator): void {
    this.dataSource.paginator = paginator;
  }

  // ---- export ----

  // Server mode: pass the rows to write (usually the current page). Client mode: omit to export
  // the current filtered set.
  exportRows(filename: string, rows?: T[]): void {
    exportToCsv(filename, this.exportColumns(), rows ?? this.dataSource.filteredData);
  }
}
