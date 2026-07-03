import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, FormControl, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService } from '../../../../core/services/auth.service';
import { NotificationService } from '../../../../core/services/notification.service';
import { TranslateService } from '../../../../core/services/translate.service';
import { Colecao, TipoColecao, resolveTipoColecao } from '../../../colecoes/models/colecao.model';
import { ColecaoService } from '../../../colecoes/services/colecao.service';
import { Documento } from '../../../documentos/models/documento.model';
import { DocumentoService } from '../../../documentos/services/documento.service';
import { Mapeamento } from '../../../mapeamento/models/mapeamento.model';
import { MapeamentoService } from '../../../mapeamento/services/mapeamento.service';
import {
  PerfilMapeamento,
  PerfilMapeamentoItemPayload,
  PerfilMapeamentoPayload
} from '../../models/perfil-mapeamento.model';
import { PerfilMapeamentoService } from '../../services/perfil-mapeamento.service';

interface HeaderColecaoOption {
  colecao: Colecao;
  mapeamentos: Mapeamento[];
  isLoadingMapeamentos: boolean;
  initialMapeamentoId: number | null;
}

interface LineColecaoMapeamentoGroup {
  colecao: Colecao;
  mapeamentos: Mapeamento[];
  isLoadingMapeamentos: boolean;
  initialMapeamentoId: number | null;
  selectedControl: FormControl<boolean>;
  mapeamentoControl: FormControl<number | null>;
}

@Component({
  selector: 'app-perfil-form',
  templateUrl: './perfil-form.component.html',
  styleUrl: './perfil-form.component.css'
})
export class PerfilFormComponent implements OnInit {
  form!: FormGroup;
  documentos: Documento[] = [];
  headerColecoes: HeaderColecaoOption[] = [];
  lineColecaoGroups: LineColecaoMapeamentoGroup[] = [];
  perfil: PerfilMapeamento | null = null;
  isEditMode = false;
  isReadonly = false;
  isLoading = false;
  isSaving = false;
  isLoadingDocumentos = false;
  isLoadingColecoes = false;

  private readonly destroyRef = inject(DestroyRef);

  constructor(
    private readonly fb: FormBuilder,
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly perfilService: PerfilMapeamentoService,
    private readonly documentoService: DocumentoService,
    private readonly colecaoService: ColecaoService,
    private readonly mapeamentoService: MapeamentoService,
    private readonly notificationService: NotificationService,
    private readonly authService: AuthService,
    private readonly translate: TranslateService
  ) { }

  get isAdministrator(): boolean {
    return this.authService.isAdministrator();
  }

  get empresaId(): number | null {
    return this.authService.getSession()?.empresaId ?? null;
  }

  get headerColecaoControl(): FormControl<number | null> {
    return this.form.controls['headerColecaoId'] as FormControl<number | null>;
  }

  get headerMapeamentoControl(): FormControl<number | null> {
    return this.form.controls['headerMapeamentoId'] as FormControl<number | null>;
  }

  get selectedHeaderColecao(): HeaderColecaoOption | null {
    return this.headerColecoes.find((option) => option.colecao.id === this.headerColecaoControl.value) ?? null;
  }

  get selectedHeaderMapeamentos(): Mapeamento[] {
    return this.selectedHeaderColecao?.mapeamentos ?? [];
  }

  get hasHeaderColecoes(): boolean {
    return this.headerColecoes.length > 0;
  }

  get hasLineColecoes(): boolean {
    return this.lineColecaoGroups.length > 0;
  }

  get selectedLineGroups(): LineColecaoMapeamentoGroup[] {
    return this.lineColecaoGroups.filter((group) => group.selectedControl.value);
  }

  get isFormValid(): boolean {
    if (!this.form.controls['nome'].valid || !this.form.controls['documentoId'].valid) {
      return false;
    }

    if (this.hasHeaderColecoes && (!this.headerColecaoControl.value || !this.headerMapeamentoControl.value)) {
      return false;
    }

    if (this.hasLineColecoes && this.selectedLineGroups.length === 0) {
      return false;
    }

    return this.selectedLineGroups.every((group) => group.mapeamentoControl.value !== null);
  }

