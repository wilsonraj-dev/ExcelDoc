import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { HttpService } from '../../../core/services/http.service';
import { PerfilMapeamento, PerfilMapeamentoPayload } from '../models/perfil-mapeamento.model';

@Injectable({
  providedIn: 'root'
})
export class PerfilMapeamentoService {
  private readonly apiUrl = '/api/perfil-mapeamento';

  constructor(private readonly httpService: HttpService) { }

  getByDocumento(documentoId: number): Observable<PerfilMapeamento[]> {
    return this.httpService.get<PerfilMapeamento[]>(`${this.apiUrl}/documento/${documentoId}`);
  }

  getById(id: number): Observable<PerfilMapeamento> {
    return this.httpService.get<PerfilMapeamento>(`${this.apiUrl}/${id}`);
  }

  create(payload: PerfilMapeamentoPayload): Observable<PerfilMapeamento> {
    return this.httpService.post<PerfilMapeamento, PerfilMapeamentoPayload>(this.apiUrl, payload);
  }

  update(id: number, payload: PerfilMapeamentoPayload): Observable<PerfilMapeamento> {
    return this.httpService.put<PerfilMapeamento, PerfilMapeamentoPayload>(`${this.apiUrl}/${id}`, payload);
  }

  delete(id: number): Observable<void> {
    return this.httpService.delete<void>(`${this.apiUrl}/${id}`);
  }

  clone(id: number): Observable<PerfilMapeamento> {
    return this.httpService.post<PerfilMapeamento, undefined>(`${this.apiUrl}/${id}/clone`, undefined);
  }
}
