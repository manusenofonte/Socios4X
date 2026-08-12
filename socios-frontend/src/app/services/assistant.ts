import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface AskQuestionResponse {
  answer: string;
}

export interface FaqSuggestion {
  id: number;
  question: string;
}

@Injectable({ providedIn: 'root' })
export class AssistantService {
  private readonly apiUrl = `${environment.apiUrl}/assistant`;
  private readonly faqUrl = `${environment.apiUrl}/faq`;

  constructor(private http: HttpClient) {}

  ask(query: string, clubId?: number): Observable<AskQuestionResponse> {
    return this.http.post<AskQuestionResponse>(`${this.apiUrl}/ask`, { query, clubId });
  }

  getSuggestions(count = 4): Observable<FaqSuggestion[]> {
    return this.http.get<FaqSuggestion[]>(`${this.faqUrl}/suggestions?count=${count}`);
  }
}
