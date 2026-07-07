export type ProcessamentoStatus = 'Processando' | 'Sucesso' | 'Erro';

export interface Processamento {
  id: number;
  nomeArquivo: string;
  dataExecucao: string;
  status: ProcessamentoStatus;
  totalRegistros: number;
  totalSucesso: number;
  totalErro: number;
  totalIgnorado: number;
  progresso?: number;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

export type ProcessamentoItemStatus = 'Sucesso' | 'Erro' | 'Ignorado';

export interface ProcessamentoItem {
  id: number;
  idExcel: number | null;
  idDocumentoUnico: string | null;
  linhaExcel: number;
  jsonEnviado: string;
  jsonRetorno: string;
  status: ProcessamentoItemStatus;
  mensagem: string | null;
  erro: string | null;
  dataExecucao: string | null;
  dataFinalizacao: string | null;
}
