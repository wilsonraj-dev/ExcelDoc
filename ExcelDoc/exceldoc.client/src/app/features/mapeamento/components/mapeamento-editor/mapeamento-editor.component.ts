import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { finalize, forkJoin } from 'rxjs';
import * as XLSX from 'xlsx';
import { AuthService } from '../../../../core/services/auth.service';
import { NotificationService } from '../../../../core/services/notification.service';
import {
  MapeamentoCampo,
  MapeamentoCampoPayload,
  MapeamentoCampoRow,
  MapeamentoRowErrors,
  TipoCampo,
  TIPO_CAMPO_OPTIONS
} from '../../models/mapeamento.model';
import { MapeamentoService } from '../../services/mapeamento.service';

@Component({
  selector: 'app-mapeamento-editor',
  templateUrl: './mapeamento-editor.component.html',
  styleUrl: './mapeamento-editor.component.css'
})
export class MapeamentoEditorComponent implements OnInit {
  readonly displayedColumns: string[] = [
    'nomeCampo', 'descricaoCampo', 'indiceColuna', 'tipoCampo', 'formato', 'preview', 'acoes'
  ];
  readonly tipoCampoOptions = TIPO_CAMPO_OPTIONS;

  colecaoId!: number;
  rows: MapeamentoCampoRow[] = [];
  originalRows: MapeamentoCampoRow[] = [];
  isLoading = false;
  isSaving = false;
  isAdministrator: boolean;
  excelPreviewData: string[] = [];

