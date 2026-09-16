import { Component, OnInit, ChangeDetectorRef, ViewChild, ElementRef, Inject, Renderer2 } from '@angular/core';
import { DOCUMENT, CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AdminAuthService } from '../../services/admin-auth';
import { AdminDocumentsService, AdminDocument } from '../../services/admin-documents';
import { PagedResult } from '../../models/paged-result';
import { PaginatorComponent } from '../paginator/paginator';
import { UnansweredQuestionsComponent } from '../unanswered-questions/unanswered-questions';

type AdminTab = 'documents' | 'questions';

@Component({
  selector: 'app-admin-documents',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, PaginatorComponent, UnansweredQuestionsComponent],
  templateUrl: './admin-documents.html',
  styleUrl: './admin-documents.css'
})
export class AdminDocumentsComponent implements OnInit {
  private static readonly THEME_STORAGE_KEY = 'socios-theme';

  @ViewChild('fileInput') private fileInput!: ElementRef<HTMLInputElement>;

  isDarkMode = false;
  activeTab: AdminTab = 'documents';

  keyInput = '';
  authError = '';
  verifying = false;

  documents: AdminDocument[] = [];
  loading = false;
  loadError = '';

  readonly pageSize = 10;
  page = 1;
  totalCount = 0;

  selectedFile: File | null = null;
  uploadClubId: number | null = null;
  uploading = false;
  uploadError = '';

  editingId: number | null = null;
  editFileName = '';
  editClubId: number | null = null;
  savingEdit = false;

  deletingId: number | null = null;
  actionError = '';

  constructor(
    private auth: AdminAuthService,
    private documentsService: AdminDocumentsService,
    private cdr: ChangeDetectorRef,
    @Inject(DOCUMENT) private document: Document,
    private renderer: Renderer2
  ) {}

  ngOnInit(): void {
    this.loadTheme();
    if (this.isAuthenticated) {
      this.fetchDocuments();
    }
  }

  get isAuthenticated(): boolean {
    return this.auth.isAuthenticated();
  }

  private loadTheme(): void {
    const saved = localStorage.getItem(AdminDocumentsComponent.THEME_STORAGE_KEY);
    const prefersDark = typeof window.matchMedia === 'function' && window.matchMedia('(prefers-color-scheme: dark)').matches;
    this.isDarkMode = saved ? saved === 'dark' : prefersDark;
    this.applyTheme();
  }

  private applyTheme(): void {
    if (this.isDarkMode) {
      this.renderer.addClass(this.document.body, 'dark-theme');
    } else {
      this.renderer.removeClass(this.document.body, 'dark-theme');
    }
  }

  toggleTheme(): void {
    this.isDarkMode = !this.isDarkMode;
    localStorage.setItem(AdminDocumentsComponent.THEME_STORAGE_KEY, this.isDarkMode ? 'dark' : 'light');
    this.applyTheme();
  }

  submitKey(): void {
    const key = this.keyInput.trim();
    if (!key || this.verifying) return;

    this.verifying = true;
    this.authError = '';
    this.auth.setKey(key);

    this.documentsService.list(1, this.pageSize).subscribe({
      next: (result) => {
        this.applyPage(result);
        this.verifying = false;
        this.keyInput = '';
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.verifying = false;
        this.auth.clear();
        this.authError = err.status === 401
          ? 'Clave de administrador incorrecta.'
          : 'No se pudo conectar con el servidor.';
        this.cdr.detectChanges();
      }
    });
  }

  logout(): void {
    this.auth.clear();
    this.documents = [];
    this.page = 1;
    this.totalCount = 0;
    this.editingId = null;
  }

  selectTab(tab: AdminTab): void {
    this.activeTab = tab;
    this.editingId = null;
  }

  // La pestaña de preguntas recibió un 401: se vuelve a pedir la clave
  onUnauthorized(): void {
    this.handleAuthError({ status: 401 });
    this.cdr.detectChanges();
  }

  onPageChange(page: number): void {
    this.editingId = null;
    this.fetchDocuments(page);
  }

