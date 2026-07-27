import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { PagedResult, Processamento, ProcessamentoItem } from '../models/processamento.model';

@Injectable({
  providedIn: 'root'
})
export class ProcessamentoService {
  private readonly apiUrl = '/api/processamentos';

  constructor(private readonly http: HttpClient) { }

  upload(file: File, documentoId: number, perfilMapeamentoId: number): Observable<Processamento> {
    const formData = new FormData();
    formData.append('Arquivo', file);
    formData.append('DocumentoId', documentoId.toString());
    formData.append('PerfilMapeamentoId', perfilMapeamentoId.toString());
    return this.http.post<Processamento>(`${this.apiUrl}/upload`, formData);
  }

  getAll(): Observable<PagedResult<Processamento>> {
    return this.http.get<PagedResult<Processamento>>(this.apiUrl);
  }

  getById(id: number): Observable<Processamento> {
    return this.http.get<Processamento>(`${this.apiUrl}/${id}`);
  }

  getItens(processamentoId: number): Observable<PagedResult<ProcessamentoItem>> {
    return this.http.get<PagedResult<ProcessamentoItem>>(`${this.apiUrl}/${processamentoId}/itens`);
  }
}
