import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { PagedResult } from '../models/paged-result';
import { AdminAuthService } from './admin-auth';

export interface UnansweredQuestion {
  id: number;
  question: string;
  clubId: number | null;
  createdAt: string;
}

@Injectable({ providedIn: 'root' })
export class AdminUnansweredQuestionsService {
  private readonly apiUrl = `${environment.apiUrl}/unanswered-questions`;

  constructor(private http: HttpClient, private auth: AdminAuthService) {}

  list(page: number, pageSize: number): Observable<PagedResult<UnansweredQuestion>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<PagedResult<UnansweredQuestion>>(this.apiUrl, { headers: this.auth.headers(), params });
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`, { headers: this.auth.headers() });
  }
}
