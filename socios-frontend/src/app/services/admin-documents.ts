import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { PagedResult } from '../models/paged-result';
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

  list(page: number, pageSize: number): Observable<PagedResult<AdminDocument>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<PagedResult<AdminDocument>>(this.apiUrl, { headers: this.auth.headers(), params });
  }

  upload(file: File, clubId: number | null): Observable<UploadDocumentResponse> {
    const formData = new FormData();
    formData.append('file', file);
    if (clubId !== null) {
      formData.append('clubId', clubId.toString());
    }
    return this.http.post<UploadDocumentResponse>(`${this.apiUrl}/upload`, formData, { headers: this.auth.headers() });
  }

  update(id: number, fileName: string, clubId: number | null): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, { fileName, clubId }, { headers: this.auth.headers() });
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`, { headers: this.auth.headers() });
  }
}
