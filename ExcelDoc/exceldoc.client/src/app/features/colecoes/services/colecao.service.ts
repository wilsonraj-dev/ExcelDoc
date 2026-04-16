import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { HttpService, HttpRequestOptions } from '../../../core/services/http.service';
import { Documento } from '../../documentos/models/documento.model';
import { Colecao, ColecaoPayload } from '../models/colecao.model';

@Injectable({
  providedIn: 'root'
})
export class ColecaoService {
  private readonly apiUrl = '/api/colecoes';
  private readonly documentosApiUrl = '/api/documentos';

  constructor(
    private readonly authService: AuthService,
    private readonly httpService: HttpService
  ) { }

  getAll(): Observable<Colecao[]> {
    return this.httpService.get<Colecao[]>(this.apiUrl, this.buildEmpresaRequestOptions());
  }

  getById(id: number): Observable<Colecao> {
    return this.httpService.get<Colecao>(`${this.apiUrl}/${id}`);
  }

  create(colecao: ColecaoPayload): Observable<Colecao> {
    return this.httpService.post<Colecao, ColecaoPayload>(this.apiUrl, colecao);
  }

  update(id: number, colecao: ColecaoPayload): Observable<Colecao> {
    return this.httpService.put<Colecao, ColecaoPayload>(`${this.apiUrl}/${id}`, colecao);
  }

  delete(id: number): Observable<void> {
    return this.httpService.delete<void>(`${this.apiUrl}/${id}`);
  }

  getDocumentos(): Observable<Documento[]> {
    return this.httpService.get<Documento[]>(this.documentosApiUrl);
  }

  private buildEmpresaRequestOptions(): HttpRequestOptions | undefined {
    const empresaId = this.authService.getSession()?.empresaId;

    if (!empresaId) {
      return undefined;
    }

    return {
      params: { empresaId }
    };
  }
}
