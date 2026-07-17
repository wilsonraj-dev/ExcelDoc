import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService } from '../../../../core/services/auth.service';
import { NotificationService } from '../../../../core/services/notification.service';
import { TranslateService } from '../../../../core/services/translate.service';
import { Documento } from '../../../documentos/models/documento.model';
import { DocumentoService } from '../../../documentos/services/documento.service';
import { PerfilMapeamento } from '../../../perfil-mapeamento/models/perfil-mapeamento.model';
import { PerfilMapeamentoService } from '../../../perfil-mapeamento/services/perfil-mapeamento.service';
import { ProcessamentoService } from '../../services/processamento.service';

@Component({
  selector: 'app-processamento-upload',
  templateUrl: './processamento-upload.component.html',
  styleUrl: './processamento-upload.component.css'
})
export class ProcessamentoUploadComponent implements OnInit {
  form!: FormGroup;
  documentos: Documento[] = [];
  perfisDisponiveis: PerfilMapeamento[] = [];
  selectedFile: File | null = null;
  isLoading = false;
  isLoadingDocumentos = false;
  isLoadingPerfis = false;

  private readonly destroyRef = inject(DestroyRef);
  private perfisRequestVersion = 0;

  constructor(
    private readonly fb: FormBuilder,
    private readonly router: Router,
    private readonly documentoService: DocumentoService,
    private readonly perfilService: PerfilMapeamentoService,
    private readonly processamentoService: ProcessamentoService,
    private readonly notificationService: NotificationService,
    private readonly authService: AuthService,
    private readonly translate: TranslateService
  ) {}

  ngOnInit(): void {
    this.form = this.fb.group({
      documentoId: [null, Validators.required],
      perfilMapeamentoId: [null, Validators.required]
    });

    this.form.controls['documentoId'].valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((documentoId) => {
        this.form.controls['perfilMapeamentoId'].setValue(null, { emitEvent: false });
        this.perfisDisponiveis = [];

        if (documentoId) {
          this.loadPerfis(documentoId as number);
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

  get selectedPerfilNome(): string {
    const perfilMapeamentoId = this.form?.controls['perfilMapeamentoId']?.value as number | null | undefined;

    if (!perfilMapeamentoId) {
      return '';
    }

    return this.perfisDisponiveis.find((perfil) => perfil.id === perfilMapeamentoId)?.nome ?? '';
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      const file = input.files[0];
      const allowedTypes = ['application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'];
      if (!allowedTypes.includes(file.type)) {
        this.notificationService.showError(this.translate.instant('processamento.processamentoUpload.feedback.errors.invalidFile'));
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
    const perfilMapeamentoId = this.form.value.perfilMapeamentoId as number;
    const empresaId = this.empresaId;

    if (!empresaId) {
      this.notificationService.showError(this.translate.instant('processamento.processamentoUpload.feedback.errors.companyNotFound'));
      return;
    }

    this.isLoading = true;
    this.processamentoService.upload(this.selectedFile, documentoId, perfilMapeamentoId, empresaId)
      .pipe(
        finalize(() => { this.isLoading = false; }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (result) => {
          this.notificationService.showSuccess(this.translate.instant('processamento.processamentoUpload.feedback.success.uploaded'));
          void this.router.navigate(['/processamento', result.id]);
        },
        error: (error: HttpErrorResponse) => {
          const message = error.error?.detail ?? this.translate.instant('processamento.processamentoUpload.feedback.errors.uploadFile');
          this.notificationService.showError(message);
        }
      });
  }

  getPerfilBadgeClass(perfil: PerfilMapeamento): string {
    return perfil.isPadrao ? 'mapping-chip mapping-chip--padrao' : 'mapping-chip mapping-chip--empresa';
  }

  getPerfilBadgeLabel(perfil: PerfilMapeamento): string {
    return perfil.isPadrao
      ? this.translate.instant('processamento.common.scope.default')
      : this.translate.instant('processamento.common.scope.company');
  }

  getPerfilTooltip(perfil: PerfilMapeamento): string {
    return perfil.isPadrao
      ? this.translate.instant('processamento.processamentoUpload.tooltips.defaultProfile')
      : this.translate.instant('processamento.processamentoUpload.tooltips.companyProfile');
  }

  getItensQuantidadeLabel(perfil: PerfilMapeamento): string {
    const quantidade = perfil.itens?.length ?? 0;
    return quantidade === 1
      ? this.translate.instant('processamento.processamentoUpload.summary.oneCollection')
      : `${quantidade} ${this.translate.instant('processamento.processamentoUpload.summary.collectionsSuffix')}`;
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
        },
        error: () => {
          this.notificationService.showError(this.translate.instant('processamento.processamentoUpload.feedback.errors.loadDocuments'));
        }
      });
  }

  private loadPerfis(documentoId: number): void {
    const requestVersion = ++this.perfisRequestVersion;
    this.isLoadingPerfis = true;
    this.perfilService.getByDocumento(documentoId)
      .pipe(
        finalize(() => {
          if (requestVersion === this.perfisRequestVersion) {
            this.isLoadingPerfis = false;
          }
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (perfis) => {
          if (requestVersion !== this.perfisRequestVersion || this.form.controls['documentoId'].value !== documentoId) {
            return;
          }

          this.perfisDisponiveis = [...perfis].sort((left, right) => {
            if (left.isPadrao !== right.isPadrao) {
              return left.isPadrao ? -1 : 1;
            }

            return left.nome.localeCompare(right.nome, 'pt-BR', { sensitivity: 'base' });
          });

          const perfisPadrao = this.perfisDisponiveis.filter((perfil) => perfil.isPadrao);
          const perfilPadrao = perfisPadrao.length === 1 ? perfisPadrao[0] : null;

          this.form.controls['perfilMapeamentoId'].setValue(perfilPadrao?.id ?? null, { emitEvent: false });

          if (!perfis.length) {
            this.notificationService.showInfo(this.translate.instant('processamento.processamentoUpload.feedback.info.noProfiles'));
          }
        },
        error: () => {
          if (requestVersion !== this.perfisRequestVersion || this.form.controls['documentoId'].value !== documentoId) {
            return;
          }

          this.notificationService.showError(this.translate.instant('processamento.processamentoUpload.feedback.errors.loadProfiles'));
        }
      });
  }
}
