import { Component, ChangeDetectorRef, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AdminAuthService, PASSWORD_MIN_LENGTH, passwordPolicyError } from '../../services/admin-auth';

// Pantalla a la que lleva el link del mail de "Olvidé mi contraseña"
@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './reset-password.html'
})
export class ResetPasswordComponent implements OnInit {
  readonly minLength = PASSWORD_MIN_LENGTH;

  token = '';
  newPassword = '';
  confirmPassword = '';
  submitting = false;
  error = '';
  successMessage = '';

  constructor(
    private route: ActivatedRoute,
    private auth: AdminAuthService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.token = this.route.snapshot.queryParamMap.get('token') ?? '';
    if (!this.token) {
      this.error = 'El link no es válido. Pedí uno nuevo desde "Olvidé mi contraseña".';
    }
  }

  submit(): void {
    if (this.submitting || !this.token) return;

    const policyError = passwordPolicyError(this.newPassword);
    if (policyError) {
      this.error = policyError;
      return;
    }
    if (this.newPassword !== this.confirmPassword) {
      this.error = 'Las contraseñas no coinciden.';
      return;
    }

    this.submitting = true;
    this.error = '';

    this.auth.resetPassword(this.token, this.newPassword).subscribe({
      next: (response) => {
        this.submitting = false;
        this.successMessage = response.message;
        this.newPassword = this.confirmPassword = '';
        // Si había una sesión abierta en esta pestaña, ya no vale
        this.auth.clear();
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.submitting = false;
        this.error = err.status === 429
          ? 'Demasiados intentos seguidos. Esperá un minuto y probá de nuevo.'
          : err.error?.error ?? 'No se pudo restablecer la contraseña. Intentá de nuevo.';
        this.cdr.detectChanges();
      }
    });
  }
}
