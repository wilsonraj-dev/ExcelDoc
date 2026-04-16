import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { HttpService } from '../../../core/services/http.service';
import { Documento, DocumentoPayload } from '../models/documento.model';

@Injectable({
  providedIn: 'root'
})
export class DocumentoService {
  private readonly apiUrl = '/api/documentos';

  constructor(private readonly httpService: HttpService) { }

  getAll(): Observable<Documento[]> {
    return this.httpService.get<Documento[]>(this.apiUrl);
  }

  getById(id: number): Observable<Documento> {
    return this.httpService.get<Documento>(`${this.apiUrl}/${id}`);
  }

  create(documento: DocumentoPayload): Observable<Documento> {
    return this.httpService.post<Documento, DocumentoPayload>(this.apiUrl, documento);
  }

  update(id: number, documento: DocumentoPayload): Observable<Documento> {
    return this.httpService.put<Documento, DocumentoPayload>(`${this.apiUrl}/${id}`, documento);
  }

  delete(id: number): Observable<void> {
    return this.httpService.delete<void>(`${this.apiUrl}/${id}`);
  }
}
