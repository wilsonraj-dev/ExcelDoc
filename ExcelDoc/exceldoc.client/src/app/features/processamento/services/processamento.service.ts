import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import {
  PagedResult,
  Processamento,
  ProcessamentoItem,
  ProcessamentoItemStatus,
  ProcessamentoStatus
} from '../models/processamento.model';

type ProcessamentoApi = Omit<Processamento, 'status'> & { status: ProcessamentoStatus | number | string };
type ProcessamentoItemApi = Omit<ProcessamentoItem, 'status'> & { status: ProcessamentoItemStatus | number | string };

const PROCESSAMENTO_STATUS: Record<string, ProcessamentoStatus> = {
  '1': 'Processando',
  '2': 'Sucesso',
  '3': 'Erro',
  Processando: 'Processando',
  Sucesso: 'Sucesso',
  Erro: 'Erro'
};

const PROCESSAMENTO_ITEM_STATUS: Record<string, ProcessamentoItemStatus> = {
  '1': 'Sucesso',
  '2': 'Erro',
  '3': 'Ignorado',
  Sucesso: 'Sucesso',
  Erro: 'Erro',
  Ignorado: 'Ignorado'
};

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
    return this.http.post<ProcessamentoApi>(`${this.apiUrl}/upload`, formData)
      .pipe(map((processamento) => this.normalizeProcessamento(processamento)));
  }

  getAll(): Observable<PagedResult<Processamento>> {
    return this.http.get<PagedResult<ProcessamentoApi>>(this.apiUrl)
      .pipe(map((result) => ({
        ...result,
        items: result.items.map((processamento) => this.normalizeProcessamento(processamento))
      })));
  }

  getById(id: number): Observable<Processamento> {
    return this.http.get<ProcessamentoApi>(`${this.apiUrl}/${id}`)
      .pipe(map((processamento) => this.normalizeProcessamento(processamento)));
  }

  getItens(processamentoId: number): Observable<PagedResult<ProcessamentoItem>> {
    return this.http.get<PagedResult<ProcessamentoItemApi>>(`${this.apiUrl}/${processamentoId}/itens`)
      .pipe(map((result) => ({
        ...result,
        items: result.items.map((item) => ({
          ...item,
          status: PROCESSAMENTO_ITEM_STATUS[String(item.status)] ?? 'Erro'
        }))
      })));
  }

  private normalizeProcessamento(processamento: ProcessamentoApi): Processamento {
    return {
      ...processamento,
      status: PROCESSAMENTO_STATUS[String(processamento.status)] ?? 'Erro'
    };
  }
}
