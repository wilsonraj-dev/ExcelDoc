import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService } from '../../../../core/services/auth.service';
import { NotificationService } from '../../../../core/services/notification.service';
import { Colecao, getColecaoDocumentoIds } from '../../../colecoes/models/colecao.model';
import { ColecaoService } from '../../../colecoes/services/colecao.service';
import { Documento } from '../../../documentos/models/documento.model';
import { DocumentoService } from '../../../documentos/services/documento.service';
import { Mapeamento, getMapeamentoEmpresaId, orderMapeamentos } from '../../../mapeamento/models/mapeamento.model';
import { MapeamentoService } from '../../../mapeamento/services/mapeamento.service';
import { ProcessamentoService } from '../../services/processamento.service';

@Component({
  selector: 'app-processamento-upload',
  templateUrl: './processamento-upload.component.html',
  styleUrl: './processamento-upload.component.css'
})
export class ProcessamentoUploadComponent implements OnInit {
  form!: FormGroup;
  documentos: Documento[] = [];
  colecoesDisponiveis: Colecao[] = [];
  mapeamentosDisponiveis: Mapeamento[] = [];
  selectedFile: File | null = null;
  isLoading = false;
  isLoadingDocumentos = false;
  isLoadingMapeamentos = false;

  private readonly destroyRef = inject(DestroyRef);

  constructor(
    private readonly fb: FormBuilder,
    private readonly router: Router,
    private readonly documentoService: DocumentoService,
    private readonly colecaoService: ColecaoService,
    private readonly mapeamentoService: MapeamentoService,
    private readonly processamentoService: ProcessamentoService,
    private readonly notificationService: NotificationService,
    private readonly authService: AuthService
  ) {}

  ngOnInit(): void {
    this.form = this.fb.group({
      documentoId: [null, Validators.required],
      mapeamentoId: [null, Validators.required]
    });

    this.form.controls['documentoId'].valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((documentoId) => {
        this.form.controls['mapeamentoId'].setValue(null, { emitEvent: false });
        this.mapeamentosDisponiveis = [];

        if (documentoId) {
          this.loadMapeamentosByDocumento(documentoId as number);
        }
      });

    this.loadDocumentos();
  }

  get isAdministrator(): boolean {
    return this.authService.isAdministrator();
  }

  get empresaId(): number | null {
    return this.authService.getSession()?.empresaId ?? null;
  }

  get isFormValid(): boolean {
    return this.form.valid && this.selectedFile !== null;
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      const file = input.files[0];
      const allowedTypes = ['application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'];
      if (!allowedTypes.includes(file.type)) {
        this.notificationService.showError('Selecione um arquivo .xlsx válido.');
        this.selectedFile = null;
        return;
      }
      this.selectedFile = file;
    }
  }

  processar(): void {
    if (!this.isFormValid || !this.selectedFile) {
      this.form.markAllAsTouched();
      return;
    }

    const documentoId = this.form.value.documentoId as number;
    const mapeamentoId = this.form.value.mapeamentoId as number;
    const empresaId = this.empresaId;

    if (!empresaId) {
      this.notificationService.showError('Empresa não identificada na sessão.');
      return;
    }

    this.isLoading = true;
    this.processamentoService.upload(this.selectedFile, documentoId, mapeamentoId, empresaId)
      .pipe(
        finalize(() => { this.isLoading = false; }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (result) => {
          this.notificationService.showSuccess('Arquivo enviado para processamento.');
          void this.router.navigate(['/processamento', result.id]);
        },
        error: (error: HttpErrorResponse) => {
          const message = error.error?.detail ?? 'Erro ao enviar arquivo para processamento.';
          this.notificationService.showError(message);
        }
      });
  }

  getMapeamentoBadgeClass(mapeamento: Mapeamento): string {
    return mapeamento.isPadrao ? 'mapping-chip mapping-chip--padrao' : 'mapping-chip mapping-chip--empresa';
  }

  getMapeamentoBadgeLabel(mapeamento: Mapeamento): string {
    return mapeamento.isPadrao ? 'Padrão' : 'Empresa';
  }

  getMapeamentoTooltip(mapeamento: Mapeamento): string {
    return mapeamento.isPadrao
      ? 'Mapeamento padrão disponível para todos os usuários com acesso à coleção.'
      : 'Mapeamento customizado para a empresa.';
  }

  getCampoQuantidadeLabel(mapeamento: Mapeamento): string {
    const quantidade = mapeamento.quantidadeCampos ?? 0;
    return quantidade === 1 ? '1 campo' : `${quantidade} campos`;
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
          this.loadColecoes();
        },
        error: () => {
          this.notificationService.showError('Erro ao carregar documentos.');
        }
      });
  }

  private loadColecoes(): void {
    this.colecaoService.getAll()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (colecoes) => {
          this.colecoesDisponiveis = colecoes;
        },
        error: () => {
          this.notificationService.showError('Erro ao carregar coleções dos documentos.');
        }
      });
  }

  private loadMapeamentosByDocumento(documentoId: number): void {
    const colecao = this.resolveColecaoByDocumento(documentoId);

    if (!colecao) {
      this.notificationService.showError('Não foi possível identificar a coleção vinculada ao documento selecionado.');
      return;
    }

    this.isLoadingMapeamentos = true;
    this.mapeamentoService.getMapeamentosByColecao(colecao.id)
      .pipe(
        finalize(() => { this.isLoadingMapeamentos = false; }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (mapeamentos) => {
          this.mapeamentosDisponiveis = orderMapeamentos(mapeamentos.filter((item) => this.isMapeamentoVisible(item)));
          if (!this.mapeamentosDisponiveis.length) {
            this.notificationService.showInfo('Nenhum mapeamento disponível para o documento selecionado.');
          }
        },
        error: () => {
          this.notificationService.showError('Falha ao carregar mapeamentos para o documento selecionado.');
        }
      });
  }

  private resolveColecaoByDocumento(documentoId: number): Colecao | null {
    return this.colecoesDisponiveis.find((colecao) => getColecaoDocumentoIds(colecao).includes(documentoId)) ?? null;
  }

  private isMapeamentoVisible(mapeamento: Mapeamento): boolean {
    if (this.isAdministrator) {
      return true;
    }

    if (mapeamento.isPadrao) {
      return true;
    }

    return getMapeamentoEmpresaId(mapeamento) === this.empresaId;
  }
}
