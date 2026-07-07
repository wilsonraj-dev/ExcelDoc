import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, OnInit, ViewChild, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatPaginator } from '@angular/material/paginator';
import { MatTableDataSource } from '@angular/material/table';
import { Router } from '@angular/router';
import { Subject, switchMap, timer } from 'rxjs';
import { finalize, takeUntil } from 'rxjs/operators';
import { NotificationService } from '../../../../core/services/notification.service';
import { TranslateService } from '../../../../core/services/translate.service';
import { Processamento } from '../../models/processamento.model';
import { ProcessamentoService } from '../../services/processamento.service';

@Component({
  selector: 'app-processamento-list',
  templateUrl: './processamento-list.component.html',
  styleUrl: './processamento-list.component.css'
})
export class ProcessamentoListComponent implements OnInit {
  readonly displayedColumns: string[] = [
    'nomeArquivo', 'dataExecucao', 'status',
    'totalRegistros', 'totalSucesso', 'totalErro', 'totalIgnorado', 'acoes'
  ];
  readonly pageSizeOptions = [5, 10, 20];
  readonly dataSource = new MatTableDataSource<Processamento>([]);

  isLoading = false;
  statusFilter = '';

  private readonly destroyRef = inject(DestroyRef);
  private readonly stopPolling$ = new Subject<void>();

  @ViewChild(MatPaginator)
  set paginator(paginator: MatPaginator | undefined) {
    if (paginator) {
      this.dataSource.paginator = paginator;
      this.dataSource.paginator.pageSize = this.pageSizeOptions[0];
    }
  }

  constructor(
    private readonly router: Router,
    private readonly processamentoService: ProcessamentoService,
    private readonly notificationService: NotificationService,
    private readonly translate: TranslateService
  ) {}

  ngOnInit(): void {
    this.loadProcessamentos();
    this.startPolling();
  }

  get hasData(): boolean {
    return this.dataSource.data.length > 0;
  }

  novoProcessamento(): void {
    void this.router.navigate(['/processamento/upload']);
  }

  verDetalhes(item: Processamento): void {
    void this.router.navigate(['/processamento', item.id]);
  }

  applyStatusFilter(status: string): void {
    this.statusFilter = status;
    this.dataSource.filterPredicate = (data: Processamento) => {
      if (!this.statusFilter) {
        return true;
      }
      return data.status === this.statusFilter;
    };
    this.dataSource.filter = status || ' ';
  }

  getStatusClass(status: string): string {
    switch (status) {
      case 'Sucesso': return 'status-sucesso';
      case 'Erro': return 'status-erro';
      default: return 'status-processando';
    }
  }

  getStatusLabel(status: string): string {
    const key = status === 'Erro' ? 'error' : status === 'Sucesso' ? 'success' : 'processing';
    return this.translate.instant(`processamento.common.status.${key}`);
  }

  private loadProcessamentos(): void {
    this.isLoading = true;
    this.processamentoService.getAll()
      .pipe(
        finalize(() => { this.isLoading = false; }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (data) => { this.dataSource.data = data.items; },
        error: (error: HttpErrorResponse) => {
          const msg = error.error?.detail ?? this.translate.instant('processamento.processamentoList.feedback.errors.loadProcessings');
          this.notificationService.showError(msg);
        }
      });
  }

  private startPolling(): void {
    timer(5000, 5000).pipe(
      takeUntil(this.stopPolling$),
      takeUntilDestroyed(this.destroyRef),
      switchMap(() => this.processamentoService.getAll())
    ).subscribe({
      next: (data) => {
        this.dataSource.data = data.items;
        const hasProcessing = data.items.some((p) => p.status === 'Processando');
        if (!hasProcessing) {
          this.stopPolling$.next();
        }
      }
    });
  }
}
