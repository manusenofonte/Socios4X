import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { AdminAuthService } from './admin-auth';

export interface AdminDocument {
  id: number;
  fileName: string;
  fileType: string;
  clubId: number | null;
  chunkCount: number;
  status: string;
  uploadDate: string;
  processedDate: string | null;
}

export interface UploadDocumentResponse {
  message: string;
  chunksStored: number;
}

@Injectable({ providedIn: 'root' })
export class AdminDocumentsService {
  private readonly apiUrl = `${environment.apiUrl}/documents`;

  constructor(private http: HttpClient, private auth: AdminAuthService) {}

  list(): Observable<AdminDocument[]> {
    return this.http.get<AdminDocument[]>(this.apiUrl, { headers: this.authHeaders() });
  }

  upload(file: File, clubId: number | null): Observable<UploadDocumentResponse> {
    const formData = new FormData();
    formData.append('file', file);
    if (clubId !== null) {
      formData.append('clubId', clubId.toString());
    }
    return this.http.post<UploadDocumentResponse>(`${this.apiUrl}/upload`, formData, { headers: this.authHeaders() });
  }

  update(id: number, fileName: string, clubId: number | null): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, { fileName, clubId }, { headers: this.authHeaders() });
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`, { headers: this.authHeaders() });
  }

  private authHeaders(): HttpHeaders {
    return new HttpHeaders({ 'X-Admin-Key': this.auth.key() ?? '' });
  }
}
