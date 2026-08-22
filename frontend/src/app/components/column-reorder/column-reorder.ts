import { Component, input, output } from '@angular/core';
import { DragDropModule, CdkDragDrop } from '@angular/cdk/drag-drop';
import { MatIconModule } from '@angular/material/icon';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { reorderColumns } from '../../shared/column-reorder';

export interface ReorderableColumn {
  key: string;
  label: string;
  hidden: boolean;
}

// Draggable, hide/show-able chip list for a table's columns. Deliberately NOT built on top of
// the table's own header cells — cdkDropList can't discover cdkDrag directives rendered through
// mat-table's matHeaderCellDef machinery (its cells aren't reachable via the @ContentChildren
// query CdkDropList relies on), so dragging the real <th> elements never fires a drop. This list
// is plain divs in its own template, which CDK drag-drop supports natively. Dragging is
// restricted to the handle icon (cdkDragHandle) so the checkbox stays a normal, independently
// clickable control instead of fighting the drag gesture.
@Component({
  selector: 'app-column-reorder',
  imports: [DragDropModule, MatIconModule, MatCheckboxModule],
  templateUrl: './column-reorder.html',
  styleUrl: './column-reorder.scss',
})
export class ColumnReorder {
  columns = input.required<ReorderableColumn[]>();
  orderChange = output<string[]>();
  visibilityToggle = output<string>();

  onDrop(event: CdkDragDrop<ReorderableColumn[]>): void {
    const reordered = reorderColumns(this.columns(), event);
    this.orderChange.emit(reordered.map((c) => c.key));
  }
}
