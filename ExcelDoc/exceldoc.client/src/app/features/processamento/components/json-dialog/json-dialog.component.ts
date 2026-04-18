import { Component, Inject } from '@angular/core';
import { MAT_DIALOG_DATA } from '@angular/material/dialog';

export interface JsonDialogData {
  linhaExcel: number;
  jsonEnviado: string;
  jsonRetorno: string;
}

@Component({
  selector: 'app-json-dialog',
  templateUrl: './json-dialog.component.html',
  styleUrl: './json-dialog.component.css'
})
export class JsonDialogComponent {
  formattedEnviado = '';
  formattedRetorno = '';

  constructor(@Inject(MAT_DIALOG_DATA) public data: JsonDialogData) {
    this.formattedEnviado = this.formatJson(data.jsonEnviado);
    this.formattedRetorno = this.formatJson(data.jsonRetorno);
  }

  private formatJson(raw: string): string {
    try {
      return JSON.stringify(JSON.parse(raw), null, 2);
    } catch {
      return raw || '—';
    }
  }
}
