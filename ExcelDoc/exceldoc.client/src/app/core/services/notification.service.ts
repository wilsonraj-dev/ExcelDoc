import { Injectable } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  constructor(private readonly snackBar: MatSnackBar) {}

  showError(message: string): void {
    this.open(message, 'error-snackbar');
  }

  showInfo(message: string): void {
    this.open(message, 'info-snackbar');
  }

  showSuccess(message: string): void {
    this.open(message, 'success-snackbar');
  }

  private open(message: string, panelClass: string): void {
    this.snackBar.open(message, 'Fechar', {
      duration: 5000,
      horizontalPosition: 'right',
      verticalPosition: 'top',
      panelClass: [panelClass]
    });
  }
}
