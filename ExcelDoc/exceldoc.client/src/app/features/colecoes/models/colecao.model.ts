import { Documento } from '../../documentos/models/documento.model';

export enum TipoColecao {
  Header = 'Header',
  Line = 'Line'
}

export interface Colecao {
  id: number;
  nomeColecao: string;
  tipoColecao: TipoColecao | string | number;
  fk_IdEmpresa?: number | null;
  empresaId?: number | null;
  padraoSistema?: boolean;
  documentos?: Documento[];
  documentoIds?: number[];
  campos?: ReadonlyArray<unknown>;
}

export interface ColecaoPayload {
  nomeColecao: string;
  tipoColecao: number;
  fk_IdEmpresa: number | null;
  documentoIds: number[];
}

export interface TipoColecaoOption {
  label: string;
  value: TipoColecao;
}

export const TIPO_COLECAO_OPTIONS: ReadonlyArray<TipoColecaoOption> = [
  { label: 'Header', value: TipoColecao.Header },
  { label: 'Line', value: TipoColecao.Line }
];

export function resolveTipoColecao(value: TipoColecao | string | number | null | undefined): TipoColecao {
  if (value === TipoColecao.Header || value === 'header' || value === 'Header' || value === 1 || value === '1') {
    return TipoColecao.Header;
  }

  if (value === TipoColecao.Line || value === 'line' || value === 'Line' || value === 2 || value === '2') {
    return TipoColecao.Line;
  }

  return TipoColecao.Header;
}

export function toTipoColecaoRequestValue(value: TipoColecao | string | number | null | undefined): number {
  return resolveTipoColecao(value) === TipoColecao.Line ? 2 : 1;
}

export function getColecaoEmpresaId(colecao: Colecao): number | null {
  return colecao.fk_IdEmpresa ?? colecao.empresaId ?? null;
}

export function isColecaoPadrao(colecao: Colecao): boolean {
  if (typeof colecao.padraoSistema === 'boolean') {
    return colecao.padraoSistema;
  }

  return getColecaoEmpresaId(colecao) === null;
}

export function getColecaoDocumentoIds(colecao: Colecao): number[] {
  if (colecao.documentoIds?.length) {
    return [...new Set(colecao.documentoIds)];
  }

  if (colecao.documentos?.length) {
    return [...new Set(colecao.documentos.map((documento) => documento.id))];
  }

  return [];
}
