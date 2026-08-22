export interface ExportColumn<T> {
  key: string;
  header: string; // already-translated label
  getValue: (row: T) => string;
}

// Shared by every admin table's Export button. Builds a CSV from the given columns/rows and
// triggers a browser download — no server round trip beyond whatever already fetched `rows`.
export function exportToCsv<T>(filename: string, columns: ExportColumn<T>[], rows: T[]): void {
  const headerLine = columns.map((c) => escapeCsvValue(c.header)).join(',');
  const dataLines = rows.map((row) => columns.map((c) => escapeCsvValue(c.getValue(row))).join(','));
  const csv = [headerLine, ...dataLines].join('\r\n');

  const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement('a');
  anchor.href = url;
  anchor.download = filename;
  anchor.click();
  URL.revokeObjectURL(url);
}

function escapeCsvValue(value: string): string {
  if (value.includes(',') || value.includes('"') || value.includes('\n')) {
    return `"${value.replace(/"/g, '""')}"`;
  }
  return value;
}
