import { Component, Inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';

export interface NovoMapeamentoDialogData {
  nomeInicial?: string;
  isPadraoInicial?: boolean;
  exibirCampoPadrao?: boolean;
}

export interface NovoMapeamentoDialogResult {
  nome: string;
  isPadrao: boolean;
}

@Component({
  selector: 'app-novo-mapeamento-dialog',
  templateUrl: './novo-mapeamento-dialog.component.html',
  styleUrl: './novo-mapeamento-dialog.component.css'
})
export class NovoMapeamentoDialogComponent {
  readonly form;

  constructor(
    private readonly fb: FormBuilder,
    private readonly dialogRef: MatDialogRef<NovoMapeamentoDialogComponent, NovoMapeamentoDialogResult>,
    @Inject(MAT_DIALOG_DATA) public readonly data: NovoMapeamentoDialogData | null
  ) {
    this.form = this.fb.group({
      nome: [this.data?.nomeInicial ?? '', [Validators.required, Validators.maxLength(120)]],
      isPadrao: [this.data?.isPadraoInicial ?? false]
    });
  }

  get exibirCampoPadrao(): boolean {
    return this.data?.exibirCampoPadrao ?? false;
  }

  confirmar(): void {
    const nome = this.form.controls.nome.value?.trim();

    if (!nome) {
      this.form.controls.nome.markAsTouched();
      return;
    }

    this.dialogRef.close({
      nome,
      isPadrao: this.exibirCampoPadrao ? !!this.form.controls.isPadrao.value : false
    });
  }

  cancelar(): void {
    this.dialogRef.close();
  }
}