  fetchDocuments(page = this.page): void {
    this.loading = true;
    this.loadError = '';

    this.documentsService.list(page, this.pageSize).subscribe({
      next: (result) => {
        // Si la página quedó vacía (p. ej. se borró el último documento de la última
        // página), saltamos a la última página que todavía tiene documentos.
        const lastPage = Math.max(1, Math.ceil(result.totalCount / this.pageSize));
        if (result.items.length === 0 && page > lastPage) {
          this.fetchDocuments(lastPage);
          return;
        }

        this.applyPage(result);
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.loading = false;
        if (!this.handleAuthError(err)) {
          this.loadError = 'No se pudieron cargar los documentos.';
        }
        this.cdr.detectChanges();
      }
    });
  }

  triggerFileInput(): void {
    if (this.uploading) return;
    this.fileInput.nativeElement.click();
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.selectedFile = input.files?.[0] ?? null;
  }

  clearSelectedFile(): void {
    this.selectedFile = null;
    if (this.fileInput) {
      this.fileInput.nativeElement.value = '';
    }
  }

  upload(): void {
    if (!this.selectedFile || this.uploading) return;

    this.uploading = true;
    this.uploadError = '';

    this.documentsService.upload(this.selectedFile, this.uploadClubId).subscribe({
      next: () => {
        this.uploading = false;
        this.clearSelectedFile();
        this.uploadClubId = null;
        // El listado va del más nuevo al más viejo: el documento subido aparece en la página 1
        this.fetchDocuments(1);
      },
      error: (err) => {
        this.uploading = false;
        if (!this.handleAuthError(err)) {
          this.uploadError = 'No se pudo procesar el documento. Asegurate de que sea un PDF o un .txt válido.';
        }
        this.cdr.detectChanges();
      }
    });
  }

  startEdit(doc: AdminDocument): void {
    this.editingId = doc.id;
    this.editFileName = doc.fileName;
    this.editClubId = doc.clubId;
  }

  cancelEdit(): void {
    this.editingId = null;
  }

  saveEdit(doc: AdminDocument): void {
    const fileName = this.editFileName.trim();
    if (!fileName || this.savingEdit) return;

    this.savingEdit = true;
    this.actionError = '';

    this.documentsService.update(doc.id, fileName, this.editClubId).subscribe({
      next: () => {
        doc.fileName = fileName;
        doc.clubId = this.editClubId;
        this.editingId = null;
        this.savingEdit = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.savingEdit = false;
        this.reportActionError(err);
        this.cdr.detectChanges();
      }
    });
  }

  confirmDelete(doc: AdminDocument): void {
    if (this.deletingId !== null) return;
    if (!window.confirm(`¿Borrar "${doc.fileName}"? También se eliminan sus fragmentos del buscador.`)) return;

    this.deletingId = doc.id;
    this.actionError = '';

    this.documentsService.delete(doc.id).subscribe({
      next: () => {
        this.deletingId = null;
        // Recargamos la página para que suba el siguiente documento y se actualice el total
        this.fetchDocuments();
      },
      error: (err) => {
        this.deletingId = null;
        this.reportActionError(err);
        this.cdr.detectChanges();
      }
    });
  }

  private reportActionError(err: { status?: number }): void {
    if (this.handleAuthError(err)) return;

    if (err.status === 429) {
      this.actionError = 'Demasiados intentos seguidos. Esperá un minuto y probá de nuevo.';
    } else if (err.status === 404) {
      this.actionError = 'Ese documento ya no existe. Actualizando la lista...';
      this.fetchDocuments();
    } else {
      this.actionError = 'Ocurrió un error. Intentá de nuevo.';
    }
  }

  private applyPage(result: PagedResult<AdminDocument>): void {
    this.documents = result.items;
    this.page = result.page;
    this.totalCount = result.totalCount;
  }

  statusLabel(status: string): string {
    switch (status) {
      case 'Processed': return 'Procesado';
      case 'Failed': return 'Falló';
      default: return 'Pendiente';
    }
  }

  private handleAuthError(err: { status?: number }): boolean {
    if (err.status === 401) {
      this.auth.clear();
      this.authError = 'La clave dejó de ser válida.';
      return true;
    }
    return false;
  }
}
