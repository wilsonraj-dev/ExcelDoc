import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnDestroy } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { AuthService, MessageResponse } from '../auth.service';

@Component({
  selector: 'app-forgot-password',
  templateUrl: './forgot-password.component.html',
  styleUrl: './forgot-password.component.css'
})
export class ForgotPasswordComponent implements OnDestroy {
  codigo = '';
  codeExpirationLabel = '';
  codeRequested = false;
  confirmacaoNovaSenha = '';
  email = '';
  errorMessage = '';
  isRequestingCode = false;
  isResettingPassword = false;
  novaSenha = '';
  successMessage = '';
  private countdownIntervalId: number | null = null;
  private expirationTimestamp: number | null = null;

  constructor(
    private readonly authService: AuthService,
    private readonly route: ActivatedRoute
  ) {
    this.email = this.route.snapshot.queryParamMap.get('email')?.trim() ?? '';
  }

  solicitarCodigo(): void {
    this.errorMessage = '';
    this.successMessage = '';

    if (!this.email.trim()) {
      this.errorMessage = 'Informe o e-mail cadastrado.';
      return;
    }

    if (!this.isValidEmail(this.email)) {
      this.errorMessage = 'Informe um e-mail válido.';
      return;
    }

    this.isRequestingCode = true;
    this.authService.forgotPassword({ email: this.email.trim() }).subscribe({
      next: (response: MessageResponse) => {
        this.codeRequested = true;
        this.startExpirationCountdown();
        this.successMessage = response.message;
        this.isRequestingCode = false;
      },
      error: (error: HttpErrorResponse) => {
        this.errorMessage = error.error?.detail ?? 'Não foi possível solicitar o código.';
        this.isRequestingCode = false;
      }
    });
  }

  redefinirSenha(): void {
    this.errorMessage = '';
    this.successMessage = '';

    if (!this.email.trim() || !this.codigo.trim() || !this.novaSenha.trim()) {
      this.errorMessage = 'Informe e-mail, código e nova senha.';
      return;
    }

    if (!this.isValidEmail(this.email)) {
      this.errorMessage = 'Informe um e-mail válido.';
      return;
    }

    if (this.novaSenha.length < 6) {
      this.errorMessage = 'A nova senha deve ter pelo menos 6 caracteres.';
      return;
    }

    if (this.novaSenha !== this.confirmacaoNovaSenha) {
      this.errorMessage = 'A confirmação da nova senha não confere.';
      return;
    }

    this.isResettingPassword = true;
    this.authService.resetPassword({
      email: this.email.trim(),
      codigo: this.codigo.trim(),
      novaSenha: this.novaSenha
    }).subscribe({
      next: (response: MessageResponse) => {
        this.successMessage = response.message;
        this.codeRequested = false;
        this.codigo = '';
        this.novaSenha = '';
        this.confirmacaoNovaSenha = '';
        this.stopExpirationCountdown();
        this.isResettingPassword = false;
      },
      error: (error: HttpErrorResponse) => {
        this.errorMessage = error.error?.detail ?? 'Não foi possível redefinir a senha.';
        this.isResettingPassword = false;
      }
    });
  }

  ngOnDestroy(): void {
    this.stopExpirationCountdown();
  }

  private isValidEmail(email: string): boolean {
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.trim());
  }

  private startExpirationCountdown(): void {
    this.stopExpirationCountdown();
    this.expirationTimestamp = Date.now() + (10 * 60 * 1000);
    this.updateExpirationLabel();

    this.countdownIntervalId = window.setInterval(() => {
      this.updateExpirationLabel();
    }, 1000);
  }

  private stopExpirationCountdown(): void {
    if (this.countdownIntervalId !== null) {
      window.clearInterval(this.countdownIntervalId);
      this.countdownIntervalId = null;
    }

    this.expirationTimestamp = null;
    this.codeExpirationLabel = '';
  }

  private updateExpirationLabel(): void {
    if (!this.expirationTimestamp) {
      this.codeExpirationLabel = '';
      return;
    }

    const remainingMilliseconds = this.expirationTimestamp - Date.now();

    if (remainingMilliseconds <= 0) {
      this.stopExpirationCountdown();
      this.codeRequested = false;
      this.successMessage = '';
      this.errorMessage = 'O código expirou. Solicite um novo envio para continuar.';
      return;
    }

    const remainingSeconds = Math.ceil(remainingMilliseconds / 1000);
    const minutes = Math.floor(remainingSeconds / 60).toString().padStart(2, '0');
    const seconds = (remainingSeconds % 60).toString().padStart(2, '0');
    this.codeExpirationLabel = `${minutes}:${seconds}`;
  }
}
