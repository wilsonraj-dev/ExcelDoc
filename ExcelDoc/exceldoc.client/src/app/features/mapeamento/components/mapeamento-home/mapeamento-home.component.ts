import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatDialog } from '@angular/material/dialog';
import { ActivatedRoute, Router } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService } from '../../../../core/services/auth.service';
import { NotificationService } from '../../../../core/services/notification.service';
import { TranslateService } from '../../../../core/services/translate.service';
import { ColecaoService } from '../../../colecoes/services/colecao.service';
import {
  Mapeamento,
  MapeamentoPayload,
  isMapeamentoEmpresa,
  orderMapeamentos
} from '../../models/mapeamento.model';
import { MapeamentoService } from '../../services/mapeamento.service';
import {
  NovoMapeamentoDialogComponent,
  NovoMapeamentoDialogResult
} from '../novo-mapeamento-dialog/novo-mapeamento-dialog.component';
import { ConfirmDialogComponent } from '../../../../shared/components/confirm-dialog/confirm-dialog.component';
import {
  CloneNameDialogComponent,
  CloneNameDialogResult
} from '../../../../shared/components/clone-name-dialog/clone-name-dialog.component';

@Component({
  selector: 'app-mapeamento-home',
  templateUrl: './mapeamento-home.component.html',
  styleUrl: './mapeamento-home.component.css'
})
export class MapeamentoHomeComponent implements OnInit {
  colecaoId!: number;
  colecaoNome = '';
  mapeamentos: Mapeamento[] = [];
  selectedMapeamentoId: number | null = null;
  isLoading = false;
  isLoadingMapeamentos = false;
  isSubmitting = false;
  loadError = '';

