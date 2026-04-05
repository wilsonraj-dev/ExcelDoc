import { HttpErrorResponse } from '@angular/common/http';
import { Component } from '@angular/core';
import { AuthService, RegisterResponse } from '../auth.service';

@Component({
  selector: 'app-create-user',
  templateUrl: './create-user.component.html',
  styleUrl: './create-user.component.css'
})
export class CreateUserComponent {
  confirmacaoSenha = '';
  email = '';
  errorMessage = '';
  isSubmitting = false;
  nomeUsuario = '';
  registeredEmail = '';
  senha = '';
  successMessage = '';

  constructor(private readonly authService: AuthService) {}

  onSubmit(): void {
    this.errorMessage = '';
    this.successMessage = '';
    this.registeredEmail = '';

    if (!this.nomeUsuario.trim() || !this.email.trim() || !this.senha.trim()) {
      this.errorMessage = 'Preencha usuário, e-mail e senha.';
      return;
    }

    if (!this.isValidEmail(this.email)) {
      this.errorMessage = 'Informe um e-mail válido.';
      return;
    }

    if (this.senha.length < 6) {
      this.errorMessage = 'A senha deve ter pelo menos 6 caracteres.';
      return;
    }

    if (this.senha !== this.confirmacaoSenha) {
      this.errorMessage = 'A confirmação da senha não confere.';
      return;
    }

    this.isSubmitting = true;
    this.authService.register({
      nomeUsuario: this.nomeUsuario.trim(),
      email: this.email.trim(),
      senha: this.senha
    }).subscribe({
      next: (response: RegisterResponse) => {
        this.registeredEmail = response.email;
        this.successMessage = `Usuário ${response.nomeUsuario} criado com sucesso.`;
        this.nomeUsuario = '';
        this.email = '';
        this.senha = '';
        this.confirmacaoSenha = '';
        this.isSubmitting = false;
      },
      error: (error: HttpErrorResponse) => {
        this.errorMessage = error.error?.detail ?? 'Não foi possível criar o usuário.';
        this.isSubmitting = false;
      }
    });
  }

  private isValidEmail(email: string): boolean {
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.trim());
  }
}
