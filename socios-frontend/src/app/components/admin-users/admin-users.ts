import { Component, ChangeDetectorRef, EventEmitter, OnInit, Output } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminAuthService, AdminUser } from '../../services/admin-auth';
import { AdminUsersService, TemporaryPasswordResponse } from '../../services/admin-users';

// Pestaña "Administradores": solo la ve el administrador principal
@Component({
  selector: 'app-admin-users',
  standalone: true,
  imports: [DatePipe, FormsModule],
  templateUrl: './admin-users.html',
  styleUrl: './admin-users.css'
})
export class AdminUsersComponent implements OnInit {
  @Output() unauthorized = new EventEmitter<void>();

  users: AdminUser[] = [];
  loading = false;
  loadError = '';
  actionError = '';
  busyId: number | null = null;

  newEmail = '';
  newName = '';
  creating = false;

  // Contraseña temporal recién generada: se muestra una sola vez
  issued: TemporaryPasswordResponse | null = null;
  copied = false;

  constructor(
    private usersService: AdminUsersService,
    private auth: AdminAuthService,
    private cdr: ChangeDetectorRef
  ) {}

  get currentUserId(): number | undefined {
    return this.auth.user()?.id;
  }

  ngOnInit(): void {
    this.fetchUsers();
  }

  fetchUsers(): void {
    this.loading = true;
    this.loadError = '';

    this.usersService.list().subscribe({
      next: (users) => {
        this.users = users;
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.loading = false;
        if (!this.handleAuthError(err)) {
          this.loadError = 'No se pudieron cargar los administradores.';
        }
        this.cdr.detectChanges();
      }
    });
  }

  create(): void {
    if (!this.newEmail.trim() || this.creating) return;

    this.creating = true;
    this.actionError = '';

    this.usersService.create(this.newEmail.trim(), this.newName.trim()).subscribe({
      next: (response) => {
        this.creating = false;
        this.newEmail = this.newName = '';
        this.showIssued(response);
        this.fetchUsers();
      },
      error: (err) => {
        this.creating = false;
        this.reportError(err, 'No se pudo crear el administrador.');
      }
    });
  }

  resetPassword(user: AdminUser): void {
    if (this.busyId !== null) return;
    if (!window.confirm(`¿Generar una contraseña temporal nueva para ${user.email}?\n\nLa contraseña actual deja de funcionar y se cierran sus sesiones abiertas.`)) return;

    this.busyId = user.id;
    this.actionError = '';

    this.usersService.resetPassword(user.id).subscribe({
      next: (response) => {
        this.busyId = null;
        this.showIssued(response);
        this.fetchUsers();
      },
      error: (err) => {
        this.busyId = null;
        this.reportError(err, 'No se pudo blanquear la contraseña.');
      }
    });
  }

  toggleActive(user: AdminUser): void {
    if (this.busyId !== null) return;

    const activate = !user.isActive;
    const question = activate
      ? `¿Reactivar a ${user.email}?`
      : `¿Desactivar a ${user.email}?\n\nNo va a poder ingresar al panel y se cierran sus sesiones abiertas.`;
    if (!window.confirm(question)) return;

    this.busyId = user.id;
    this.actionError = '';

    this.usersService.setActive(user.id, activate).subscribe({
      next: () => {
        this.busyId = null;
        this.fetchUsers();
      },
      error: (err) => {
        this.busyId = null;
        this.reportError(err, 'No se pudo actualizar el administrador.');
      }
    });
  }

  copyPassword(): void {
    if (!this.issued) return;
    navigator.clipboard.writeText(this.issued.temporaryPassword);
    this.copied = true;
    this.cdr.detectChanges();
  }

  dismissIssued(): void {
    this.issued = null;
    this.copied = false;
  }

  private showIssued(response: TemporaryPasswordResponse): void {
    this.issued = response;
    this.copied = false;
    this.cdr.detectChanges();
  }

  private reportError(err: { status?: number; error?: { error?: string } }, fallback: string): void {
    if (this.handleAuthError(err)) return;

    this.actionError = err.status === 429
      ? 'Demasiados intentos seguidos. Esperá un minuto y probá de nuevo.'
      : err.error?.error ?? fallback;
    this.cdr.detectChanges();
  }

  private handleAuthError(err: { status?: number }): boolean {
    if (err.status === 401 || err.status === 403) {
      this.unauthorized.emit();
      return true;
    }
    return false;
  }
}
