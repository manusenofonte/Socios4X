import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { AdminAuthService, AdminUser } from './admin-auth';

export interface TemporaryPasswordResponse {
  user: AdminUser;
  temporaryPassword: string;
}

@Injectable({ providedIn: 'root' })
export class AdminUsersService {
  private readonly apiUrl = `${environment.apiUrl}/admin-users`;

  constructor(private http: HttpClient, private auth: AdminAuthService) {}

  list(): Observable<AdminUser[]> {
    return this.http.get<AdminUser[]>(this.apiUrl, { headers: this.auth.headers() });
  }

  create(email: string, name: string): Observable<TemporaryPasswordResponse> {
    return this.http.post<TemporaryPasswordResponse>(this.apiUrl, { email, name }, { headers: this.auth.headers() });
  }

  resetPassword(id: number): Observable<TemporaryPasswordResponse> {
    return this.http.post<TemporaryPasswordResponse>(`${this.apiUrl}/${id}/reset-password`, {}, { headers: this.auth.headers() });
  }

  setActive(id: number, isActive: boolean): Observable<AdminUser> {
    return this.http.put<AdminUser>(`${this.apiUrl}/${id}/active`, { isActive }, { headers: this.auth.headers() });
  }
}
