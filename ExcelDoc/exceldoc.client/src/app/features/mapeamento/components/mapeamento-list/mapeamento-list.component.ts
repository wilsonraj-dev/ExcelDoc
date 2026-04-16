import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService } from '../../../../core/services/auth.service';
import { NotificationService } from '../../../../core/services/notification.service';
import { Colecao } from '../../../colecoes/models/colecao.model';
import { ColecaoService } from '../../../colecoes/services/colecao.service';

@Component({
  selector: 'app-mapeamento-list',
  templateUrl: './mapeamento-list.component.html',
  styleUrl: './mapeamento-list.component.css'
})
export class MapeamentoListComponent implements OnInit {
  colecoes: Colecao[] = [];
  isLoading = false;

  private readonly destroyRef = inject(DestroyRef);

  constructor(
    private readonly router: Router,
    private readonly authService: AuthService,
    private readonly colecaoService: ColecaoService,
    private readonly notificationService: NotificationService
  ) {}

  ngOnInit(): void {
    this.loadColecoes();
  }

  loadColecoes(): void {
    this.isLoading = true;
    this.colecaoService.getAll()
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.isLoading = false)
      )
      .subscribe({
        next: (colecoes) => this.colecoes = colecoes,
        error: (err: HttpErrorResponse) => {
          this.notificationService.showError(
            err.error?.message ?? 'Erro ao carregar coleções.'
          );
        }
      });
  }

  abrirMapeamento(colecao: Colecao): void {
    void this.router.navigate(['/mapeamento', colecao.id]);
  }
}
