import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { PagedResult, Processamento, ProcessamentoItem } from '../models/processamento.model';

@Injectable({
  providedIn: 'root'
})
export class ProcessamentoService {
  private readonly apiUrl = '/api/processamentos';

  constructor(
    private readonly http: HttpClient,
    private readonly authService: AuthService
  ) { }

  upload(file: File, documentoId: number, empresaId: number): Observable<Processamento> {
    const formData = new FormData();
    formData.append('Arquivo', file);
    formData.append('DocumentoId', documentoId.toString());
    formData.append('EmpresaId', empresaId.toString());
    return this.http.post<Processamento>(`${this.apiUrl}/upload`, formData);
  }

  getAll(): Observable<PagedResult<Processamento>> {
    const empresaId = this.authService.getSession()?.empresaId;
    const params = empresaId
      ? new HttpParams().set('empresaId', empresaId.toString())
      : undefined;

    return this.http.get<PagedResult<Processamento>>(this.apiUrl, { params });
  }

  getById(id: number): Observable<Processamento> {
    return this.http.get<Processamento>(`${this.apiUrl}/${id}`);
  }

  getItens(processamentoId: number): Observable<PagedResult<ProcessamentoItem>> {
    return this.http.get<PagedResult<ProcessamentoItem>>(`${this.apiUrl}/${processamentoId}/itens`);
  }
}
