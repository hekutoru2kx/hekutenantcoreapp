import { DataTableController, DataTableOptions } from './data-table-controller';

interface Row {
  name: string;
  age: number;
  created: string;
}

function makeController(overrides: Partial<DataTableOptions<Row>> = {}) {
  return new DataTableController<Row>({
    tableKey: 'spec',
    columns: [
      { key: 'name', header: () => 'Name', sortable: true, exportValue: (r) => r.name },
      { key: 'age', header: () => 'Age', sortable: true, exportValue: (r) => String(r.age) },
      { key: 'created', header: () => 'Created', hideable: false },
    ],
    ...overrides,
  });
}

describe('DataTableController', () => {
  beforeEach(() => localStorage.clear());

  it('lists columns in config order and appends the actions column by default', () => {
    expect(makeController().displayedColumns()).toEqual(['name', 'age', 'created', 'actions']);
  });

  it('omits the actions column when actionsColumn is false', () => {
    expect(makeController({ actionsColumn: false }).displayedColumns()).toEqual([
      'name',
      'age',
      'created',
    ]);
  });

  it('excludes non-hideable columns from the reorder chip list', () => {
    expect(makeController().reorderableColumns().map((c) => c.key)).toEqual(['name', 'age']);
  });

  it('hides/shows a column, updates displayedColumns, and persists', () => {
    const c = makeController();
    c.onColumnVisibilityToggled('age');

    expect(c.displayedColumns()).toEqual(['name', 'created', 'actions']);
    const saved = JSON.parse(localStorage.getItem('hekutenantcoreapp.columnPrefs.spec')!);
    expect(saved.hidden).toEqual(['age']);

    c.onColumnVisibilityToggled('age');
    expect(c.displayedColumns()).toContain('age');
  });

  it('reorders hideable columns and keeps non-hideable ones at the end', () => {
    const c = makeController();
    c.onColumnsReordered(['age', 'name']);
    expect(c.columnOrder()).toEqual(['age', 'name', 'created']);
  });

  it('restore() applies a saved order/visibility and drops unknown keys', () => {
    localStorage.setItem(
      'hekutenantcoreapp.columnPrefs.spec',
      JSON.stringify({ order: ['age', 'stale', 'name'], hidden: ['name', 'stale'] }),
    );
    const c = makeController();
    c.restore();

    expect(c.columnOrder()).toEqual(['age', 'name', 'created']);
    expect(c.displayedColumns()).toEqual(['age', 'created', 'actions']);
  });

  it('applies defaultSort', () => {
    const c = makeController({ defaultSort: { active: 'name', direction: 'asc' } });
    expect(c.sort()).toEqual({ active: 'name', direction: 'asc' });
    expect(c.sortActive).toBe('name');
  });

  it('onSortChange resets the page and notifies in server mode', () => {
    const onChange = vi.fn();
    const c = makeController({ onChange });
    c.pageIndex = 3;
    c.onSortChange({ active: 'age', direction: 'desc' });

    expect(c.sort()).toEqual({ active: 'age', direction: 'desc' });
    expect(c.pageIndex).toBe(0);
    expect(c.page).toBe(1);
    expect(onChange).toHaveBeenCalledOnce();
  });

  it('does not notify on sort/page changes in client mode', () => {
    const onChange = vi.fn();
    const c = makeController({ mode: 'client', onChange });
    c.onSortChange({ active: 'age', direction: 'asc' });
    c.onPageChange({ pageIndex: 1, pageSize: 50, length: 0 });
    expect(onChange).not.toHaveBeenCalled();
  });

  it('round-trips sort through prefs only when persistSort is on', () => {
    const withSort = makeController({ persistSort: true });
    withSort.onSortChange({ active: 'age', direction: 'desc' });

    const reloaded = makeController({ persistSort: true });
    reloaded.restore();
    expect(reloaded.sort()).toEqual({ active: 'age', direction: 'desc' });

    localStorage.clear();
    const noPersist = makeController();
    noPersist.onSortChange({ active: 'age', direction: 'desc' });
    noPersist.onColumnVisibilityToggled('age'); // force a persist so there is a payload to inspect
    const saved = JSON.parse(localStorage.getItem('hekutenantcoreapp.columnPrefs.spec')!);
    expect(saved.sortActive).toBeUndefined();
  });

  it('exportColumns follows displayed order, skips hidden and non-exportable columns', () => {
    const c = makeController();
    expect(c.exportColumns().map((col) => col.key)).toEqual(['name', 'age']); // 'created' has no exportValue

    c.onColumnsReordered(['age', 'name']);
    c.onColumnVisibilityToggled('name');
    expect(c.exportColumns().map((col) => col.key)).toEqual(['age']);
  });

  it('falls back to the column key when header is omitted (headerless client tables)', () => {
    const c = new DataTableController<Row>({
      tableKey: 'spec',
      mode: 'client',
      columns: [
        { key: 'name', sortable: true, sortValue: (r) => r.name },
        { key: 'age', header: () => 'Age', exportValue: (r) => String(r.age) },
      ],
    });
    expect(c.reorderableColumns().map((x) => x.label)).toEqual(['name', 'Age']);
    // 'name' has no exportValue so it is left out; 'age' still carries its explicit header
    expect(c.exportColumns().map((x) => x.header)).toEqual(['Age']);
  });

  it('client-mode sortingDataAccessor prefers sortValue, then sortKey, then key', () => {
    const c = new DataTableController<Row>({
      tableKey: 'spec',
      mode: 'client',
      columns: [
        { key: 'name', header: () => 'Name' },
        { key: 'when', header: () => 'When', sortKey: 'created' },
        { key: 'label', header: () => 'Label', sortValue: (r) => r.age },
      ],
    });
    const accessor = c.dataSource.sortingDataAccessor;
    const row: Row = { name: 'Ada', age: 42, created: '2026-01-01' };

    expect(accessor(row, 'name')).toBe('Ada');
    expect(accessor(row, 'when')).toBe('2026-01-01'); // via sortKey -> row.created
    expect(accessor(row, 'label')).toBe(42); // via sortValue
  });
});
