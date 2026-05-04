export interface PerfilMapeamento {
  id: number;
  nome: string;
  fk_IdDocumento: number;
  fk_IdEmpresa: number | null;
  isPadrao: boolean;
  dataCriacao: string;
  itens: PerfilMapeamentoItem[];
}

export interface PerfilMapeamentoItem {
  id: number;
  fk_IdColecao: number;
  nomeColecao: string;
  fk_IdMapeamento: number;
  nomeMapeamento: string;
}

export interface PerfilMapeamentoPayload {
  nome: string;
  fk_IdDocumento: number;
  fk_IdEmpresa?: number | null;
  isPadrao?: boolean;
  itens: PerfilMapeamentoItemPayload[];
}

export interface PerfilMapeamentoItemPayload {
  fk_IdColecao: number;
  fk_IdMapeamento: number;
}
