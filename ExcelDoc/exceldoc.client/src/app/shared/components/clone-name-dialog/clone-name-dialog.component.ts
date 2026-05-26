import { Component, Inject } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';

export interface CloneNameDialogData {
  title: string;
  label: string;
  confirmLabel?: string;
  cancelLabel?: string;
  initialValue?: string;
  maxLength?: number;
}

export interface CloneNameDialogResult {
  nome: string;
}

@Component({
  selector: 'app-clone-name-dialog',
  templateUrl: './clone-name-dialog.component.html',
  styleUrl: './clone-name-dialog.component.css'
})
export class CloneNameDialogComponent {
  readonly form;

  constructor(
    private readonly fb: FormBuilder,
    private readonly dialogRef: MatDialogRef<CloneNameDialogComponent, CloneNameDialogResult>,
    @Inject(MAT_DIALOG_DATA) public readonly data: CloneNameDialogData
  ) {
    this.form = this.fb.group({
      nome: [data.initialValue ?? '', [Validators.required, Validators.maxLength(data.maxLength ?? 150)]]
    });
  }

  confirmar(): void {
    const nome = this.form.controls.nome.value?.trim();

    if (!nome) {
      this.form.controls.nome.markAsTouched();
      return;
    }

    this.dialogRef.close({ nome });
  }

  cancelar(): void {
    this.dialogRef.close();
  }
}
