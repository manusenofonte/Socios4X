import { Injectable, computed, signal } from '@angular/core';
import { HttpHeaders } from '@angular/common/http';

@Injectable({ providedIn: 'root' })
export class AdminAuthService {
  private readonly _key = signal<string | null>(null);

  readonly key = this._key.asReadonly();
  readonly isAuthenticated = computed(() => this._key() !== null);

  setKey(key: string): void {
    this._key.set(key);
  }

  clear(): void {
    this._key.set(null);
  }

  // Headers para los endpoints protegidos del panel de administración
  headers(): HttpHeaders {
    return new HttpHeaders({ 'X-Admin-Key': this._key() ?? '' });
  }
}
