import { Component, Inject, OnInit, Renderer2, ChangeDetectorRef, ViewChild, ElementRef } from '@angular/core';
import { DOCUMENT, CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AssistantService, FaqSuggestion } from '../../services/assistant';
import { DocumentUploadService } from '../../services/document-upload';
import { MarkdownPipe } from '../../pipes/markdown';

export interface ChatMessage {
  text: string;
  isUser: boolean;
}

@Component({
  selector: 'app-chat',
  standalone: true,
  imports: [CommonModule, FormsModule, MarkdownPipe],
  templateUrl: './chat.html',
  styleUrl: './chat.css'
})
export class ChatComponent implements OnInit {
  private static readonly THEME_STORAGE_KEY = 'socios-theme';

  @ViewChild('chatScroll') private chatScrollContainer!: ElementRef;
  @ViewChild('fileInput') private fileInput!: ElementRef<HTMLInputElement>;

  private readonly welcomeMessage: ChatMessage = {
    text: 'Hola, soy asistente virtual. ¿En qué puedo ayudarte hoy?',
    isUser: false
  };

  isDarkMode = false;
  newMessage: string = '';
  isTyping: boolean = false;
  isUploading: boolean = false;
  copiedIndex: number | null = null;
  suggestions: FaqSuggestion[] = [];

  messages: ChatMessage[] = [this.welcomeMessage];

  constructor(
    @Inject(DOCUMENT) private document: Document,
    private renderer: Renderer2,
    private cdr: ChangeDetectorRef,
    private assistantService: AssistantService,
    private documentUploadService: DocumentUploadService
  ) { }

  ngOnInit(): void {
    this.loadTheme();

    this.assistantService.getSuggestions().subscribe({
      next: (suggestions) => {
        this.suggestions = suggestions;
        this.cdr.detectChanges();
      },
      error: () => { /* sin sugerencias no pasa nada, el chat sigue andando */ }
    });
  }

  private loadTheme(): void {
    const saved = localStorage.getItem(ChatComponent.THEME_STORAGE_KEY);
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

  scrollToBottom(): void {
    setTimeout(() => {
      try {
        this.chatScrollContainer.nativeElement.scrollTop = this.chatScrollContainer.nativeElement.scrollHeight;
      } catch (err) { }
    }, 50);
  }

  toggleTheme() {
    this.isDarkMode = !this.isDarkMode;
    localStorage.setItem(ChatComponent.THEME_STORAGE_KEY, this.isDarkMode ? 'dark' : 'light');
    this.applyTheme();
  }

  clearChat() {
    this.messages = [this.welcomeMessage];
  }

  copyMessage(text: string, index: number) {
    navigator.clipboard.writeText(text);
    this.copiedIndex = index;
    setTimeout(() => {
      this.copiedIndex = null;
      this.cdr.detectChanges();
    }, 1500);
  }

  sendMessage() {
    if (!this.newMessage.trim() || this.isTyping) return;

    const userText = this.newMessage;
    this.messages.push({ text: userText, isUser: true });
    this.newMessage = '';

    this.isTyping = true;
    this.cdr.detectChanges();
    this.scrollToBottom();

    this.assistantService.ask(userText).subscribe({
      next: (response) => {
        this.isTyping = false;
        this.messages.push({ text: response.answer, isUser: false });
        this.cdr.detectChanges();
        this.scrollToBottom();
      },
      error: () => {
        this.isTyping = false;
        this.messages.push({
          text: 'Uy, no pude conectarme con el servidor. Intentá de nuevo en un momento.',
          isUser: false
        });
        this.cdr.detectChanges();
        this.scrollToBottom();
      }
    });
  }

  useSuggestion(question: string) {
    this.newMessage = question;
    this.sendMessage();
  }

  triggerFileInput() {
    if (this.isUploading) return;
    this.fileInput.nativeElement.click();
  }

  onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) return;

    const adminKey = window.prompt('Clave de administrador para cargar documentos:');
    if (!adminKey) return;

    this.isUploading = true;
    this.messages.push({ text: `Adjunté "${file.name}"`, isUser: true });
    this.cdr.detectChanges();
    this.scrollToBottom();

    this.documentUploadService.upload(file, adminKey).subscribe({
      next: (response) => {
        this.isUploading = false;
        this.messages.push({
          text: `Listo, sumé "${file.name}" a mi base de conocimiento (${response.chunksStored} fragmentos).`,
          isUser: false
        });
        this.cdr.detectChanges();
        this.scrollToBottom();
      },
      error: (err) => {
        this.isUploading = false;
        const text = err.status === 401
          ? 'Clave de administrador incorrecta.'
          : `No pude procesar "${file.name}". Asegurate de que sea un PDF o un .txt.`;
        this.messages.push({ text, isUser: false });
        this.cdr.detectChanges();
        this.scrollToBottom();
      }
    });
  }
}
