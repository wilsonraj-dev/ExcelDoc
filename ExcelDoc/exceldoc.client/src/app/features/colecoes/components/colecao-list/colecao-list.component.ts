import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, OnInit, ViewChild, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup } from '@angular/forms';
import { MatDialog } from '@angular/material/dialog';
import { MatPaginator } from '@angular/material/paginator';
import { MatTableDataSource } from '@angular/material/table';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService } from '../../../../core/services/auth.service';
import { NotificationService } from '../../../../core/services/notification.service';
import { TranslateService } from '../../../../core/services/translate.service';
import { ConfirmDialogComponent } from '../../../../shared/components/confirm-dialog/confirm-dialog.component';
import {
  Colecao,
  TIPO_COLECAO_OPTIONS,
  TipoColecao,
  getColecaoEmpresaId,
  isColecaoPadrao,
  resolveTipoColecao
} from '../../models/colecao.model';
import { ColecaoService } from '../../services/colecao.service';

interface ColecaoListFilter {
  termo: string;
  tipoColecao: string;
}

@Component({
  selector: 'app-colecao-list',
  templateUrl: './colecao-list.component.html',
  styleUrl: './colecao-list.component.css'
})
export class ColecaoListComponent implements OnInit {
  readonly displayedColumns: string[] = ['nomeColecao', 'tipoColecao', 'tipo', 'mapeamentos', 'acoes'];
  readonly pageSizeOptions = [5, 10, 20];
  readonly dataSource = new MatTableDataSource<Colecao>([]);
  readonly filterForm = new FormGroup({
    termo: new FormControl('', { nonNullable: true }),
    tipoColecao: new FormControl('', { nonNullable: true })
  });
  readonly tipoColecaoOptions: ReadonlyArray<{ value: TipoColecao | '' }> = [
    { value: '' },
    ...TIPO_COLECAO_OPTIONS
  ];
  errorMessage = '';
  isLoading = false;
  deletingColecaoId: number | null = null;
  readonly isAdministrator: boolean;

  private readonly destroyRef = inject(DestroyRef);

  @ViewChild(MatPaginator)
  set paginator(paginator: MatPaginator | undefined) {
    if (paginator) {
      this.dataSource.paginator = paginator;
      this.dataSource.paginator.pageSize = this.pageSizeOptions[0];
    }
  }

  constructor(
    private readonly authService: AuthService,
    private readonly colecaoService: ColecaoService,
    private readonly dialog: MatDialog,
    private readonly notificationService: NotificationService,
    private readonly router: Router,
    private readonly translate: TranslateService
  ) {
    this.isAdministrator = this.authService.isAdministrator();
    this.configureFiltering();
  }

  get hasColecoes(): boolean {
    return this.dataSource.filteredData.length > 0;
  }

  get deleteLabel(): string {
    return this.translate.instant('colecoes.colecaoList.actions.delete');
  }

  get deletingLabel(): string {
    return this.translate.instant('colecoes.colecaoList.actions.deleting');
  }

