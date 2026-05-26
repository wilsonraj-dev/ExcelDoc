import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, OnInit, ViewChild, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatDialog } from '@angular/material/dialog';
import { MatPaginator } from '@angular/material/paginator';
import { MatTableDataSource } from '@angular/material/table';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService } from '../../../../core/services/auth.service';
import { NotificationService } from '../../../../core/services/notification.service';
import { ConfirmDialogComponent } from '../../../../shared/components/confirm-dialog/confirm-dialog.component';
import {
  CloneNameDialogComponent,
  CloneNameDialogResult
} from '../../../../shared/components/clone-name-dialog/clone-name-dialog.component';
import { Documento } from '../../../documentos/models/documento.model';
import { DocumentoService } from '../../../documentos/services/documento.service';
import { PerfilMapeamento } from '../../models/perfil-mapeamento.model';
import { PerfilMapeamentoService } from '../../services/perfil-mapeamento.service';

@Component({
  selector: 'app-perfil-list',
  templateUrl: './perfil-list.component.html',
  styleUrl: './perfil-list.component.css'
})
export class PerfilListComponent implements OnInit {
  readonly displayedColumns: string[] = ['nome', 'documento', 'tipo', 'itens', 'acoes'];
  readonly pageSizeOptions = [5, 10, 20];
  readonly dataSource = new MatTableDataSource<PerfilMapeamento>([]);

  documentos: Documento[] = [];
  isLoading = false;
  isLoadingDocumentos = false;
  deletingId: number | null = null;
  selectedDocumentoId: number | null = null;
  readonly isAdministrator: boolean;
  readonly empresaId: number | null;

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
    private readonly perfilService: PerfilMapeamentoService,
    private readonly documentoService: DocumentoService,
    private readonly dialog: MatDialog,
    private readonly notificationService: NotificationService,
    private readonly router: Router
  ) {
    this.isAdministrator = this.authService.isAdministrator();
    this.empresaId = this.authService.getSession()?.empresaId ?? null;
  }

  get hasPerfis(): boolean {
    return this.dataSource.filteredData.length > 0;
  }

  ngOnInit(): void {
    this.loadDocumentos();
  }

  onDocumentoChange(documentoId: number): void {
    this.selectedDocumentoId = documentoId;
    this.loadPerfis(documentoId);
  }

  novoPerfil(): void {
    void this.router.navigate(['/perfil-mapeamento/novo']);
  }

  editar(perfil: PerfilMapeamento): void {
    void this.router.navigate(['/perfil-mapeamento', perfil.id]);
  }

  clonar(perfil: PerfilMapeamento): void {
    const dialogRef = this.dialog.open(CloneNameDialogComponent, {
      width: '360px',
      data: {
        title: 'Clonar perfil',
        label: 'Nome do novo perfil',
        initialValue: `${perfil.nome}`,
        confirmLabel: 'Clonar'
      }
    });

    dialogRef.afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result?: CloneNameDialogResult) => {
        const nome = result?.nome?.trim();

        if (!nome) {
          return;
        }

        this.perfilService.clone(perfil.id, nome)
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe({
            next: (clone) => {
              this.notificationService.showSuccess('Perfil clonado com sucesso.');
              void this.router.navigate(['/perfil-mapeamento', clone.id]);
            },
            error: (err: HttpErrorResponse) => {
              this.notificationService.showError(err.error?.detail ?? 'Erro ao clonar perfil.');
            }
          });
      });
  }

  excluir(perfil: PerfilMapeamento): void {
    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      data: {
        title: 'Excluir perfil',
        message: `Deseja realmente excluir o perfil "${perfil.nome}"?`
      }
    });

    dialogRef.afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((confirmed) => {
        if (!confirmed) return;

        this.deletingId = perfil.id;
        this.perfilService.delete(perfil.id)
          .pipe(
            finalize(() => { this.deletingId = null; }),
            takeUntilDestroyed(this.destroyRef)
          )
          .subscribe({
            next: () => {
              this.notificationService.showSuccess('Perfil excluído com sucesso.');
              if (this.selectedDocumentoId) {
                this.loadPerfis(this.selectedDocumentoId);
              }
            },
            error: (err: HttpErrorResponse) => {
              this.notificationService.showError(err.error?.detail ?? 'Erro ao excluir perfil.');
            }
          });
      });
  }

  canEdit(perfil: PerfilMapeamento): boolean {
    if (this.isAdministrator) return true;
    if (perfil.isPadrao) return false;
    return perfil.fk_IdEmpresa === this.empresaId;
  }

  canDelete(perfil: PerfilMapeamento): boolean {
    if (perfil.isPadrao && !this.isAdministrator) return false;
    return this.canEdit(perfil);
  }

  getTipoLabel(perfil: PerfilMapeamento): string {
    return perfil.isPadrao ? 'Padrão' : 'Minha Empresa';
  }

  getTipoBadgeClass(perfil: PerfilMapeamento): string {
    return perfil.isPadrao ? 'perfil-badge perfil-badge--padrao' : 'perfil-badge perfil-badge--empresa';
  }

  getDocumentoNome(perfil: PerfilMapeamento): string {
    return this.documentos.find(d => d.id === perfil.fk_IdDocumento)?.nomeDocumento ?? '—';
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
          if (docs.length > 0) {
            this.selectedDocumentoId = docs[0].id;
            this.loadPerfis(docs[0].id);
          }
        },
        error: () => {
          this.notificationService.showError('Erro ao carregar documentos.');
        }
      });
  }

  private loadPerfis(documentoId: number): void {
    this.isLoading = true;
    this.perfilService.getByDocumento(documentoId)
      .pipe(
        finalize(() => { this.isLoading = false; }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (perfis) => {
          this.dataSource.data = perfis;
        },
        error: () => {
          this.notificationService.showError('Erro ao carregar perfis de mapeamento.');
        }
      });
  }
}