  get pageTitle(): string {
    if (this.isReadonly) return this.translate.instant('perfilMapeamento.perfilForm.title.view');
    return this.isEditMode
      ? this.translate.instant('perfilMapeamento.perfilForm.title.edit')
      : this.translate.instant('perfilMapeamento.perfilForm.title.create');
  }

  ngOnInit(): void {
    this.form = this.fb.group({
      nome: ['', [Validators.required, Validators.maxLength(150)]],
      documentoId: [null, Validators.required],
      headerColecaoId: [null as number | null],
      headerMapeamentoId: [null as number | null]
    });

    this.form.controls['documentoId'].valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((documentoId) => {
        if (documentoId && !this.isEditMode) {
          this.loadColecoesForDocumento(documentoId as number);
        }
      });

    this.headerColecaoControl.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((colecaoId) => {
        this.syncHeaderMapeamentoSelection((colecaoId as number | null) ?? null);
      });

    this.loadDocumentos();
  }

  salvar(): void {
    if (!this.isFormValid || this.isReadonly) return;

    const payload: PerfilMapeamentoPayload = {
      nome: this.form.value.nome,
      fk_IdDocumento: this.form.value.documentoId,
      itens: this.buildItensPayload()
    };

    this.isSaving = true;
    const request$ = this.isEditMode
      ? this.perfilService.update(this.perfil!.id, payload)
      : this.perfilService.create(payload);

    request$
      .pipe(
        finalize(() => { this.isSaving = false; }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: () => {
          this.notificationService.showSuccess(
            this.isEditMode
              ? this.translate.instant('perfilMapeamento.perfilForm.feedback.success.updated')
              : this.translate.instant('perfilMapeamento.perfilForm.feedback.success.created')
          );
          void this.router.navigate(['/perfil-mapeamento']);
        },
        error: (err: HttpErrorResponse) => {
          this.notificationService.showError(err.error?.detail ?? this.translate.instant('perfilMapeamento.perfilForm.feedback.errors.saveProfile'));
        }
      });
  }

  cancelar(): void {
    void this.router.navigate(['/perfil-mapeamento']);
  }

  getTipoColecaoLabel(colecao: Colecao): string {
    const key = resolveTipoColecao(colecao.tipoColecao) === TipoColecao.Line ? 'line' : 'header';
    return this.translate.instant(`perfilMapeamento.collectionTypes.${key}`);
  }

  getMapeamentoBadgeClass(mapeamento: Mapeamento): string {
    return mapeamento.isPadrao ? 'mapping-chip mapping-chip--padrao' : 'mapping-chip mapping-chip--empresa';
  }

  getMapeamentoBadgeLabel(mapeamento: Mapeamento): string {
    return mapeamento.isPadrao
      ? this.translate.instant('perfilMapeamento.common.scope.default')
      : this.translate.instant('perfilMapeamento.common.scope.company');
  }

  onLineColecaoSelectionChange(group: LineColecaoMapeamentoGroup): void {
    if (this.isReadonly) {
      return;
    }

    if (group.selectedControl.value) {
      group.mapeamentoControl.enable({ emitEvent: false });

      if (!group.mapeamentoControl.value) {
        group.mapeamentoControl.setValue(this.getDefaultMapeamentoId(group.mapeamentos), { emitEvent: false });
      }

      return;
    }

    group.mapeamentoControl.disable({ emitEvent: false });
  }

  private buildItensPayload(): PerfilMapeamentoItemPayload[] {
    const itens: PerfilMapeamentoItemPayload[] = [];
    const selectedHeader = this.selectedHeaderColecao;
    const selectedHeaderMapeamentoId = this.headerMapeamentoControl.value;

    if (selectedHeader && selectedHeaderMapeamentoId) {
      itens.push({
        fk_IdColecao: selectedHeader.colecao.id,
        fk_IdMapeamento: selectedHeaderMapeamentoId
      });
    }

    this.selectedLineGroups.forEach((group) => {
      const mapeamentoId = group.mapeamentoControl.value;

      if (!mapeamentoId) {
        return;
      }

      itens.push({
        fk_IdColecao: group.colecao.id,
        fk_IdMapeamento: mapeamentoId
      });
    });

    return itens;
  }

