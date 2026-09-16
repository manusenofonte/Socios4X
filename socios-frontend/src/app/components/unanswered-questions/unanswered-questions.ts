import { Component, OnInit, ChangeDetectorRef, EventEmitter, Output } from '@angular/core';
import { DatePipe } from '@angular/common';
import { AdminUnansweredQuestionsService, UnansweredQuestion } from '../../services/admin-unanswered-questions';
import { PaginatorComponent } from '../paginator/paginator';

// Pestaña "Preguntas sin respuesta" del panel de administración
@Component({
  selector: 'app-unanswered-questions',
  standalone: true,
  imports: [DatePipe, PaginatorComponent],
  templateUrl: './unanswered-questions.html',
  styleUrl: './unanswered-questions.css'
})
export class UnansweredQuestionsComponent implements OnInit {
  // La clave dejó de ser válida: el panel vuelve a pedirla
  @Output() unauthorized = new EventEmitter<void>();

  questions: UnansweredQuestion[] = [];
  loading = false;
  loadError = '';
  actionError = '';
  deletingId: number | null = null;

  readonly pageSize = 10;
  page = 1;
  totalCount = 0;

  constructor(
    private questionsService: AdminUnansweredQuestionsService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.fetchQuestions(1);
  }

  fetchQuestions(page = this.page): void {
    this.loading = true;
    this.loadError = '';

    this.questionsService.list(page, this.pageSize).subscribe({
      next: (result) => {
        // Si la página quedó vacía (p. ej. se borró la última pregunta de la última
        // página), saltamos a la última página que todavía tiene preguntas.
        const lastPage = Math.max(1, Math.ceil(result.totalCount / this.pageSize));
        if (result.items.length === 0 && page > lastPage) {
          this.fetchQuestions(lastPage);
          return;
        }

        this.questions = result.items;
        this.page = result.page;
        this.totalCount = result.totalCount;
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.loading = false;
        if (err.status === 401) {
          this.unauthorized.emit();
          return;
        }
        this.loadError = 'No se pudieron cargar las preguntas.';
        this.cdr.detectChanges();
      }
    });
  }

  confirmDelete(question: UnansweredQuestion): void {
    if (this.deletingId !== null) return;

    const preview = question.question.length > 80 ? `${question.question.slice(0, 80)}…` : question.question;
    if (!window.confirm(`¿Eliminar "${preview}" del listado?\n\nHacelo cuando ya hayas cargado la información que la responde.`)) return;

    this.deletingId = question.id;
    this.actionError = '';

    this.questionsService.delete(question.id).subscribe({
      next: () => {
        this.deletingId = null;
        this.fetchQuestions();
      },
      error: (err) => {
        this.deletingId = null;
        if (err.status === 401) {
          this.unauthorized.emit();
          return;
        }
        if (err.status === 404) {
          // Ya la había borrado otro administrador: refrescamos la lista
          this.fetchQuestions();
          return;
        }
        this.actionError = err.status === 429
          ? 'Demasiados intentos seguidos. Esperá un minuto y probá de nuevo.'
          : 'No se pudo eliminar la pregunta. Intentá de nuevo.';
        this.cdr.detectChanges();
      }
    });
  }
}
