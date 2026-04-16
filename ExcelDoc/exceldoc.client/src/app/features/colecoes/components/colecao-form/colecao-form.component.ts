import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { finalize, forkJoin } from 'rxjs';
import { AuthService } from '../../../../core/services/auth.service';
import { NotificationService } from '../../../../core/services/notification.service';
import { Documento } from '../../../documentos/models/documento.model';
import {
  Colecao,
  ColecaoPayload,
  TIPO_COLECAO_OPTIONS,
  TipoColecao,
  getColecaoDocumentoIds,
  isColecaoPadrao,
  resolveTipoColecao,
  toTipoColecaoRequestValue
} from '../../models/colecao.model';
import { ColecaoService } from '../../services/colecao.service';

@Component({
  selector: 'app-colecao-form',
  templateUrl: './colecao-form.component.html',
  styleUrl: './colecao-form.component.css'
})
export class ColecaoFormComponent implements OnInit {
  readonly form = new FormGroup({
    nomeColecao: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(150)] }),
    tipoColecao: new FormControl<TipoColecao>(TipoColecao.Header, { nonNullable: true, validators: [Validators.required] }),
    documentoIds: new FormControl<number[]>([], { nonNullable: true }),
    criarComoPadrao: new FormControl(false, { nonNullable: true })
  });
  readonly tipoColecaoOptions = TIPO_COLECAO_OPTIONS;

  apiError = '';
  documentoError = '';
  permissionMessage = '';
  colecaoId: number | null = null;
  documentos: Documento[] = [];
  isLoading = false;
  isSaving = false;
  readonly isAdministrator: boolean;
  readonly empresaId: number | null;
  readonly empresaNome: string;

  private readonly destroyRef = inject(DestroyRef);
  private currentColecao: Colecao | null = null;
  private readOnlyMode = false;

  constructor(
    private readonly activatedRoute: ActivatedRoute,
    private readonly authService: AuthService,
    private readonly colecaoService: ColecaoService,
    private readonly notificationService: NotificationService,
    private readonly router: Router
  ) {
    const session = this.authService.getSession();
    this.isAdministrator = this.authService.isAdministrator(session);
    this.empresaId = session?.empresaId ?? null;
    this.empresaNome = session?.nomeEmpresa?.trim() ?? '';
  }

  get isEditMode(): boolean {
    return this.colecaoId !== null;
  }

  get pageTitle(): string {
    return this.isEditMode ? 'Editar coleção' : 'Nova coleção';
  }

  get pageSubtitle(): string {
    return this.isEditMode
      ? 'Atualize os dados da coleção e os documentos vinculados.'
      : 'Cadastre uma nova coleção para reutilizar estruturas JSON do SAP.';
  }

  get submitLabel(): string {
    if (this.isSaving) {
      return 'Salvando...';
    }

    return this.isEditMode ? 'Salvar alterações' : 'Salvar coleção';
  }

  get canSubmit(): boolean {
    return !this.readOnlyMode && !this.isSaving;
  }

  get documentosHint(): string {
    if (!this.documentos.length) {
      return 'Nenhum documento disponível para vinculação.';
    }

    return 'Selecione um ou mais documentos que poderão usar esta coleção.';
  }

  get selectedDocumentosLabel(): string {
    const selectedIds = this.form.controls.documentoIds.getRawValue();

    if (!selectedIds.length) {
      return 'Nenhum documento selecionado';
    }

    const selectedDocumentos = this.documentos
      .filter((documento) => selectedIds.includes(documento.id))
      .map((documento) => documento.nomeDocumento);

    if (!selectedDocumentos.length) {
      return `${selectedIds.length} documento(s) selecionado(s)`;
    }

    if (selectedDocumentos.length === 1) {
      return selectedDocumentos[0];
    }

    return `${selectedDocumentos[0]} +${selectedDocumentos.length - 1}`;
  }

  get escopoBadgeLabel(): string {
    return this.isColecaoPadraoSelecionada ? 'Padrão' : 'Minha Empresa';
  }

  get escopoBadgeDescription(): string {
    if (this.isColecaoPadraoSelecionada) {
      return 'Coleção global disponível para todas as empresas.';
    }

    if (this.empresaNome) {
      return `Coleção customizada da empresa ${this.empresaNome}.`;
    }

    return 'Coleção customizada vinculada à empresa do usuário.';
  }

  get isColecaoPadraoSelecionada(): boolean {
    return this.isAdministrator && this.form.controls.criarComoPadrao.getRawValue();
  }

  get hasCompanyScopeUnavailable(): boolean {
    return !this.isColecaoPadraoSelecionada && !this.empresaId;
  }

  ngOnInit(): void {
    if (!this.isAdministrator) {
      this.form.controls.criarComoPadrao.disable({ emitEvent: false });
    }

    const rawId = this.activatedRoute.snapshot.paramMap.get('id');

    if (!rawId) {
      this.loadCreateData();
      return;
    }

    const colecaoId = Number(rawId);
    if (!Number.isInteger(colecaoId) || colecaoId <= 0) {
      this.notificationService.showError('Coleção inválida para edição.');
      void this.router.navigate(['/colecoes']);
      return;
    }

    this.colecaoId = colecaoId;
    this.loadEditData(colecaoId);
  }

  isInvalid(controlName: 'nomeColecao' | 'tipoColecao'): boolean {
    const control = this.form.controls[controlName];
    return control.invalid && (control.dirty || control.touched);
  }

  getErrorMessage(controlName: 'nomeColecao' | 'tipoColecao'): string {
    const control = this.form.controls[controlName];

    if (control.hasError('required')) {
      return controlName === 'nomeColecao'
        ? 'Informe o nome da coleção.'
        : 'Selecione o tipo da coleção.';
    }

    if (control.hasError('maxlength')) {
      return 'O nome da coleção deve ter no máximo 150 caracteres.';
    }

    return 'Campo inválido.';
  }

  cancel(): void {
    void this.router.navigate(['/colecoes']);
  }

  submit(): void {
    this.apiError = '';

    if (!this.canSubmit) {
      return;
    }

    if (this.hasCompanyScopeUnavailable) {
      this.apiError = 'Não foi possível identificar a empresa do usuário para criar uma coleção customizada.';
      this.notificationService.showError(this.apiError);
      return;
    }

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const payload: ColecaoPayload = {
      nomeColecao: this.form.controls.nomeColecao.getRawValue().trim(),
      tipoColecao: toTipoColecaoRequestValue(this.form.controls.tipoColecao.getRawValue()),
      fk_IdEmpresa: this.isColecaoPadraoSelecionada ? null : this.empresaId,
      documentoIds: this.form.controls.documentoIds.getRawValue()
    };

    const request$ = this.isEditMode && this.colecaoId !== null
      ? this.colecaoService.update(this.colecaoId, payload)
      : this.colecaoService.create(payload);

    this.isSaving = true;
    request$
      .pipe(
        finalize(() => {
          this.isSaving = false;
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: () => {
          this.notificationService.showSuccess(
            this.isEditMode ? 'Coleção atualizada com sucesso.' : 'Coleção criada com sucesso.'
          );
          void this.router.navigate(['/colecoes']);
        },
        error: (error: HttpErrorResponse) => {
          this.apiError = error.error?.detail ?? 'Não foi possível salvar a coleção.';
          this.notificationService.showError(this.apiError);
        }
      });
  }

  private applyReadOnlyMode(message: string): void {
    this.readOnlyMode = true;
    this.permissionMessage = message;
    this.form.disable({ emitEvent: false });
  }

  private loadCreateData(): void {
    if (!this.isAdministrator && !this.empresaId) {
      this.applyReadOnlyMode('O usuário atual não está vinculado a uma empresa para criar coleções customizadas.');
    }

    this.loadDocumentos();
  }

  private loadDocumentos(): void {
    this.apiError = '';
    this.documentoError = '';
    this.isLoading = true;

    this.colecaoService.getDocumentos()
      .pipe(
        finalize(() => {
          this.isLoading = false;
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (documentos: Documento[]) => {
          this.documentos = [...documentos].sort((left, right) => left.nomeDocumento.localeCompare(right.nomeDocumento));
        },
        error: (error: HttpErrorResponse) => {
          this.documentoError = error.error?.detail ?? 'Não foi possível carregar a lista de documentos.';
          this.notificationService.showError(this.documentoError);
        }
      });
  }

  private loadEditData(colecaoId: number): void {
    this.apiError = '';
    this.documentoError = '';
    this.permissionMessage = '';
    this.isLoading = true;

    forkJoin({
      colecao: this.colecaoService.getById(colecaoId),
      documentos: this.colecaoService.getDocumentos()
    })
      .pipe(
        finalize(() => {
          this.isLoading = false;
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: ({ colecao, documentos }) => {
          this.currentColecao = colecao;
          this.documentos = [...documentos].sort((left, right) => left.nomeDocumento.localeCompare(right.nomeDocumento));
          this.form.reset({
            nomeColecao: colecao.nomeColecao,
            tipoColecao: resolveTipoColecao(colecao.tipoColecao),
            documentoIds: getColecaoDocumentoIds(colecao),
            criarComoPadrao: isColecaoPadrao(colecao)
          });

          if (!this.isAdministrator) {
            this.form.controls.criarComoPadrao.disable({ emitEvent: false });
          }

          if (!this.canEditColecao(colecao)) {
            this.applyReadOnlyMode('Coleções padrão do sistema não podem ser alteradas por usuários comuns.');
          }
        },
        error: (error: HttpErrorResponse) => {
          this.apiError = error.error?.detail ?? 'Não foi possível carregar a coleção.';
          this.notificationService.showError(this.apiError);
        }
      });
  }

  private canEditColecao(colecao: Colecao): boolean {
    return this.isAdministrator || !isColecaoPadrao(colecao);
  }
}
