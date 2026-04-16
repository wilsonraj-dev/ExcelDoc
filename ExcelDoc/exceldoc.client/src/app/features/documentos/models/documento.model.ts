export interface Documento {
  id: number;
  nomeDocumento: string;
  endpoint: string;
}

export interface DocumentoPayload {
  nomeDocumento: string;
  endpoint: string;
}
