import { Injectable, computed, signal } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';

export interface AdminUser {
  id: number;
  email: string;
  name: string;
  role: 'SuperAdmin' | 'Admin';
  isActive: boolean;
  mustChangePassword: boolean;
  createdAt: string;
  lastLoginAt: string | null;
}

export interface AdminSession {
  token: string;
  expiresAt: string;
  user: AdminUser;
}

export interface MessageResponse {
  message: string;
}

// Requisitos de contraseña (los mismos que valida el backend)
export const PASSWORD_MIN_LENGTH = 10;

export function passwordPolicyError(password: string): string | null {
  if (password.length < PASSWORD_MIN_LENGTH) {
    return `La contraseña debe tener al menos ${PASSWORD_MIN_LENGTH} caracteres.`;
  }
  if (!/\p{L}/u.test(password) || !/\d/.test(password)) {
    return 'La contraseña debe tener al menos una letra y un número.';
  }
  return null;
}

@Injectable({ providedIn: 'root' })
export class AdminAuthService {
  // sessionStorage: la sesión sobrevive a recargar la página, pero se borra al cerrar la pestaña
  private static readonly STORAGE_KEY = 'socios-admin-session';

  private readonly apiUrl = `${environment.apiUrl}/auth`;
  private readonly _session = signal<AdminSession | null>(this.loadSession());

  readonly user = computed(() => this._session()?.user ?? null);
  readonly isAuthenticated = computed(() => this._session() !== null);
  readonly mustChangePassword = computed(() => this.user()?.mustChangePassword ?? false);
  readonly isSuperAdmin = computed(() => this.user()?.role === 'SuperAdmin');

  constructor(private http: HttpClient) {}

  login(email: string, password: string): Observable<AdminSession> {
    return this.http
      .post<AdminSession>(`${this.apiUrl}/login`, { email, password })
      .pipe(tap((session) => this.setSession(session)));
  }

  changePassword(currentPassword: string, newPassword: string): Observable<AdminSession> {
    return this.http
      .post<AdminSession>(`${this.apiUrl}/change-password`, { currentPassword, newPassword }, { headers: this.headers() })
      .pipe(tap((session) => this.setSession(session)));
  }

  forgotPassword(email: string): Observable<MessageResponse> {
    return this.http.post<MessageResponse>(`${this.apiUrl}/forgot-password`, { email });
  }

  resetPassword(token: string, newPassword: string): Observable<MessageResponse> {
    return this.http.post<MessageResponse>(`${this.apiUrl}/reset-password`, { token, newPassword });
  }

  clear(): void {
    this._session.set(null);
    try {
      sessionStorage.removeItem(AdminAuthService.STORAGE_KEY);
    } catch { /* sin storage disponible no hay nada que borrar */ }
  }

  // Headers para los endpoints protegidos del panel de administración
  headers(): HttpHeaders {
    const token = this._session()?.token;
    return token ? new HttpHeaders({ Authorization: `Bearer ${token}` }) : new HttpHeaders();
  }

  private setSession(session: AdminSession): void {
    this._session.set(session);
    try {
      sessionStorage.setItem(AdminAuthService.STORAGE_KEY, JSON.stringify(session));
    } catch { /* si no se puede guardar, la sesión dura hasta recargar */ }
  }

  private loadSession(): AdminSession | null {
    try {
      const raw = sessionStorage.getItem(AdminAuthService.STORAGE_KEY);
      if (!raw) return null;

      const session = JSON.parse(raw) as AdminSession;
      return new Date(session.expiresAt).getTime() > Date.now() ? session : null;
    } catch {
      return null;
    }
  }
}
