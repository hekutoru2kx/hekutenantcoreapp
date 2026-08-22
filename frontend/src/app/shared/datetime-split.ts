// Splits/recombines a naive "YYYY-MM-DDTHH:mm[:ss]" datetime string (no timezone designator —
// the format this app's DateTime? fields have always used end-to-end, both from the API and
// from the native datetime-local inputs this replaces) into a separate date + time-of-day pair,
// so the date portion can go through mat-datepicker (locale-independent yyyy-MM-dd display via
// YmdDateAdapter) while the time-of-day stays a plain HH:mm control. Deliberately avoids
// Date.toISOString() (UTC, adds "Z") to keep the exact same naive-local-string wire format the
// backend already expects.
export interface SplitDateTime {
  date: Date | null;
  time: string;
}

export function splitDateTime(value: string | null | undefined): SplitDateTime {
  if (!value) return { date: null, time: '' };

  const d = new Date(value);
  if (isNaN(d.getTime())) return { date: null, time: '' };

  const hh = String(d.getHours()).padStart(2, '0');
  const mm = String(d.getMinutes()).padStart(2, '0');
  return { date: d, time: `${hh}:${mm}` };
}

export function combineDateTime(date: Date | null, time: string): string | null {
  if (!date) return null;

  const [hh, mm] = (time || '00:00').split(':').map(Number);
  const hours = String(hh || 0).padStart(2, '0');
  const minutes = String(mm || 0).padStart(2, '0');
  return `${toYmd(date)}T${hours}:${minutes}`;
}

// Plain date (no time) fields go through mat-datepicker directly; this formats the picked
// Date back to the "yyyy-MM-dd" string those fields are stored/sent as.
export function toYmd(date: Date | null | undefined): string | null {
  if (!date) return null;

  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}
