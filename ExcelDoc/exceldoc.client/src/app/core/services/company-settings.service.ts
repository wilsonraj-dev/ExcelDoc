import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ConfiguracaoRequest, ConfiguracaoResponse, EmpresaResponse } from '../../features/empresa/models/empresa.models';

export { type ConfiguracaoRequest, type ConfiguracaoResponse, type EmpresaResponse } from '../../features/empresa/models/empresa.models';

@Injectable({
  providedIn: 'root'
})
export class CompanySettingsService {
  private readonly empresasApiUrl = '/api/empresas';
  private readonly configuracoesApiUrl = '/api/configuracoes';

  constructor(private readonly http: HttpClient) {}

  getEmpresas(): Observable<EmpresaResponse[]> {
    return this.http.get<EmpresaResponse[]>(this.empresasApiUrl);
  }

  getConfiguracao(empresaId: number): Observable<ConfiguracaoResponse> {
    return this.http.get<ConfiguracaoResponse>(`${this.configuracoesApiUrl}/${empresaId}`);
  }

  saveConfiguracao(request: ConfiguracaoRequest): Observable<ConfiguracaoResponse> {
    return this.http.put<ConfiguracaoResponse>(this.configuracoesApiUrl, request);
  }
}
