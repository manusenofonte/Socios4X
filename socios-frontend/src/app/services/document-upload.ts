import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface UploadResponse {
  message: string;
  chunksStored: number;
}

@Injectable({ providedIn: 'root' })
export class DocumentUploadService {
  private readonly apiUrl = `${environment.apiUrl}/documents`;

  constructor(private http: HttpClient) {}

  upload(file: File, adminKey: string, clubId?: number): Observable<UploadResponse> {
    const formData = new FormData();
    formData.append('file', file);
    if (clubId !== undefined) {
      formData.append('clubId', clubId.toString());
    }
    const headers = new HttpHeaders({ 'X-Admin-Key': adminKey });
    return this.http.post<UploadResponse>(`${this.apiUrl}/upload`, formData, { headers });
  }
}
