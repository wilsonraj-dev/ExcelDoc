import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, EventEmitter, Input, OnChanges, Output, SimpleChanges, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize, forkJoin, of } from 'rxjs';
import * as XLSX from 'xlsx';
import { NotificationService } from '../../../../core/services/notification.service';
import { TranslateService } from '../../../../core/services/translate.service';
import {
  Mapeamento,
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
export class MapeamentoEditorComponent implements OnChanges {
  @Input({ required: true }) colecaoId!: number;
  @Input() colecaoNome = '';
  @Input({ required: true }) mapeamento!: Mapeamento;
  @Input() readonly = false;

  @Output() camposChanged = new EventEmitter<void>();

  readonly displayedColumns: string[] = ['nomeCampo', 'indiceColuna', 'tipoCampo', 'formato', 'preview', 'acoes'];
  readonly tipoCampoOptions = TIPO_CAMPO_OPTIONS;

  rows: MapeamentoCampoRow[] = [];
  originalRows: MapeamentoCampoRow[] = [];
  isLoading = false;
  isSaving = false;
  excelPreviewData: string[] = [];

  private readonly destroyRef = inject(DestroyRef);

  constructor(
    private readonly mapeamentoService: MapeamentoService,
    private readonly notificationService: NotificationService,
    private readonly translate: TranslateService
  ) {}

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['mapeamento']?.currentValue?.id) {
      this.loadCampos();
      return;
    }

    if (changes['readonly'] && this.readonly) {
      this.validateAll();
    }
  }

  get hasChanges(): boolean {
    return JSON.stringify(this.rows) !== JSON.stringify(this.originalRows);
  }

  get hasErrors(): boolean {
    return this.rows.some((row) => !!(row.errors.nomeCampo || row.errors.indiceColuna || row.errors.tipoCampo || row.errors.formato));
  }

  adicionarCampo(): void {
    if (this.readonly) {
      return;
    }

    const newRow: MapeamentoCampoRow = {
      id: null,
      nomeCampo: '',
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
    if (this.readonly) {
      return;
    }

    this.rows = this.rows.filter((_, currentIndex) => currentIndex !== index);
    this.validateAll();
    this.refreshPreview();
  }

  onFieldChange(row: MapeamentoCampoRow): void {
    if (this.readonly) {
      return;
    }

    if (row.tipoCampo !== TipoCampo.DateTime) {
      row.formato = '';
    }

    this.validateRow(row);
    this.refreshPreview();
  }

  salvar(): void {
    if (this.readonly) {
      return;
    }

    this.validateAll();

    if (this.hasErrors) {
      const messages: string[] = [];
      for (const row of this.rows) {
        const label = row.nomeCampo?.trim() || `Linha ${this.rows.indexOf(row) + 1}`;
        if (row.errors.nomeCampo) messages.push(`${label}: ${row.errors.nomeCampo}`);
        if (row.errors.indiceColuna) messages.push(`${label}: ${row.errors.indiceColuna}`);
        if (row.errors.tipoCampo) messages.push(`${label}: ${row.errors.tipoCampo}`);
        if (row.errors.formato) messages.push(`${label}: ${row.errors.formato}`);
      }

      this.notificationService.showError(messages.length ? messages.join(' | ') : this.translate.instant('mapeamento.mapeamentoEditor.feedback.errors.fixErrors'));
      return;
    }

    const deletedIds = this.originalRows
      .filter((original) => original.id !== null && !this.rows.some((row) => row.id === original.id))
      .map((original) => original.id!);

    const deletes$ = deletedIds.map((id) => this.mapeamentoService.deleteCampo(id));
    const creates$ = this.rows
      .filter((row) => row.isNew)
      .map((row) => this.mapeamentoService.createCampo(this.toPayload(row)));
    const updates$ = this.rows
      .filter((row) => !row.isNew && row.id !== null)
      .map((row) => this.mapeamentoService.updateCampo(row.id!, this.toPayload(row)));

    const operations = [...deletes$, ...creates$, ...updates$];

    if (!operations.length) {
      this.notificationService.showInfo(this.translate.instant('mapeamento.mapeamentoEditor.feedback.info.noChanges'));
      return;
    }

    this.isSaving = true;
    forkJoin(operations.length ? operations : [of(null)])
      .pipe(
        finalize(() => { this.isSaving = false; }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: () => {
          this.notificationService.showSuccess(this.translate.instant('mapeamento.mapeamentoEditor.feedback.success.saved'));
          this.loadCampos(true);
          this.camposChanged.emit();
        },
        error: (error: HttpErrorResponse) => {
          this.notificationService.showError(error.error?.detail ?? this.translate.instant('mapeamento.mapeamentoEditor.feedback.errors.saveFields'));
        }
      });
  }

  cancelar(): void {
    this.rows = this.originalRows.map((row) => ({ ...row, errors: this.emptyErrors() }));
    this.refreshPreview();
  }

  onExcelUpload(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input?.files?.[0];

    if (!file) {
      return;
    }

    const reader = new FileReader();
    reader.onload = (loadEvent: ProgressEvent<FileReader>) => {
      try {
        const data = new Uint8Array(loadEvent.target!.result as ArrayBuffer);
        const workbook = XLSX.read(data, { type: 'array' });
        const firstSheet = workbook.Sheets[workbook.SheetNames[0]];
        const jsonData = XLSX.utils.sheet_to_json<string[]>(firstSheet, { header: 1 });

        if (jsonData.length > 0) {
          this.excelPreviewData = (jsonData[0] as unknown[]).map((value) => value !== null && value !== undefined ? String(value) : '');
          this.refreshPreview();
          this.notificationService.showSuccess(`${this.translate.instant('mapeamento.mapeamentoEditor.feedback.success.previewLoadedPrefix')} ${this.excelPreviewData.length} ${this.translate.instant('mapeamento.mapeamentoEditor.feedback.success.previewLoadedSuffix')}`);
        } else {
          this.notificationService.showError(this.translate.instant('mapeamento.mapeamentoEditor.feedback.errors.emptyExcel'));
        }
      } catch {
        this.notificationService.showError(this.translate.instant('mapeamento.mapeamentoEditor.feedback.errors.readExcel'));
      }

      input.value = '';
    };

    reader.readAsArrayBuffer(file);
  }

  isFormatoEnabled(row: MapeamentoCampoRow): boolean {
    return !this.readonly && row.tipoCampo === TipoCampo.DateTime;
  }

  trackByRow(_: number, row: MapeamentoCampoRow): number | string {
    return row.id ?? `new-${row.nomeCampo}-${row.indiceColuna}`;
  }

  private loadCampos(showSuccess = false): void {
    this.isLoading = true;
    this.mapeamentoService.getCamposByMapeamento(this.mapeamento.id)
      .pipe(
        finalize(() => { this.isLoading = false; }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (campos) => {
          this.rows = campos
            .sort((left, right) => left.indiceColuna - right.indiceColuna)
            .map((campo) => this.toRow(campo));
          this.snapshotOriginal();
          this.refreshPreview();

          if (showSuccess) {
            this.notificationService.showSuccess(this.translate.instant('mapeamento.mapeamentoEditor.feedback.success.updated'));
          }
        },
        error: (error: HttpErrorResponse) => {
          this.rows = [];
          this.originalRows = [];
          this.notificationService.showError(error.error?.detail ?? this.translate.instant('mapeamento.mapeamentoEditor.feedback.errors.loadFields'));
        }
      });
  }

  private validateRow(row: MapeamentoCampoRow): void {
    const errors = this.emptyErrors();

    if (!row.nomeCampo?.trim()) {
      errors.nomeCampo = this.translate.instant('mapeamento.mapeamentoEditor.validation.fieldNameRequired');
    }

    if (row.indiceColuna === null || row.indiceColuna === undefined || row.indiceColuna < 1) {
      errors.indiceColuna = this.translate.instant('mapeamento.mapeamentoEditor.validation.columnIndexPositive');
    } else {
      const duplicate = this.rows.find((current) => current !== row && current.indiceColuna === row.indiceColuna);
      if (duplicate) {
        errors.indiceColuna = this.translate.instant('mapeamento.mapeamentoEditor.validation.duplicateColumnIndex');
      }
    }

    if (!row.tipoCampo) {
      errors.tipoCampo = this.translate.instant('mapeamento.mapeamentoEditor.validation.fieldTypeRequired');
    }

    if (row.tipoCampo === TipoCampo.DateTime && !row.formato?.trim()) {
      errors.formato = this.translate.instant('mapeamento.mapeamentoEditor.validation.formatRequired');
    }

    row.errors = errors;
  }

  private validateAll(): void {
    this.rows.forEach((row) => this.validateRow(row));
  }

  private refreshPreview(): void {
    for (const row of this.rows) {
      if (row.indiceColuna !== null && row.indiceColuna > 0 && this.excelPreviewData.length > 0) {
        const index = row.indiceColuna - 1;
        row.previewValue = index < this.excelPreviewData.length
          ? this.excelPreviewData[index]
          : this.translate.instant('mapeamento.mapeamentoEditor.preview.emptyColumn');
      } else {
        row.previewValue = '';
      }
    }
  }

  private toRow(campo: MapeamentoCampo): MapeamentoCampoRow {
    return {
      id: campo.id,
      nomeCampo: campo.nomeCampo,
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
      indiceColuna: row.indiceColuna!,
      tipoCampo: row.tipoCampo as number,
      formato: row.tipoCampo === TipoCampo.DateTime ? (row.formato?.trim() || null) : null,
      fk_IdMapeamento: this.mapeamento.id
    };
  }

  private snapshotOriginal(): void {
    this.originalRows = this.rows.map((row) => ({ ...row, errors: this.emptyErrors() }));
  }

  private emptyErrors(): MapeamentoRowErrors {
    return { nomeCampo: '', indiceColuna: '', tipoCampo: '', formato: '' };
  }
}
