import { Routes } from '@angular/router';
import { ChatComponent } from './components/chat/chat';
import { AdminDocumentsComponent } from './components/admin-documents/admin-documents';

export const routes: Routes = [
  { path: '', component: ChatComponent }, // Ruta por defecto al entrar a la página
  { path: 'admin', component: AdminDocumentsComponent },
  { path: '**', redirectTo: '' } // Redirige al chat si escriben cualquier otra URL
];
