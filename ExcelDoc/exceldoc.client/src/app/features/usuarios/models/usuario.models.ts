export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

export interface Usuario {
  id: number;
  nomeUsuario: string;
  email: string;
  tipoUsuario: string;
  ativo: boolean;
  empresaId?: number | null;
  nomeEmpresa?: string | null;
}

export interface UsuarioQuery {
  termo?: string;
  pageNumber: number;
  pageSize: number;
}

export interface UsuarioCreateRequest {
  nomeUsuario: string;
  email: string;
  senha: string;
  empresaId?: number | null;
}

export interface UsuarioCreateResponse {
  usuarioId: number;
  nomeUsuario: string;
  email: string;
}

export interface UsuarioEmpresaVinculoRequest {
  empresaId: number;
}
