import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, ActivatedRoute } from '@angular/router';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { Auth } from '../../../services/auth';

type Mode = 'confirming' | 'confirmed' | 'failed' | 'prompt';

@Component({
  selector: 'app-confirm-email',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatCardModule,
    MatProgressBarModule,
    TranslocoModule
  ],
  templateUrl: './confirm-email.html',
  styleUrl: './confirm-email.scss',
})
export class ConfirmEmail {
  private fb = inject(FormBuilder);
  private auth = inject(Auth);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private transloco = inject(TranslocoService);

  mode = signal<Mode>('prompt');
  errorMessage = signal('');
  resendSent = signal(false);
  resending = signal(false);

  form = this.fb.group({
    email: ['', [Validators.required, Validators.email]]
  });

  ngOnInit(): void {
    const params = this.route.snapshot.queryParams;
    const userId = params['userId'];
    const token = params['token'];
    const email = params['email'];

    if (email) this.form.patchValue({ email });

    if (userId && token) {
      this.mode.set('confirming');
      this.auth.confirmEmail(userId, token).subscribe({
        next: () => this.mode.set('confirmed'),
        error: (err) => {
          this.mode.set('failed');
          this.errorMessage.set(
            err.status === 0
              ? this.transloco.translate('common.networkError')
              : err.error || this.transloco.translate('auth.confirmEmail.failed')
          );
        }
      });
    }
  }

  resend(): void {
    if (this.form.invalid) return;
    this.resending.set(true);
    this.resendSent.set(false);
    this.auth.resendConfirmation(this.form.value.email!).subscribe({
      next: () => {
        this.resending.set(false);
        this.resendSent.set(true);
      },
      error: () => {
        // The endpoint always succeeds; treat any transport error as "sent" to avoid
        // leaking whether the address is registered.
        this.resending.set(false);
        this.resendSent.set(true);
      }
    });
  }

  goToLogin(): void {
    this.router.navigate(['/login']);
  }
}
