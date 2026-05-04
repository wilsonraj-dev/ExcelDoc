export interface Documento {
  id: number;
  nomeDocumento: string;
  endpoint: string;
  colecaoIds?: number[];
  colecoes?: { id: number; nomeColecao: string }[];
}

export interface DocumentoPayload {
  nomeDocumento: string;
  endpoint: string;
}
