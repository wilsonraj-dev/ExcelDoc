import { HttpErrorResponse } from '@angular/common/http';
import { Component } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { AuthService, LoginResponse } from '../auth.service';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})
export class LoginComponent {
  errorMessage = '';
  isSubmitting = false;
  login = '';
  senha = '';
  successMessage = '';
  session: LoginResponse | null;

  constructor(
    private readonly authService: AuthService,
    private readonly route: ActivatedRoute
  ) {
    this.session = this.authService.getSession();
    this.login = this.route.snapshot.queryParamMap.get('login')?.trim() ?? '';
    this.successMessage = this.route.snapshot.queryParamMap.get('message')?.trim() ?? '';
  }

  onSubmit(): void {
    this.errorMessage = '';
    this.successMessage = '';

    if (!this.login.trim() || !this.senha.trim()) {
      this.errorMessage = 'Informe o usuário ou e-mail e a senha.';
      return;
    }

    this.isSubmitting = true;
    this.authService.login({ login: this.login.trim(), senha: this.senha }).subscribe({
      next: (response: LoginResponse) => {
        this.session = response;
        this.successMessage = 'Login realizado com sucesso. O JWT foi salvo no navegador.';
        this.senha = '';
        this.isSubmitting = false;
      },
      error: (error: HttpErrorResponse) => {
        this.errorMessage = error.error?.detail ?? 'Não foi possível realizar o login.';
        this.isSubmitting = false;
      }
    });
  }

  logout(): void {
    this.authService.logout();
    this.session = null;
    this.successMessage = 'Sessão removida do navegador.';
  }
}
