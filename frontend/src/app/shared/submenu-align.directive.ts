import { Directive, OnDestroy, OnInit, inject } from '@angular/core';
import { MatMenuTrigger } from '@angular/material/menu';
import { Subscription } from 'rxjs';

// Material nudges a submenu panel vertically by its parent menu's first-item
// offset, to compensate for the parent panel's own inner padding. That
// heuristic assumes the parent menu starts right at its first mat-menu-item;
// it breaks when the parent has non-item content above the list (like this
// nav's user-info header), over-correcting and popping the submenu up well
// above the row that triggered it. Cancel just that vertical nudge so the
// submenu opens level with its trigger, and cap its height so it scrolls
// instead of overflowing the viewport if it still doesn't fit.
@Directive({
  selector: '[appSubmenuAlign]',
})
export class SubmenuAlignDirective implements OnInit, OnDestroy {
  private readonly trigger = inject(MatMenuTrigger, { self: true });
  private subscription?: Subscription;

  ngOnInit(): void {
    this.subscription = this.trigger.menuOpened.subscribe(() => this.alignPanel());
  }

  ngOnDestroy(): void {
    this.subscription?.unsubscribe();
  }

  private alignPanel(): void {
    requestAnimationFrame(() => {
      const pane = this.findSubmenuPane();
      if (!pane) {
        return;
      }

      const matrix = /^matrix\(([^,]+), ?([^,]+), ?([^,]+), ?([^,]+), ?([^,]+), ?([^)]+)\)$/.exec(
        getComputedStyle(pane).transform,
      );
      if (matrix) {
        const [, a, b, c, d, tx] = matrix;
        pane.style.transform = `matrix(${a}, ${b}, ${c}, ${d}, ${tx}, 0)`;
      }

      const margin = 8;
      const availableHeight = window.innerHeight - pane.getBoundingClientRect().top - margin;
      const panel = pane.querySelector<HTMLElement>('.mat-mdc-menu-panel');
      if (panel) {
        panel.style.maxHeight = `${Math.max(availableHeight, 100)}px`;
        panel.style.overflowY = 'auto';
      }
    });
  }

  private findSubmenuPane(): HTMLElement | undefined {
    const panes = document.querySelectorAll<HTMLElement>('.cdk-overlay-pane');
    return Array.from(panes)
      .reverse()
      .find((candidate) => candidate.querySelector('.mat-mdc-menu-panel'));
  }
}
