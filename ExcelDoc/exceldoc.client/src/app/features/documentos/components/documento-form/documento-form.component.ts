import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AbstractControl, FormControl, FormGroup, ValidationErrors, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { finalize } from 'rxjs';
import { NotificationService } from '../../../../core/services/notification.service';
import { DocumentoPayload } from '../../models/documento.model';
import { DocumentoService } from '../../services/documento.service';

function endpointValidator(control: AbstractControl<string | null>): ValidationErrors | null {
  const value = control.value?.trim() ?? '';

  if (!value) {
    return null;
  }

  return value.startsWith('/') ? null : { endpointPrefix: true };
}

@Component({
  selector: 'app-documento-form',
  templateUrl: './documento-form.component.html',
  styleUrl: './documento-form.component.css'
})
export class DocumentoFormComponent implements OnInit {
  readonly form = new FormGroup({
    nomeDocumento: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    endpoint: new FormControl('', { nonNullable: true, validators: [Validators.required, endpointValidator] })
  });

  apiError = '';
  documentoId: number | null = null;
  isLoading = false;
  isSaving = false;

  private readonly destroyRef = inject(DestroyRef);

  constructor(
    private readonly activatedRoute: ActivatedRoute,
    private readonly documentoService: DocumentoService,
    private readonly notificationService: NotificationService,
    private readonly router: Router
  ) { }

  get isEditMode(): boolean {
    return this.documentoId !== null;
  }

  get pageTitle(): string {
    return this.isEditMode ? 'Editar documento' : 'Novo documento';
  }

  get submitLabel(): string {
    if (this.isSaving) {
      return 'Salvando...';
    }

    return this.isEditMode ? 'Salvar alterações' : 'Salvar documento';
  }

  ngOnInit(): void {
    const rawId = this.activatedRoute.snapshot.paramMap.get('id');

    if (!rawId) {
      return;
    }

    const documentoId = Number(rawId);
    if (!Number.isInteger(documentoId) || documentoId <= 0) {
      this.notificationService.showError('Documento inválido para edição.');
      void this.router.navigate(['/documentos']);
      return;
    }

    this.documentoId = documentoId;
    this.loadDocumento(documentoId);
  }

  isInvalid(controlName: 'nomeDocumento' | 'endpoint'): boolean {
    const control = this.form.controls[controlName];
    return control.invalid && (control.dirty || control.touched);
  }

  getErrorMessage(controlName: 'nomeDocumento' | 'endpoint'): string {
    const control = this.form.controls[controlName];

    if (control.hasError('required')) {
      return controlName === 'nomeDocumento'
        ? 'Informe o nome do documento.'
        : 'Informe o endpoint do documento.';
    }

    if (control.hasError('endpointPrefix')) {
      return 'O endpoint deve começar com "/".';
    }

    return 'Campo inválido.';
  }

  cancel(): void {
    void this.router.navigate(['/documentos']);
  }

  submit(): void {
    this.apiError = '';

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const payload: DocumentoPayload = {
      nomeDocumento: this.form.controls.nomeDocumento.getRawValue().trim(),
      endpoint: this.form.controls.endpoint.getRawValue().trim()
    };

    const request$ = this.isEditMode && this.documentoId !== null
      ? this.documentoService.update(this.documentoId, payload)
      : this.documentoService.create(payload);

    this.isSaving = true;
    request$
      .pipe(
        finalize(() => {
          this.isSaving = false;
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: () => {
          this.notificationService.showSuccess(
            this.isEditMode ? 'Documento atualizado com sucesso.' : 'Documento criado com sucesso.'
          );
          void this.router.navigate(['/documentos']);
        },
        error: (error: HttpErrorResponse) => {
          this.apiError = error.error?.detail ?? 'Não foi possível salvar o documento.';
          this.notificationService.showError(this.apiError);
        }
      });
  }

  private loadDocumento(documentoId: number): void {
    this.apiError = '';
    this.isLoading = true;

    this.documentoService.getById(documentoId)
      .pipe(
        finalize(() => {
          this.isLoading = false;
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (documento) => {
          this.form.reset({
            nomeDocumento: documento.nomeDocumento,
            endpoint: documento.endpoint
          });
        },
        error: (error: HttpErrorResponse) => {
          this.apiError = error.error?.detail ?? 'Não foi possível carregar o documento.';
          this.notificationService.showError(this.apiError);
        }
      });
  }
}
