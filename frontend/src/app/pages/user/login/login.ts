import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { TranslocoModule } from '@jsverse/transloco';
import { Auth } from '../../../services/auth';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { TranslocoService } from '@jsverse/transloco';
import { GoogleSignInButton } from '../../../components/google-sign-in-button/google-sign-in-button';

@Component({
  selector: 'app-login',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatCardModule,
    TranslocoModule,
    RouterLink,
    GoogleSignInButton
  ],
  templateUrl: './login.html',
  styleUrl: './login.scss',
})

export class Login {
  private fb = inject(FormBuilder);
  private auth = inject(Auth);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private transloco = inject(TranslocoService);

  deactivatedMessage = '';

errorMessage = signal('');

  form = this.fb.group({
    email: ['', Validators.required],
    password: ['', Validators.required]
  });

  onSubmit(): void {
    if (this.form.invalid) return;

    const { email, password } = this.form.value;

    this.auth.login(email!, password!).subscribe({
      next: (res) => this.router.navigate([res.tenantId != null ? '/' : '/tenant-picker']),
      error: (err) => {
      if (err.status === 0) {
        this.errorMessage.set(this.transloco.translate('common.networkError'));
      } else {
        this.errorMessage.set(err.error || this.transloco.translate('auth.invalidCredentials'));
      }
    }
  });
}

  onGoogleCredential(idToken: string): void {
    this.auth.loginWithGoogle(idToken).subscribe({
      next: (res) => this.router.navigate([res.tenantId != null ? '/' : '/tenant-picker']),
      error: (err) => {
        if (err.status === 0) {
          this.errorMessage.set(this.transloco.translate('common.networkError'));
        } else {
          this.errorMessage.set(err.error || this.transloco.translate('auth.invalidCredentials'));
        }
      }
    });
  }

  ngOnInit(): void {
    const reason = this.route.snapshot.queryParams['reason'];
    if (reason === 'deactivated') {
      this.deactivatedMessage = this.transloco.translate('auth.deactivatedMessage');
    }
  } 
}