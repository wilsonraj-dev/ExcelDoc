import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { HttpService } from '../../../core/services/http.service';
import { MapeamentoCampo, MapeamentoCampoPayload } from '../models/mapeamento.model';

@Injectable({
  providedIn: 'root'
})
export class MapeamentoService {
  private readonly apiUrl = '/api/mapeamentos';

  constructor(private readonly httpService: HttpService) {}

  getByColecao(colecaoId: number): Observable<MapeamentoCampo[]> {
    return this.httpService.get<MapeamentoCampo[]>(this.apiUrl, {
      params: { colecaoId }
    });
  }

  create(mapeamento: MapeamentoCampoPayload): Observable<MapeamentoCampo> {
    return this.httpService.post<MapeamentoCampo, MapeamentoCampoPayload>(this.apiUrl, mapeamento);
  }

  update(id: number, mapeamento: MapeamentoCampoPayload): Observable<MapeamentoCampo> {
    return this.httpService.put<MapeamentoCampo, MapeamentoCampoPayload>(`${this.apiUrl}/${id}`, mapeamento);
  }

  delete(id: number): Observable<void> {
    return this.httpService.delete<void>(`${this.apiUrl}/${id}`);
  }
}
