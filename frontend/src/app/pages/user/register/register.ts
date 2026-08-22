import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { ReactiveFormsModule, FormBuilder, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { TranslocoModule, TranslocoService } from '@jsverse/transloco';
import { Auth, TenantSummary } from '../../../services/auth';
import { TenantManagement } from '../../../services/tenant-management';
import { GoogleSignInButton } from '../../../components/google-sign-in-button/google-sign-in-button';

function passwordsMatchValidator(control: AbstractControl): ValidationErrors | null {
  const password = control.get('password')?.value;
  const confirmPassword = control.get('confirmPassword')?.value;
  return password === confirmPassword ? null : { passwordMismatch: true };
}

@Component({
  selector: 'app-register',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatCardModule,
    TranslocoModule,
    RouterLink,
    GoogleSignInButton
  ],
  templateUrl: './register.html',
  styleUrl: './register.scss',
})
export class Register implements OnInit {
  private fb = inject(FormBuilder);
  private auth = inject(Auth);
  private tenantManagement = inject(TenantManagement);
  private router = inject(Router);
  private transloco = inject(TranslocoService);

  errorMessage = signal('');
  tenants = signal<TenantSummary[]>([]);
  tenantSelectionEnabled = signal(true);

  form = this.fb.group({
    userName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]],
    confirmPassword: ['', Validators.required],
    tenantId: [null as number | null, Validators.required]
  }, { validators: passwordsMatchValidator });

  ngOnInit(): void {
    this.auth.getRegistrationTenantInfo().subscribe({
      next: (info) => {
        this.tenantSelectionEnabled.set(info.tenantSelectionEnabled);
        if (!info.tenantSelectionEnabled) {
          this.form.controls.tenantId.setValue(info.defaultTenantId);
          this.form.controls.tenantId.clearValidators();
          this.form.controls.tenantId.updateValueAndValidity();
        } else {
          this.tenantManagement.getActiveTenants().subscribe({
            next: (data) => this.tenants.set(data)
          });
        }
      }
    });
  }

  onSubmit(): void {
    if (this.form.invalid) return;

    const { userName, email, password, tenantId } = this.form.value;

    this.auth.register(userName!, email!, password!, tenantId!).subscribe({
      next: (res) => this.router.navigate([res.tenantId != null ? '/' : '/tenant-picker']),
      error: (err) => {
        if (err.status === 0) {
          this.errorMessage.set(this.transloco.translate('common.networkError'));
        } else {
          this.errorMessage.set(err.error || this.transloco.translate('auth.registrationFailed'));
        }
      }
    });
  }

  onGoogleCredential(idToken: string): void {
    const tenantId = this.form.value.tenantId ?? undefined;
    this.auth.loginWithGoogle(idToken, tenantId ?? undefined).subscribe({
      next: (res) => this.router.navigate([res.tenantId != null ? '/' : '/tenant-picker']),
      error: (err) => {
        if (err.status === 0) {
          this.errorMessage.set(this.transloco.translate('common.networkError'));
        } else {
          this.errorMessage.set(err.error || this.transloco.translate('auth.registrationFailed'));
        }
      }
    });
  }
}