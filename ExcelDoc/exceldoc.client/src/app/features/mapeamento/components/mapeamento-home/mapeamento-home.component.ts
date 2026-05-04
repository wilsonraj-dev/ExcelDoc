import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatDialog } from '@angular/material/dialog';
import { ActivatedRoute, Router } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService } from '../../../../core/services/auth.service';
import { NotificationService } from '../../../../core/services/notification.service';
import { ColecaoService } from '../../../colecoes/services/colecao.service';
import {
  Mapeamento,
  MapeamentoPayload,
  getMapeamentoEmpresaId,
  isMapeamentoEmpresa,
  orderMapeamentos
} from '../../models/mapeamento.model';
import { MapeamentoService } from '../../services/mapeamento.service';
import {
  NovoMapeamentoDialogComponent,
  NovoMapeamentoDialogResult
} from '../novo-mapeamento-dialog/novo-mapeamento-dialog.component';
import { ConfirmDialogComponent } from '../../../../shared/components/confirm-dialog/confirm-dialog.component';

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
    private readonly notificationService: NotificationService
  ) {}

  ngOnInit(): void {
    this.colecaoId = Number(this.route.snapshot.paramMap.get('colecaoId'));

    if (!Number.isInteger(this.colecaoId) || this.colecaoId <= 0) {
      this.notificationService.showError('Coleção inválida.');
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

    if (mapeamento.isPadrao) {
      return false;
    }

    return getMapeamentoEmpresaId(mapeamento) === this.empresaId;
  }

  get isReadOnlySelectedMapeamento(): boolean {
    return !this.canEditSelectedMapeamento;
  }

  get selectedMapeamentoTipoLabel(): string {
    return this.selectedMapeamento?.isPadrao ? 'Padrão' : 'Minha Empresa';
  }

  onMapeamentoSelectionChange(mapeamentoId: number | null): void {
    this.selectedMapeamentoId = mapeamentoId;
  }

  openNovoMapeamentoDialog(): void {
    if (!this.canCreateMapeamento || this.empresaId === null) {
      this.notificationService.showError('Não foi possível identificar a empresa do usuário para criar o mapeamento.');
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

    this.isSubmitting = true;
    this.mapeamentoService.cloneMapeamento(mapeamento.id)
      .pipe(
        finalize(() => { this.isSubmitting = false; }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (novoMapeamento) => {
          this.notificationService.showSuccess('Mapeamento clonado com sucesso.');
          this.mergeAndSelectMapeamento(novoMapeamento.id);
        },
        error: (error: HttpErrorResponse) => {
          this.notificationService.showError(error.error?.detail ?? 'Falha ao clonar o mapeamento.');
        }
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
        title: 'Excluir mapeamento',
        message: `Deseja realmente excluir o mapeamento "${mapeamento.nome}"?`,
        confirmLabel: 'Excluir',
        cancelLabel: 'Cancelar'
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
    return mapeamento.isPadrao ? 'Padrão' : 'Minha Empresa';
  }

  isMapeamentoVisible(mapeamento: Mapeamento): boolean {
    if (this.isAdministrator) {
      return true;
    }

    if (mapeamento.isPadrao) {
      return true;
    }

    return getMapeamentoEmpresaId(mapeamento) === this.empresaId;
  }

  getCampoQuantidadeLabel(mapeamento: Mapeamento): string {
    const quantidade = mapeamento.quantidadeCampos ?? 0;
    return quantidade === 1 ? '1 campo' : `${quantidade} campos`;
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
          this.colecaoNome = `Coleção #${this.colecaoId}`;
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
          const visiveis = orderMapeamentos(mapeamentos.filter((item) => this.isMapeamentoVisible(item)));
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
          this.loadError = error.error?.detail ?? 'Falha ao carregar os mapeamentos da coleção.';
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
          this.notificationService.showSuccess('Mapeamento criado com sucesso.');
          this.mergeAndSelectMapeamento(mapeamento.id);
        },
        error: (error: HttpErrorResponse) => {
          this.notificationService.showError(error.error?.detail ?? 'Falha ao criar o mapeamento.');
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
          this.notificationService.showSuccess('Mapeamento excluído com sucesso.');
          this.loadMapeamentos();
        },
        error: (error: HttpErrorResponse) => {
          this.notificationService.showError(error.error?.detail ?? 'Falha ao excluir o mapeamento.');
        }
      });
  }

  private mergeAndSelectMapeamento(mapeamentoId: number): void {
    this.loadMapeamentos(mapeamentoId);
  }

  protected readonly isMapeamentoEmpresa = isMapeamentoEmpresa;
}
