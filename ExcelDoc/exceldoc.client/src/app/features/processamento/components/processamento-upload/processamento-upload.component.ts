import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';
import { NotificationService } from '../../../../core/services/notification.service';
import { Documento } from '../../../documentos/models/documento.model';
import { DocumentoService } from '../../../documentos/services/documento.service';
import { AuthService } from '../../../../core/services/auth.service';
import { ProcessamentoService } from '../../services/processamento.service';

@Component({
  selector: 'app-processamento-upload',
  templateUrl: './processamento-upload.component.html',
  styleUrl: './processamento-upload.component.css'
})
export class ProcessamentoUploadComponent implements OnInit {
  form!: FormGroup;
  documentos: Documento[] = [];
  selectedFile: File | null = null;
  isLoading = false;
  isLoadingDocumentos = false;

  private readonly destroyRef = inject(DestroyRef);

  constructor(
    private readonly fb: FormBuilder,
    private readonly router: Router,
    private readonly documentoService: DocumentoService,
    private readonly processamentoService: ProcessamentoService,
    private readonly notificationService: NotificationService,
    private readonly authService: AuthService
  ) {}

  ngOnInit(): void {
    this.form = this.fb.group({
      documentoId: [null, Validators.required]
    });
    this.loadDocumentos();
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      const file = input.files[0];
      const allowedTypes = [
        'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'
      ];
      if (!allowedTypes.includes(file.type)) {
        this.notificationService.showError('Selecione um arquivo .xlsx válido.');
        this.selectedFile = null;
        return;
      }
      this.selectedFile = file;
    }
  }

  get isFormValid(): boolean {
    return this.form.valid && this.selectedFile !== null;
  }

  processar(): void {
    if (!this.isFormValid || !this.selectedFile) {
      return;
    }

    this.isLoading = true;
    const documentoId = this.form.value.documentoId as number;
    const session = this.authService.getSession();
    const empresaId = session?.empresaId;

    if (!empresaId) {
      this.notificationService.showError('Empresa não identificada na sessão.');
      this.isLoading = false;
      return;
    }

    this.processamentoService.upload(this.selectedFile, documentoId, empresaId)
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

  private loadDocumentos(): void {
    this.isLoadingDocumentos = true;
    this.documentoService.getAll()
      .pipe(
        finalize(() => { this.isLoadingDocumentos = false; }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (docs) => { this.documentos = docs; },
        error: () => {
          this.notificationService.showError('Erro ao carregar documentos.');
        }
      });
  }
}
