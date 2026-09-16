import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, Subscription } from 'rxjs';
import { environment } from '../../environments/environment';
import { parseSseEvents } from './sse';

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

  // Emite la respuesta en partes a medida que la IA la genera. Usa fetch porque
  // HttpClient no permite leer un POST en streaming.
  askStream(query: string, clubId?: number): Observable<string> {
    return new Observable<string>((subscriber) => {
      const controller = new AbortController();
      let fallback: Subscription | undefined;

      const run = async () => {
        const response = await fetch(`${this.apiUrl}/ask/stream`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json', Accept: 'text/event-stream' },
          body: JSON.stringify({ query, clubId }),
          signal: controller.signal
        });

        // Backend todavía sin streaming (p. ej. mientras se despliega): respuesta completa
        if (response.status === 404 || response.status === 405) {
          fallback = this.ask(query, clubId).subscribe({
            next: (r) => subscriber.next(r.answer),
            error: (err) => subscriber.error(err),
            complete: () => subscriber.complete()
          });
          return;
        }

        if (!response.ok || !response.body) {
          throw new Error(`HTTP ${response.status}`);
        }

        const reader = response.body.pipeThrough(new TextDecoderStream()).getReader();
        let buffer = '';

        while (true) {
          const { value, done } = await reader.read();
          if (done) break;

          const parsed = parseSseEvents(buffer + value);
          buffer = parsed.rest;

          for (const event of parsed.events) {
            if (event.event === 'delta') {
              subscriber.next(JSON.parse(event.data).text);
            } else if (event.event === 'done') {
              subscriber.complete();
              return;
            } else if (event.event === 'error') {
              throw new Error(JSON.parse(event.data).error);
            }
          }
        }

        throw new Error('La conexión se cortó antes de terminar la respuesta.');
      };

      run().catch((err) => {
        if (!controller.signal.aborted) subscriber.error(err);
      });

      return () => {
        controller.abort();
        fallback?.unsubscribe();
      };
    });
  }

  getSuggestions(count = 10): Observable<FaqSuggestion[]> {
    return this.http.get<FaqSuggestion[]>(`${this.faqUrl}/suggestions?count=${count}`);
  }
}
