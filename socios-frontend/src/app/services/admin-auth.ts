import { Injectable, computed, signal } from '@angular/core';

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
}
