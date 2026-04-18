export type ProcessamentoStatus = 'Processando' | 'Sucesso' | 'Erro';

export interface Processamento {
  id: number;
  nomeArquivo: string;
  dataExecucao: string;
  status: ProcessamentoStatus;
  totalRegistros: number;
  totalSucesso: number;
  totalErro: number;
  progresso?: number;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

export type ProcessamentoItemStatus = 'Sucesso' | 'Erro';

export interface ProcessamentoItem {
  linhaExcel: number;
  jsonEnviado: string;
  jsonRetorno: string;
  status: ProcessamentoItemStatus;
  mensagemErro: string;
}
