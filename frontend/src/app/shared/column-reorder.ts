import { CdkDragDrop, moveItemInArray } from '@angular/cdk/drag-drop';

// Shared by the ColumnReorder chip list — moves the dragged/dropped pair within an array.
// Session-only by design (callers just re-set their signal; nothing here persists the order).
export function reorderColumns<T>(items: T[], event: CdkDragDrop<T[]>): T[] {
  const updated = [...items];
  moveItemInArray(updated, event.previousIndex, event.currentIndex);
  return updated;
}
