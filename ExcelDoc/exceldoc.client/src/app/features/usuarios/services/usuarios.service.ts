import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import {
  PagedResult,
  Usuario,
  UsuarioCreateRequest,
  UsuarioCreateResponse,
  UsuarioEmpresaVinculoRequest,
  UsuarioQuery
} from '../models/usuario.models';

@Injectable({
  providedIn: 'root'
})
export class UsuariosService {
  private readonly apiUrl = '/api/usuarios';

  constructor(private readonly http: HttpClient) {}

  create(request: UsuarioCreateRequest): Observable<UsuarioCreateResponse> {
    return this.http.post<UsuarioCreateResponse>(this.apiUrl, request);
  }

  getPaged(query: UsuarioQuery): Observable<PagedResult<Usuario>> {
    let params = new HttpParams()
      .set('pageNumber', query.pageNumber.toString())
      .set('pageSize', query.pageSize.toString());

    if (query.termo?.trim()) {
      params = params.set('termo', query.termo.trim());
    }

    return this.http.get<PagedResult<Usuario>>(this.apiUrl, { params });
  }

  vincularEmpresa(usuarioId: number, request: UsuarioEmpresaVinculoRequest): Observable<Usuario> {
    return this.http.put<Usuario>(`${this.apiUrl}/${usuarioId}/empresa`, request);
  }
}
