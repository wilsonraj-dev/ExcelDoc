export interface EmpresaRequest {
  nomeEmpresa: string;
}

export interface EmpresaResponse {
  id: number;
  nomeEmpresa: string;
}

export interface ConfiguracaoRequest {
  empresaId: number;
  linkServiceLayer: string;
  database: string;
  usuarioSAP: string;
  senhaSAP: string;
}

export interface ConfiguracaoResponse extends ConfiguracaoRequest {
  id: number;
}