  private loadDocumentos(): void {
    this.isLoadingDocumentos = true;
    this.documentoService.getAll()
      .pipe(
        finalize(() => { this.isLoadingDocumentos = false; }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (docs) => {
          this.documentos = docs;
          this.checkEditMode();
        },
        error: () => {
          this.notificationService.showError(this.translate.instant('perfilMapeamento.perfilForm.feedback.errors.loadDocuments'));
        }
      });
  }

  private checkEditMode(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id && id !== 'novo') {
      this.isEditMode = true;
      this.loadPerfil(+id);
    }
  }

  private loadPerfil(id: number): void {
    this.isLoading = true;
    this.perfilService.getById(id)
      .pipe(
        finalize(() => { this.isLoading = false; }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (perfil) => {
          this.perfil = perfil;
          this.isReadonly = perfil.isPadrao && !this.isAdministrator;

          this.form.patchValue({
            nome: perfil.nome,
            documentoId: perfil.fk_IdDocumento
          });

          if (this.isReadonly) {
            this.form.disable();
          }

          this.loadColecoesForDocumento(perfil.fk_IdDocumento, perfil);
        },
        error: () => {
          this.notificationService.showError(this.translate.instant('perfilMapeamento.perfilForm.feedback.errors.loadProfile'));
          void this.router.navigate(['/perfil-mapeamento']);
        }
      });
  }

  private loadColecoesForDocumento(documentoId: number, perfil?: PerfilMapeamento): void {
    this.isLoadingColecoes = true;
    this.resetColecoesState();

    this.colecaoService.getAll()
      .pipe(
        finalize(() => { this.isLoadingColecoes = false; }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (colecoes) => {
          const doc = this.documentos.find((documento) => documento.id === documentoId);
          const docColecaoIds = doc?.colecaoIds ?? doc?.colecoes?.map((colecao) => colecao.id) ?? [];

          const colecoesDoDocumento = colecoes
            .filter((colecao) => docColecaoIds.includes(colecao.id))
            .sort((left, right) => left.nomeColecao.localeCompare(right.nomeColecao, 'pt-BR', { sensitivity: 'base' }));

          this.configureHeaderColecoes(colecoesDoDocumento, perfil);
          this.configureLineColecoes(colecoesDoDocumento, perfil);
        },
        error: () => {
          this.notificationService.showError(this.translate.instant('perfilMapeamento.perfilForm.feedback.errors.loadCollections'));
        }
      });
  }

  private resetColecoesState(): void {
    this.headerColecoes = [];
    this.lineColecaoGroups = [];
    this.headerColecaoControl.setValue(null, { emitEvent: false });
    this.headerMapeamentoControl.setValue(null, { emitEvent: false });
    this.headerColecaoControl.disable({ emitEvent: false });
    this.headerMapeamentoControl.disable({ emitEvent: false });
  }

  private configureHeaderColecoes(colecoes: Colecao[], perfil?: PerfilMapeamento): void {
    const headerColecoes = colecoes.filter((colecao) => resolveTipoColecao(colecao.tipoColecao) === TipoColecao.Header);

    this.headerColecoes = headerColecoes.map((colecao) => ({
      colecao,
      mapeamentos: [],
      isLoadingMapeamentos: true,
      initialMapeamentoId: perfil?.itens?.find((item) => item.fk_IdColecao === colecao.id)?.fk_IdMapeamento ?? null
    }));

    if (!this.headerColecoes.length) {
      return;
    }

    const selectedHeader = this.headerColecoes.find((option) => option.initialMapeamentoId !== null) ?? this.headerColecoes[0];
    this.headerColecaoControl.setValue(selectedHeader.colecao.id);

    if (!this.isReadonly) {
      this.headerColecaoControl.enable({ emitEvent: false });
    }

    this.headerColecoes.forEach((option) => {
      this.loadMapeamentosForHeader(option, !perfil);
    });

    this.syncHeaderMapeamentoSelection(selectedHeader.colecao.id);
  }

  private configureLineColecoes(colecoes: Colecao[], perfil?: PerfilMapeamento): void {
    const lineColecoes = colecoes.filter((colecao) => resolveTipoColecao(colecao.tipoColecao) === TipoColecao.Line);

    this.lineColecaoGroups = lineColecoes.map((colecao) => {
      const existingItem = perfil?.itens?.find((item) => item.fk_IdColecao === colecao.id);
      const isSelected = !!existingItem;

      const selectedControl = new FormControl<boolean>(
        { value: isSelected, disabled: this.isReadonly },
        { nonNullable: true }
      );

      const mapeamentoControl = new FormControl<number | null>({
        value: existingItem?.fk_IdMapeamento ?? null,
        disabled: this.isReadonly || !isSelected
      });

      return {
        colecao,
        mapeamentos: [],
        isLoadingMapeamentos: true,
        initialMapeamentoId: existingItem?.fk_IdMapeamento ?? null,
        selectedControl,
        mapeamentoControl
      };
    });

    this.lineColecaoGroups.forEach((group) => {
      this.loadMapeamentosForLine(group, !perfil);
    });
  }

  private loadMapeamentosForHeader(option: HeaderColecaoOption, shouldApplyDefaultSelection: boolean): void {
    this.mapeamentoService.getMapeamentosByColecao(option.colecao.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (mapeamentos) => {
          option.mapeamentos = mapeamentos.filter((mapeamento) => this.isMapeamentoVisible());
          option.isLoadingMapeamentos = false;

          if (this.headerColecaoControl.value === option.colecao.id) {
            this.syncHeaderMapeamentoSelection(option.colecao.id, shouldApplyDefaultSelection);
          }
        },
        error: () => {
          option.isLoadingMapeamentos = false;
          this.notificationService.showError(`${this.translate.instant('perfilMapeamento.perfilForm.feedback.errors.loadCollectionMappingsPrefix')} ${option.colecao.nomeColecao}.`);
        }
      });
  }

  private loadMapeamentosForLine(group: LineColecaoMapeamentoGroup, shouldApplyDefaultSelection: boolean): void {
    this.mapeamentoService.getMapeamentosByColecao(group.colecao.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (mapeamentos) => {
          group.mapeamentos = mapeamentos.filter((mapeamento) => this.isMapeamentoVisible());
          group.isLoadingMapeamentos = false;

          if (group.initialMapeamentoId && group.mapeamentos.some((mapeamento) => mapeamento.id === group.initialMapeamentoId)) {
            group.mapeamentoControl.setValue(group.initialMapeamentoId, { emitEvent: false });
          }

          if (shouldApplyDefaultSelection && group.selectedControl.value && !group.mapeamentoControl.value) {
            group.mapeamentoControl.setValue(this.getDefaultMapeamentoId(group.mapeamentos), { emitEvent: false });
          }
        },
        error: () => {
          group.isLoadingMapeamentos = false;
          this.notificationService.showError(`${this.translate.instant('perfilMapeamento.perfilForm.feedback.errors.loadCollectionMappingsPrefix')} ${group.colecao.nomeColecao}.`);
        }
      });
  }

  private syncHeaderMapeamentoSelection(colecaoId: number | null, shouldApplyDefaultSelection = true): void {
    const option = this.headerColecoes.find((item) => item.colecao.id === colecaoId) ?? null;

    if (!option) {
      this.headerMapeamentoControl.setValue(null, { emitEvent: false });
      this.headerMapeamentoControl.disable({ emitEvent: false });
      return;
    }

    if (!this.isReadonly) {
      this.headerMapeamentoControl.enable({ emitEvent: false });
    }

    const currentValue = this.headerMapeamentoControl.value;
    const hasCurrentValue = option.mapeamentos.some((mapeamento) => mapeamento.id === currentValue);
    const nextValue = option.initialMapeamentoId
      ?? (hasCurrentValue ? currentValue : null)
      ?? (shouldApplyDefaultSelection ? this.getDefaultMapeamentoId(option.mapeamentos) : null);

    this.headerMapeamentoControl.setValue(nextValue, { emitEvent: false });
  }

  private getDefaultMapeamentoId(mapeamentos: Mapeamento[]): number | null {
    return mapeamentos.find((mapeamento) => mapeamento.isPadrao)?.id ?? mapeamentos[0]?.id ?? null;
  }

  private isMapeamentoVisible(): boolean {
    return true;
  }
}
