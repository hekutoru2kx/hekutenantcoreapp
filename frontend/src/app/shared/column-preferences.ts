export interface ColumnPreferences {
  order: string[];
  hidden: string[];
  sortActive?: string;
  sortDirection?: string;
}

const STORAGE_PREFIX = 'hekutenantcoreapp.columnPrefs.';

// Per-table column order/visibility/sort, remembered per browser — deliberately client-only
// (no backend round trip, nothing to migrate). Wrapped in try/catch since localStorage can
// throw (private browsing, quota, disabled by the user) — a boundary the app doesn't control,
// so preferences are best-effort rather than something a failure here should surface as an error.
export function loadColumnPreferences(tableKey: string): ColumnPreferences | null {
  try {
    const raw = localStorage.getItem(STORAGE_PREFIX + tableKey);
    return raw ? JSON.parse(raw) : null;
  } catch {
    return null;
  }
}

export function saveColumnPreferences(tableKey: string, prefs: ColumnPreferences): void {
  try {
    localStorage.setItem(STORAGE_PREFIX + tableKey, JSON.stringify(prefs));
  } catch {
    // Best-effort — losing persistence isn't worth surfacing to the admin.
  }
}
