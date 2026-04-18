import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, OnInit, ViewChild, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatDialog } from '@angular/material/dialog';
import { MatPaginator } from '@angular/material/paginator';
import { MatTableDataSource } from '@angular/material/table';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject, switchMap, timer } from 'rxjs';
import { finalize, takeUntil } from 'rxjs/operators';
import { NotificationService } from '../../../../core/services/notification.service';
import { Processamento, ProcessamentoItem } from '../../models/processamento.model';
import { ProcessamentoService } from '../../services/processamento.service';
import { JsonDialogComponent, JsonDialogData } from '../json-dialog/json-dialog.component';

@Component({
  selector: 'app-processamento-detalhe',
  templateUrl: './processamento-detalhe.component.html',
  styleUrl: './processamento-detalhe.component.css'
})
export class ProcessamentoDetalheComponent implements OnInit {
  processamento: Processamento | null = null;
  readonly itensColumns: string[] = ['linhaExcel', 'status', 'mensagemErro', 'acoes'];
  readonly itensDataSource = new MatTableDataSource<ProcessamentoItem>([]);
  readonly pageSizeOptions = [10, 25, 50];

  isLoading = false;
  isLoadingItens = false;

  private processamentoId!: number;
  private readonly destroyRef = inject(DestroyRef);
  private readonly stopPolling$ = new Subject<void>();

  @ViewChild(MatPaginator)
  set paginator(paginator: MatPaginator | undefined) {
    if (paginator) {
      this.itensDataSource.paginator = paginator;
      this.itensDataSource.paginator.pageSize = this.pageSizeOptions[0];
    }
  }

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly dialog: MatDialog,
    private readonly processamentoService: ProcessamentoService,
    private readonly notificationService: NotificationService
  ) {}

  ngOnInit(): void {
    this.processamentoId = Number(this.route.snapshot.paramMap.get('id'));
    this.loadProcessamento();
    this.loadItens();
    this.startPolling();
  }

  get isProcessando(): boolean {
    return this.processamento?.status === 'Processando';
  }

  get progressoPercentual(): number {
    if (!this.processamento || this.processamento.totalRegistros === 0) {
      return 0;
    }
    const processed = this.processamento.totalSucesso + this.processamento.totalErro;
    return Math.round((processed / this.processamento.totalRegistros) * 100);
  }

  voltar(): void {
    void this.router.navigate(['/processamento']);
  }

  reprocessar(): void {
    void this.router.navigate(['/processamento/upload']);
  }

  verJson(item: ProcessamentoItem): void {
    this.dialog.open(JsonDialogComponent, {
      width: '700px',
      data: {
        linhaExcel: item.linhaExcel,
        jsonEnviado: item.jsonEnviado,
        jsonRetorno: item.jsonRetorno
      } as JsonDialogData
    });
  }

  baixarErros(): void {
    const erros = this.itensDataSource.data.filter((i) => i.status === 'Erro');
    if (erros.length === 0) {
      this.notificationService.showInfo('Nenhum registro com erro.');
      return;
    }

    const blob = new Blob([JSON.stringify(erros, null, 2)], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `erros-processamento-${this.processamentoId}.json`;
    a.click();
    URL.revokeObjectURL(url);
  }

  getItemStatusClass(status: string): string {
    return status === 'Erro' ? 'status-erro' : 'status-sucesso';
  }

  private loadProcessamento(): void {
    this.isLoading = true;
    this.processamentoService.getById(this.processamentoId)
      .pipe(
        finalize(() => { this.isLoading = false; }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (data) => { this.processamento = data; },
        error: (error: HttpErrorResponse) => {
          const msg = error.error?.detail ?? 'Erro ao carregar processamento.';
          this.notificationService.showError(msg);
        }
      });
  }

  private loadItens(): void {
    this.isLoadingItens = true;
    this.processamentoService.getItens(this.processamentoId)
      .pipe(
        finalize(() => { this.isLoadingItens = false; }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (data) => { this.itensDataSource.data = data.items; },
        error: (error: HttpErrorResponse) => {
          const msg = error.error?.detail ?? 'Erro ao carregar itens.';
          this.notificationService.showError(msg);
        }
      });
  }

  private startPolling(): void {
    timer(5000, 5000).pipe(
      takeUntil(this.stopPolling$),
      takeUntilDestroyed(this.destroyRef),
      switchMap(() => this.processamentoService.getById(this.processamentoId))
    ).subscribe({
      next: (data) => {
        this.processamento = data;
        if (data.status !== 'Processando') {
          this.stopPolling$.next();
          this.loadItens();
        }
      }
    });
  }
}