  private readonly destroyRef = inject(DestroyRef);

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly dialog: MatDialog,
    private readonly authService: AuthService,
    private readonly colecaoService: ColecaoService,
    private readonly mapeamentoService: MapeamentoService,
    private readonly notificationService: NotificationService,
    private readonly translate: TranslateService
  ) { }

  ngOnInit(): void {
    this.colecaoId = Number(this.route.snapshot.paramMap.get('colecaoId'));

    if (!Number.isInteger(this.colecaoId) || this.colecaoId <= 0) {
      this.notificationService.showError(this.translate.instant('mapeamento.mapeamentoHome.feedback.errors.invalidCollection'));
      void this.router.navigate(['/mapeamento']);
      return;
    }

    this.loadColecao();
    this.loadMapeamentos();
  }

  get selectedMapeamento(): Mapeamento | null {
    return this.mapeamentos.find((item) => item.id === this.selectedMapeamentoId) ?? null;
  }

  get isAdministrator(): boolean {
    return this.authService.isAdministrator();
  }

  get empresaId(): number | null {
    return this.authService.getSession()?.empresaId ?? null;
  }

  get canCreateMapeamento(): boolean {
    return this.isAdministrator || this.empresaId !== null;
  }

  get canCloneMapeamento(): boolean {
    return !!this.selectedMapeamento && this.canCreateMapeamento;
  }

  get canDeleteMapeamento(): boolean {
    return !!this.selectedMapeamento && !this.selectedMapeamento.isPadrao && this.canEditSelectedMapeamento;
  }

  get canEditSelectedMapeamento(): boolean {
    const mapeamento = this.selectedMapeamento;

    if (!mapeamento) {
      return false;
    }

    if (this.isAdministrator) {
      return true;
    }

    return !mapeamento.isPadrao;
  }

  get isReadOnlySelectedMapeamento(): boolean {
    return !this.canEditSelectedMapeamento;
  }

  get selectedMapeamentoTipoLabel(): string {
    return this.selectedMapeamento?.isPadrao
      ? this.translate.instant('mapeamento.common.scope.default')
      : this.translate.instant('mapeamento.common.scope.myCompany');
  }

  onMapeamentoSelectionChange(mapeamentoId: number | null): void {
    this.selectedMapeamentoId = mapeamentoId;
  }

  openNovoMapeamentoDialog(): void {
    if (!this.canCreateMapeamento || this.empresaId === null) {
      this.notificationService.showError(this.translate.instant('mapeamento.mapeamentoHome.feedback.errors.identifyCompany'));
      return;
    }

    this.dialog.open(NovoMapeamentoDialogComponent, {
      width: '420px',
      data: {
        exibirCampoPadrao: this.isAdministrator
      }
    }).afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result?: NovoMapeamentoDialogResult) => {
        if (!result?.nome?.trim()) {
          return;
        }

        this.createMapeamento(result);
      });
  }

  cloneSelectedMapeamento(): void {
    const mapeamento = this.selectedMapeamento;

    if (!mapeamento || !this.canCloneMapeamento) {
      return;
    }

    this.dialog.open(CloneNameDialogComponent, {
      width: '360px',
      data: {
        title: this.translate.instant('mapeamento.mapeamentoHome.cloneDialog.title'),
        label: this.translate.instant('mapeamento.mapeamentoHome.cloneDialog.label'),
        initialValue: `${mapeamento.nome}`,
        confirmLabel: this.translate.instant('mapeamento.mapeamentoHome.actions.clone')
      }
    }).afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result?: CloneNameDialogResult) => {
        const nome = result?.nome?.trim();

        if (!nome) {
          return;
        }

        this.isSubmitting = true;
        this.mapeamentoService.cloneMapeamento(mapeamento.id, nome)
          .pipe(
            finalize(() => { this.isSubmitting = false; }),
            takeUntilDestroyed(this.destroyRef)
          )
          .subscribe({
            next: (novoMapeamento) => {
              this.notificationService.showSuccess(this.translate.instant('mapeamento.mapeamentoHome.feedback.success.cloned'));
              this.mergeAndSelectMapeamento(novoMapeamento.id);
            },
            error: (error: HttpErrorResponse) => {
              this.notificationService.showError(error.error?.detail ?? this.translate.instant('mapeamento.mapeamentoHome.feedback.errors.cloneMapping'));
            }
          });
      });
  }

  confirmDeleteSelectedMapeamento(): void {
    const mapeamento = this.selectedMapeamento;

    if (!mapeamento || !this.canDeleteMapeamento) {
      return;
    }

    this.dialog.open(ConfirmDialogComponent, {
      width: '420px',
      data: {
        title: this.translate.instant('mapeamento.mapeamentoHome.confirmDelete.title'),
        message: `${this.translate.instant('mapeamento.mapeamentoHome.confirmDelete.messagePrefix')} "${mapeamento.nome}"?`,
        confirmLabel: this.translate.instant('mapeamento.mapeamentoHome.actions.delete'),
        cancelLabel: this.translate.instant('mapeamento.mapeamentoHome.actions.cancel')
      }
    }).afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((confirmed) => {
        if (!confirmed) {
          return;
        }

        this.deleteMapeamento(mapeamento);
      });
  }

  compareMapeamento(optionValue: number | null, selectedValue: number | null): boolean {
    return optionValue === selectedValue;
  }

  getMapeamentoBadgeClass(mapeamento: Mapeamento): string {
    return mapeamento.isPadrao ? 'chip chip--padrao' : 'chip chip--empresa';
  }

  getMapeamentoBadgeLabel(mapeamento: Mapeamento): string {
    return mapeamento.isPadrao
      ? this.translate.instant('mapeamento.common.scope.default')
      : this.translate.instant('mapeamento.common.scope.myCompany');
  }

  getCampoQuantidadeLabel(mapeamento: Mapeamento): string {
    const quantidade = mapeamento.quantidadeCampos ?? 0;
    return quantidade === 1
      ? this.translate.instant('mapeamento.mapeamentoHome.summary.oneField')
      : `${quantidade} ${this.translate.instant('mapeamento.mapeamentoHome.summary.fieldsSuffix')}`;
  }

  private loadColecao(): void {
    this.isLoading = true;
    this.colecaoService.getById(this.colecaoId)
      .pipe(
        finalize(() => { this.isLoading = false; }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (colecao) => {
          this.colecaoNome = colecao.nomeColecao;
        },
        error: () => {
          this.colecaoNome = '';
        }
      });
  }

  loadMapeamentos(selectedId?: number): void {
    this.isLoadingMapeamentos = true;
    this.loadError = '';

    this.mapeamentoService.getMapeamentosByColecao(this.colecaoId)
      .pipe(
        finalize(() => { this.isLoadingMapeamentos = false; }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (mapeamentos) => {
          const visiveis = orderMapeamentos(mapeamentos);
          this.mapeamentos = visiveis;

          if (!visiveis.length) {
            this.selectedMapeamentoId = null;
            return;
          }

          this.selectedMapeamentoId = selectedId
            ?? (visiveis.some((item) => item.id === this.selectedMapeamentoId) ? this.selectedMapeamentoId : visiveis[0].id);
        },
        error: (error: HttpErrorResponse) => {
          this.mapeamentos = [];
          this.selectedMapeamentoId = null;
          this.loadError = error.error?.detail ?? this.translate.instant('mapeamento.mapeamentoHome.feedback.errors.loadMappings');
          this.notificationService.showError(this.loadError);
        }
      });
  }

  private createMapeamento(result: NovoMapeamentoDialogResult): void {
    const payload: MapeamentoPayload = {
      nome: result.nome,
      fk_IdColecao: this.colecaoId,
      fk_IdEmpresa: result.isPadrao ? null : this.empresaId,
      isPadrao: this.isAdministrator ? result.isPadrao : false
    };

    this.isSubmitting = true;
    this.mapeamentoService.createMapeamento(payload)
      .pipe(
        finalize(() => { this.isSubmitting = false; }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (mapeamento) => {
          this.notificationService.showSuccess(this.translate.instant('mapeamento.mapeamentoHome.feedback.success.created'));
          this.mergeAndSelectMapeamento(mapeamento.id);
        },
        error: (error: HttpErrorResponse) => {
          this.notificationService.showError(error.error?.detail ?? this.translate.instant('mapeamento.mapeamentoHome.feedback.errors.createMapping'));
        }
      });
  }

  private deleteMapeamento(mapeamento: Mapeamento): void {
    this.isSubmitting = true;
    this.mapeamentoService.deleteMapeamento(mapeamento.id)
      .pipe(
        finalize(() => { this.isSubmitting = false; }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: () => {
          this.notificationService.showSuccess(this.translate.instant('mapeamento.mapeamentoHome.feedback.success.deleted'));
          this.loadMapeamentos();
        },
        error: (error: HttpErrorResponse) => {
          this.notificationService.showError(error.error?.detail ?? this.translate.instant('mapeamento.mapeamentoHome.feedback.errors.deleteMapping'));
        }
      });
  }

  private mergeAndSelectMapeamento(mapeamentoId: number): void {
    this.loadMapeamentos(mapeamentoId);
  }

  protected readonly isMapeamentoEmpresa = isMapeamentoEmpresa;
}
