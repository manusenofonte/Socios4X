import { Component, ChangeDetectorRef, EventEmitter, Input, Output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AdminAuthService } from '../../services/admin-auth';

// Pantalla de ingreso al panel, con la opción "Olvidé mi contraseña"
@Component({
  selector: 'app-admin-login',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './admin-login.html'
})
export class AdminLoginComponent {
  // Aviso de por qué se volvió a esta pantalla (p. ej. sesión vencida)
  @Input() notice = '';

  @Output() loggedIn = new EventEmitter<void>();

  mode: 'login' | 'forgot' = 'login';

  email = '';
  password = '';
  submitting = false;
  error = '';
  forgotMessage = '';

  constructor(private auth: AdminAuthService, private cdr: ChangeDetectorRef) {}

  submitLogin(): void {
    if (!this.email.trim() || !this.password || this.submitting) return;

    this.submitting = true;
    this.error = '';

    this.auth.login(this.email.trim(), this.password).subscribe({
      next: () => {
        this.submitting = false;
        this.password = '';
        this.loggedIn.emit();
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.submitting = false;
        this.password = '';
        this.error = err.status === 429
          ? 'Demasiados intentos seguidos. Esperá un minuto y probá de nuevo.'
          : err.error?.error ?? 'No se pudo conectar con el servidor.';
        this.cdr.detectChanges();
      }
    });
  }

  submitForgot(): void {
    if (!this.email.trim() || this.submitting) return;

    this.submitting = true;
    this.error = '';

    this.auth.forgotPassword(this.email.trim()).subscribe({
      next: (response) => {
        this.submitting = false;
        this.forgotMessage = response.message;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.submitting = false;
        this.error = err.status === 429
          ? 'Demasiados intentos seguidos. Esperá un minuto y probá de nuevo.'
          : 'No se pudo conectar con el servidor.';
        this.cdr.detectChanges();
      }
    });
  }

  showForgot(): void {
    this.mode = 'forgot';
    this.error = '';
    this.forgotMessage = '';
  }

  showLogin(): void {
    this.mode = 'login';
    this.error = '';
  }
}
