export enum TipoCampo {
  String = 1,
  Int = 2,
  Double = 3,
  DateTime = 4,
  Boolean = 5
}

export const TIPO_CAMPO_LABELS: Record<TipoCampo, string> = {
  [TipoCampo.String]: 'String',
  [TipoCampo.Int]: 'Int',
  [TipoCampo.Double]: 'Double',
  [TipoCampo.DateTime]: 'DateTime',
  [TipoCampo.Boolean]: 'Boolean'
};

export const TIPO_CAMPO_OPTIONS: readonly { label: string; value: TipoCampo }[] = [
  { label: 'String', value: TipoCampo.String },
  { label: 'Int', value: TipoCampo.Int },
  { label: 'Double', value: TipoCampo.Double },
  { label: 'DateTime', value: TipoCampo.DateTime },
  { label: 'Boolean', value: TipoCampo.Boolean }
];

export interface MapeamentoCampo {
  id: number;
  nomeCampo: string;
  descricaoCampo: string | null;
  indiceColuna: number;
  tipoCampo: TipoCampo;
  formato: string | null;
  fk_IdColecao: number;
}

export interface MapeamentoCampoPayload {
  nomeCampo: string;
  descricaoCampo: string | null;
  indiceColuna: number;
  tipoCampo: number;
  formato: string | null;
  fk_IdColecao: number;
}

export interface MapeamentoCampoRow {
  id: number | null;
  nomeCampo: string;
  descricaoCampo: string;
  indiceColuna: number | null;
  tipoCampo: TipoCampo | '';
  formato: string;
  isNew: boolean;
  previewValue: string;
  errors: MapeamentoRowErrors;
}

export interface MapeamentoRowErrors {
  nomeCampo: string;
  indiceColuna: string;
  tipoCampo: string;
  formato: string;
}
