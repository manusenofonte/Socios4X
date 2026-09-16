import { Component, ChangeDetectorRef, EventEmitter, Input, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AdminAuthService, PASSWORD_MIN_LENGTH, passwordPolicyError } from '../../services/admin-auth';

// Formulario para cambiar la contraseña. Con [forced] es la pantalla obligatoria de quien
// entró con una contraseña temporal; sin él, es la opción "Cambiar contraseña" del panel.
@Component({
  selector: 'app-change-password',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './change-password.html'
})
export class ChangePasswordComponent {
  @Input() forced = false;

  @Output() changed = new EventEmitter<void>();
  @Output() cancelled = new EventEmitter<void>();

  readonly minLength = PASSWORD_MIN_LENGTH;

  currentPassword = '';
  newPassword = '';
  confirmPassword = '';
  submitting = false;
  error = '';

  constructor(private auth: AdminAuthService, private cdr: ChangeDetectorRef) {}

  submit(): void {
    if (this.submitting) return;

    const policyError = passwordPolicyError(this.newPassword);
    if (policyError) {
      this.error = policyError;
      return;
    }
    if (this.newPassword !== this.confirmPassword) {
      this.error = 'Las contraseñas nuevas no coinciden.';
      return;
    }

    this.submitting = true;
    this.error = '';

    this.auth.changePassword(this.currentPassword, this.newPassword).subscribe({
      next: () => {
        this.submitting = false;
        this.currentPassword = this.newPassword = this.confirmPassword = '';
        this.changed.emit();
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.submitting = false;
        if (err.status === 401) {
          this.auth.clear();
          return;
        }
        this.error = err.status === 429
          ? 'Demasiados intentos seguidos. Esperá un minuto y probá de nuevo.'
          : err.error?.error ?? 'No se pudo cambiar la contraseña. Intentá de nuevo.';
        this.cdr.detectChanges();
      }
    });
  }
}