  private readonly destroyRef = inject(DestroyRef);

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly authService: AuthService,
    private readonly mapeamentoService: MapeamentoService,
    private readonly notificationService: NotificationService
  ) {
    this.isAdministrator = this.authService.isAdministrator();
  }

  ngOnInit(): void {
    this.colecaoId = Number(this.route.snapshot.paramMap.get('colecaoId'));

    if (isNaN(this.colecaoId) || this.colecaoId <= 0) {
      this.notificationService.showError('Coleção inválida.');
      void this.router.navigate(['/colecoes']);
      return;
    }

    this.loadMapeamentos();
  }

  get hasChanges(): boolean {
    return JSON.stringify(this.rows) !== JSON.stringify(this.originalRows);
  }

  get hasErrors(): boolean {
    return this.rows.some(r =>
      r.errors.nomeCampo || r.errors.indiceColuna || r.errors.tipoCampo || r.errors.formato
    );
  }

  loadMapeamentos(): void {
    this.isLoading = true;

    this.mapeamentoService.getByColecao(this.colecaoId)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isLoading = false)
      )
      .subscribe({
        next: (campos) => {
          this.rows = campos
            .sort((a, b) => a.indiceColuna - b.indiceColuna)
            .map(c => this.toRow(c));
          this.snapshotOriginal();
          this.refreshPreview();
        },
        error: (err: HttpErrorResponse) => {
          this.notificationService.showError(
            err.error?.message ?? 'Erro ao carregar mapeamentos.'
          );
        }
      });
  }

  adicionarCampo(): void {
    const newRow: MapeamentoCampoRow = {
      id: null,
      nomeCampo: '',
      descricaoCampo: '',
      indiceColuna: null,
      tipoCampo: '',
      formato: '',
      isNew: true,
      previewValue: '',
      errors: this.emptyErrors()
    };
    this.rows = [...this.rows, newRow];
  }

  removerCampo(index: number): void {
    this.rows = this.rows.filter((_, i) => i !== index);
    this.validateAll();
    this.refreshPreview();
  }

  onFieldChange(row: MapeamentoCampoRow): void {
    if (row.tipoCampo !== TipoCampo.DateTime) {
      row.formato = '';
    }
    this.validateRow(row);
    this.refreshPreview();
  }

  validateRow(row: MapeamentoCampoRow): void {
    const errors = this.emptyErrors();

    if (!row.nomeCampo?.trim()) {
      errors.nomeCampo = 'Nome do campo é obrigatório';
    }

    if (row.indiceColuna === null || row.indiceColuna === undefined || row.indiceColuna < 1) {
      errors.indiceColuna = 'Índice deve ser um número positivo';
    } else {
      const duplicate = this.rows.find(
        r => r !== row && r.indiceColuna === row.indiceColuna
      );
      if (duplicate) {
        errors.indiceColuna = 'Índice duplicado nesta coleção';
      }
    }

    if (!row.tipoCampo) {
      errors.tipoCampo = 'Tipo do campo é obrigatório';
    }

    if (row.tipoCampo === TipoCampo.DateTime && !row.formato?.trim()) {
      errors.formato = 'Formato é obrigatório para DateTime';
    }

    row.errors = errors;
  }

  validateAll(): void {
    this.rows.forEach(r => this.validateRow(r));
  }

  salvar(): void {
    this.validateAll();

    if (this.hasErrors) {
      this.notificationService.showError('Corrija os erros antes de salvar.');
      return;
    }

    this.isSaving = true;

    const deletedIds = this.originalRows
      .filter(o => o.id !== null && !this.rows.some(r => r.id === o.id))
      .map(o => o.id!);

    const deletes$ = deletedIds.map(id => this.mapeamentoService.delete(id));
    const creates$ = this.rows
      .filter(r => r.isNew)
      .map(r => this.mapeamentoService.create(this.toPayload(r)));
    const updates$ = this.rows
      .filter(r => !r.isNew && r.id !== null)
      .map(r => this.mapeamentoService.update(r.id!, this.toPayload(r)));

    const all$ = [...deletes$, ...creates$, ...updates$];

    if (all$.length === 0) {
      this.isSaving = false;
      this.notificationService.showInfo('Nenhuma alteração para salvar.');
      return;
    }

    forkJoin(all$)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isSaving = false)
      )
      .subscribe({
        next: () => {
          this.notificationService.showSuccess('Mapeamentos salvos com sucesso!');
          this.loadMapeamentos();
        },
        error: (err: HttpErrorResponse) => {
          this.notificationService.showError(
            err.error?.message ?? 'Erro ao salvar mapeamentos.'
          );
        }
      });
  }

  cancelar(): void {
    this.rows = this.originalRows.map(r => ({ ...r, errors: this.emptyErrors() }));
    this.refreshPreview();
  }

  voltar(): void {
    void this.router.navigate(['/colecoes']);
  }

  onExcelUpload(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input?.files?.[0];

    if (!file) {
      return;
    }

    const reader = new FileReader();
    reader.onload = (e: ProgressEvent<FileReader>) => {
      try {
        const data = new Uint8Array(e.target!.result as ArrayBuffer);
        const workbook = XLSX.read(data, { type: 'array' });
        const firstSheet = workbook.Sheets[workbook.SheetNames[0]];
        const jsonData = XLSX.utils.sheet_to_json<string[]>(firstSheet, { header: 1 });

        if (jsonData.length > 0) {
          this.excelPreviewData = (jsonData[0] as unknown[]).map(v =>
            v !== null && v !== undefined ? String(v) : ''
          );
          this.refreshPreview();
          this.notificationService.showSuccess(
            `Preview carregado: ${this.excelPreviewData.length} colunas detectadas.`
          );
        } else {
          this.notificationService.showError('O arquivo Excel está vazio.');
        }
      } catch {
        this.notificationService.showError('Erro ao ler o arquivo Excel.');
      }

      input.value = '';
    };
    reader.readAsArrayBuffer(file);
  }

  refreshPreview(): void {
    for (const row of this.rows) {
      if (row.indiceColuna !== null && row.indiceColuna > 0 && this.excelPreviewData.length > 0) {
        const idx = row.indiceColuna - 1;
        row.previewValue = idx < this.excelPreviewData.length
          ? this.excelPreviewData[idx]
          : '(coluna vazia)';
      } else {
        row.previewValue = '';
      }
    }
  }

  isFormatoEnabled(row: MapeamentoCampoRow): boolean {
    return row.tipoCampo === TipoCampo.DateTime;
  }

  private toRow(campo: MapeamentoCampo): MapeamentoCampoRow {
    return {
      id: campo.id,
      nomeCampo: campo.nomeCampo,
      descricaoCampo: campo.descricaoCampo ?? '',
      indiceColuna: campo.indiceColuna,
      tipoCampo: campo.tipoCampo,
      formato: campo.formato ?? '',
      isNew: false,
      previewValue: '',
      errors: this.emptyErrors()
    };
  }

  private toPayload(row: MapeamentoCampoRow): MapeamentoCampoPayload {
    return {
      nomeCampo: row.nomeCampo.trim(),
      descricaoCampo: row.descricaoCampo?.trim() || null,
      indiceColuna: row.indiceColuna!,
      tipoCampo: row.tipoCampo as string,
      formato: row.tipoCampo === TipoCampo.DateTime ? (row.formato?.trim() || null) : null,
      fk_IdColecao: this.colecaoId
    };
  }

  private snapshotOriginal(): void {
    this.originalRows = this.rows.map(r => ({ ...r, errors: this.emptyErrors() }));
  }

  private emptyErrors(): MapeamentoRowErrors {
    return { nomeCampo: '', indiceColuna: '', tipoCampo: '', formato: '' };
  }
}
