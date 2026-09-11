import { Component, DestroyRef, effect, inject, input, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { Content } from '../../services/content';

// Small read-only counterpart to AvatarUpload — for a *list* of owners (a grid column, a report)
// rather than a single edit view. No upload/remove affordance, just a display of whichever
// content item the caller resolved for that row (typically via
// IContentService.GetSlotsForOwnersAsync's one batch call for the whole page, not one
// GetSlotAsync per row). Same authenticated-image handling as AvatarUpload: the download
// endpoint requires a bearer token that a plain <img src> wouldn't send, so this fetches the
// blob through HttpClient and binds an object URL instead.
@Component({
  selector: 'app-content-thumbnail',
  imports: [CommonModule, MatIconModule],
  templateUrl: './content-thumbnail.html',
  styleUrl: './content-thumbnail.scss',
})
export class ContentThumbnail {
  private content = inject(Content);
  private destroyRef = inject(DestroyRef);

  contentId = input<number | null>(null);

  displayUrl = signal<string | null>(null);

  private currentObjectUrl: string | null = null;

  constructor() {
    effect(() => this.loadImage(this.contentId()));
    this.destroyRef.onDestroy(() => this.releaseObjectUrl());
  }

  private loadImage(id: number | null): void {
    this.releaseObjectUrl();

    if (id == null) {
      this.displayUrl.set(null);
      return;
    }

    this.content.fetchImage(id).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        this.currentObjectUrl = url;
        this.displayUrl.set(url);
      },
      error: () => this.displayUrl.set(null)
    });
  }

  private releaseObjectUrl(): void {
    if (this.currentObjectUrl) {
      URL.revokeObjectURL(this.currentObjectUrl);
      this.currentObjectUrl = null;
    }
  }
}
