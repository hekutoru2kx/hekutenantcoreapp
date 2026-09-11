import { Component, DestroyRef, effect, inject, input, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { Content } from '../../services/content';

// A small, reusable upload control for a singleton image slot (see the backend's ContentItem
// "slot" concept) — not the full content list/panel, which no page in this core needs yet.
// The caller owns which feature's route this talks to (`path`, e.g.
// '/user/person/profile-picture') and how the resulting picture is displayed; this component
// only handles picking a file, showing progress/errors, and removal.
//
// Takes `contentId`, not a ready-to-bind URL: the download endpoint is authenticated, and a
// plain <img src> request carries no Authorization header (browsers don't attach one to
// image/navigation requests, only HttpClient does via the auth interceptor) — it would 401.
// So this component fetches the image itself through Content.fetchImage (which goes through
// HttpClient) and binds the resulting blob as an object URL instead.
@Component({
  selector: 'app-avatar-upload',
  imports: [CommonModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule, TranslocoModule],
  templateUrl: './avatar-upload.html',
  styleUrl: './avatar-upload.scss',
})
export class AvatarUpload {
  private content = inject(Content);
  private transloco = inject(TranslocoService);
  private destroyRef = inject(DestroyRef);

  contentId = input<number | null>(null);
  path = input.required<string>();

  uploaded = output<number>();
  removed = output<void>();

  uploading = signal(false);
  errorMessage = signal<string | null>(null);
  displayUrl = signal<string | null>(null);

  private currentObjectUrl: string | null = null;

  constructor() {
    effect(() => this.loadImage(this.contentId()));
    this.destroyRef.onDestroy(() => this.releaseObjectUrl());
  }

  onFileSelected(event: Event): void {
    const target = event.target as HTMLInputElement;
    const file = target.files?.[0];
    target.value = '';
    if (!file) return;

    this.uploading.set(true);
    this.errorMessage.set(null);
    this.content.upload(this.path(), file).subscribe({
      next: (result) => {
        this.uploading.set(false);
        this.uploaded.emit(result.id);
      },
      error: (err) => {
        this.uploading.set(false);
        this.errorMessage.set(this.errorText(err, 'avatarUpload.uploadError'));
      }
    });
  }

  onRemove(): void {
    if (!window.confirm(this.transloco.translate('avatarUpload.confirmRemove'))) return;

    this.uploading.set(true);
    this.errorMessage.set(null);
    this.content.remove(this.path()).subscribe({
      next: () => {
        this.uploading.set(false);
        this.removed.emit();
      },
      error: (err) => {
        this.uploading.set(false);
        this.errorMessage.set(this.errorText(err, 'avatarUpload.removeError'));
      }
    });
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

  // err.error is only ever safe to render directly when the server sent a plain string body
  // (e.g. BadRequest(ex.Message)); anything else — a ProblemDetails object, an accidental
  // non-string BadRequest payload, a network failure with no body — falls back to a translated
  // message instead of rendering "[object Object]".
  private errorText(err: unknown, fallbackKey: string): string {
    const body = (err as { error?: unknown })?.error;
    return typeof body === 'string' && body.length > 0 ? body : this.transloco.translate(fallbackKey);
  }
}
