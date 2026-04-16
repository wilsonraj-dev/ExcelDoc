import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { EmpresaRequest, EmpresaResponse } from '../../features/empresa/models/empresa.models';

export { type EmpresaRequest, type EmpresaResponse } from '../../features/empresa/models/empresa.models';

@Injectable({
  providedIn: 'root'
})
export class CreateCompanyService {
  private readonly apiUrl = '/api/empresas';

  constructor(private readonly http: HttpClient) {}

  getEmpresas(): Observable<EmpresaResponse[]> {
    return this.http.get<EmpresaResponse[]>(this.apiUrl);
  }

  createEmpresa(request: EmpresaRequest): Observable<EmpresaResponse> {
    return this.http.post<EmpresaResponse>(this.apiUrl, request);
  }
}
