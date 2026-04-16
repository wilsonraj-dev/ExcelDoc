import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface EmpresaResponse {
  id: number;
  nomeEmpresa: string;
}

export interface ConfiguracaoRequest {
  empresaId: number;
  linkServiceLayer: string;
  database: string;
  usuarioBanco: string;
  senhaBanco: string;
  usuarioSAP: string;
  senhaSAP: string;
}

export interface ConfiguracaoResponse extends ConfiguracaoRequest {
  id: number;
}

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