  ngOnInit(): void {
    this.loadColecoes();

    this.filterForm.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.applyFilter();
      });
  }

  novaColecao(): void {
    void this.router.navigate(['/colecoes/nova']);
  }

  editar(colecao: Colecao): void {
    if (!this.canManageColecao(colecao)) {
      this.notificationService.showError(this.translate.instant('colecoes.colecaoList.feedback.errors.editDefaultCollection'));
      return;
    }

    void this.router.navigate(['/colecoes', colecao.id]);
  }

  abrirMapeamento(colecao: Colecao): void {
    void this.router.navigate(['/mapeamento', colecao.id]);
  }

  excluir(colecao: Colecao): void {
    if (!this.canManageColecao(colecao)) {
      this.notificationService.showError(this.translate.instant('colecoes.colecaoList.feedback.errors.deleteDefaultCollection'));
      return;
    }

    this.dialog.open(ConfirmDialogComponent, {
      width: '420px',
      data: {
        title: this.translate.instant('colecoes.colecaoList.confirmDelete.title'),
        message: `${this.translate.instant('colecoes.colecaoList.confirmDelete.messagePrefix')} "${colecao.nomeColecao}"?`,
        confirmLabel: this.translate.instant('colecoes.colecaoList.confirmDelete.confirmLabel'),
        cancelLabel: this.translate.instant('colecoes.colecaoList.confirmDelete.cancelLabel')
      }
    }).afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((confirmed: boolean | undefined) => {
        if (!confirmed) {
          return;
        }

        this.deleteColecao(colecao);
      });
  }

  canManageColecao(colecao: Colecao): boolean {
    return this.isAdministrator || !isColecaoPadrao(colecao);
  }

  getTipoColecaoLabel(colecao: Colecao): string {
    return this.getTipoColecaoOptionLabel(resolveTipoColecao(colecao.tipoColecao));
  }

  getEscopoLabel(colecao: Colecao): string {
    return isColecaoPadrao(colecao)
      ? this.translate.instant('colecoes.colecaoList.scope.default')
      : this.translate.instant('colecoes.colecaoList.scope.myCompany');
  }

  isPadrao(colecao: Colecao): boolean {
    return isColecaoPadrao(colecao);
  }

  private applyFilter(): void {
    const filter: ColecaoListFilter = {
      termo: this.normalize(this.filterForm.controls.termo.getRawValue()),
      tipoColecao: this.filterForm.controls.tipoColecao.getRawValue()
    };

    this.dataSource.filter = JSON.stringify(filter);

    if (this.dataSource.paginator) {
      this.dataSource.paginator.firstPage();
    }
  }

  private configureFiltering(): void {
    this.dataSource.filterPredicate = (colecao: Colecao, rawFilter: string): boolean => {
      let filter: ColecaoListFilter = { termo: '', tipoColecao: '' };

      try {
        filter = JSON.parse(rawFilter) as ColecaoListFilter;
      } catch {
        filter = { termo: '', tipoColecao: '' };
      }

      const matchesNome = !filter.termo || this.normalize(colecao.nomeColecao).includes(filter.termo);
      const matchesTipo = !filter.tipoColecao || resolveTipoColecao(colecao.tipoColecao) === filter.tipoColecao as TipoColecao;

      return matchesNome && matchesTipo;
    };
  }

  private deleteColecao(colecao: Colecao): void {
    this.errorMessage = '';
    this.deletingColecaoId = colecao.id;

    this.colecaoService.delete(colecao.id)
      .pipe(
        finalize(() => {
          this.deletingColecaoId = null;
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: () => {
          this.dataSource.data = this.dataSource.data.filter((item) => item.id !== colecao.id);
          this.applyFilter();
          this.notificationService.showSuccess(this.translate.instant('colecoes.colecaoList.feedback.success.deleted'));
        },
        error: (error: HttpErrorResponse) => {
          this.errorMessage = error.error?.detail ?? this.translate.instant('colecoes.colecaoList.feedback.errors.deleteCollection');
          this.notificationService.showError(this.errorMessage);
        }
      });
  }

  private loadColecoes(): void {
    this.errorMessage = '';
    this.isLoading = true;

    this.colecaoService.getAll()
      .pipe(
        finalize(() => {
          this.isLoading = false;
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (colecoes: Colecao[]) => {
          this.dataSource.data = [...colecoes].sort((left, right) => left.nomeColecao.localeCompare(right.nomeColecao));
          this.applyFilter();
        },
        error: (error: HttpErrorResponse) => {
          this.errorMessage = error.error?.detail ?? this.translate.instant('colecoes.colecaoList.feedback.errors.loadCollections');
          this.notificationService.showError(this.errorMessage);
        }
      });
  }

  private normalize(value: string | null | undefined): string {
    return (value ?? '')
      .normalize('NFD')
      .replace(/\p{Diacritic}/gu, '')
      .toLowerCase()
      .trim();
  }

  private getTipoColecaoOptionLabel(tipoColecao: TipoColecao): string {
    const key = tipoColecao === TipoColecao.Line ? 'line' : 'header';
    return this.translate.instant(`colecoes.collectionTypes.${key}`);
  }
}
