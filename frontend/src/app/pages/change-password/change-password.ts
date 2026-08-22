import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { Router } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { HttpClient } from '@angular/common/http';
import { Auth } from '../../services/auth';
import { environment } from '../../../environments/environment';

function passwordsMatchValidator(control: AbstractControl): ValidationErrors | null {
  const password = control.get('newPassword')?.value;
  const confirm = control.get('confirmPassword')?.value;
  return password === confirm ? null : { passwordMismatch: true };
}

@Component({
  selector: 'app-change-password',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    TranslocoModule
  ],
  templateUrl: './change-password.html',
  styleUrl: './change-password.scss',
})
export class ChangePassword {
  private fb = inject(FormBuilder);
  private auth = inject(Auth);
  private router = inject(Router);
  private http = inject(HttpClient);
  private transloco = inject(TranslocoService);

  errorMessage = signal('');
  successMessage = signal('');

  form = this.fb.group({
    currentPassword: ['', Validators.required],
    newPassword: ['', [Validators.required, Validators.minLength(8)]],
    confirmPassword: ['', Validators.required]
  }, { validators: passwordsMatchValidator });

  onSubmit(): void {
    if (this.form.invalid) return;

    const { currentPassword, newPassword } = this.form.value;

    this.http.put(
      `${environment.apiUrl}/user/change-password`,
      { currentPassword, newPassword, confirmPassword: this.form.value.confirmPassword }      
    ).subscribe({
      next: () => {
        const current = this.auth.currentUser();
        if (current) {
          const updated = { ...current, mustChangePassword: false };
          this.auth.currentUser.set(updated);
          localStorage.setItem('hekutenantcoreapp_user', JSON.stringify(updated));
        }
        this.router.navigate(['/dashboard']);
      },
      error: (err) => {
        this.errorMessage.set(err.error || this.transloco.translate('auth.changePasswordFailed'));
      }
    });
  }
}