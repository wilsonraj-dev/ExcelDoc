import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, OnInit, ViewChild, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatDialog } from '@angular/material/dialog';
import { MatPaginator } from '@angular/material/paginator';
import { MatTableDataSource } from '@angular/material/table';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';
import { NotificationService } from '../../../../core/services/notification.service';
import { ConfirmDialogComponent } from '../../../../shared/components/confirm-dialog/confirm-dialog.component';
import { Documento } from '../../models/documento.model';
import { DocumentoService } from '../../services/documento.service';

@Component({
  selector: 'app-documento-list',
  templateUrl: './documento-list.component.html',
  styleUrl: './documento-list.component.css'
})
export class DocumentoListComponent implements OnInit {
  readonly displayedColumns: string[] = ['nomeDocumento', 'endpoint', 'acoes'];
  readonly pageSizeOptions = [5, 10, 20];
  readonly dataSource = new MatTableDataSource<Documento>([]);

  errorMessage = '';
  isLoading = false;
  deletingDocumentoId: number | null = null;

  private readonly destroyRef = inject(DestroyRef);

  @ViewChild(MatPaginator)
  set paginator(paginator: MatPaginator | undefined) {
    if (paginator) {
      this.dataSource.paginator = paginator;
      this.dataSource.paginator.pageSize = this.pageSizeOptions[0];
    }
  }

  constructor(
    private readonly dialog: MatDialog,
    private readonly documentoService: DocumentoService,
    private readonly notificationService: NotificationService,
    private readonly router: Router
  ) { }

  get hasDocumentos(): boolean {
    return this.dataSource.data.length > 0;
  }

  ngOnInit(): void {
    this.loadDocumentos();
  }

  novoDocumento(): void {
    void this.router.navigate(['/documentos/novo']);
  }

  editar(documento: Documento): void {
    void this.router.navigate(['/documentos', documento.id]);
  }

  excluir(documento: Documento): void {
    this.dialog.open(ConfirmDialogComponent, {
      width: '420px',
      data: {
        title: 'Excluir documento',
        message: `Deseja realmente excluir o documento "${documento.nomeDocumento}"?`,
        confirmLabel: 'Excluir',
        cancelLabel: 'Cancelar'
      }
    }).afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((confirmed: boolean | undefined) => {
        if (!confirmed) {
          return;
        }

        this.deleteDocumento(documento);
      });
  }

  private deleteDocumento(documento: Documento): void {
    this.errorMessage = '';
    this.deletingDocumentoId = documento.id;

    this.documentoService.delete(documento.id)
      .pipe(
        finalize(() => {
          this.deletingDocumentoId = null;
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: () => {
          this.dataSource.data = this.dataSource.data.filter((item) => item.id !== documento.id);
          this.notificationService.showSuccess('Documento excluído com sucesso.');
        },
        error: (error: HttpErrorResponse) => {
          this.errorMessage = error.error?.detail ?? 'Não foi possível excluir o documento.';
          this.notificationService.showError(this.errorMessage);
        }
      });
  }

  private loadDocumentos(): void {
    this.errorMessage = '';
    this.isLoading = true;

    this.documentoService.getAll()
      .pipe(
        finalize(() => {
          this.isLoading = false;
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (documentos: Documento[]) => {
          this.dataSource.data = [...documentos].sort((left, right) => left.nomeDocumento.localeCompare(right.nomeDocumento));
        },
        error: (error: HttpErrorResponse) => {
          this.errorMessage = error.error?.detail ?? 'Não foi possível carregar os documentos.';
          this.notificationService.showError(this.errorMessage);
        }
      });
  }
}
