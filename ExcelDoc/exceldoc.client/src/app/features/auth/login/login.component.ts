import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService, LoginResponse } from '../../../core/services/auth.service';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})
export class LoginComponent implements OnInit {
  errorMessage = '';
  hidePassword = true;
  isSubmitting = false;
  login = '';
  senha = '';
  successMessage = '';

  constructor(
    private readonly authService: AuthService,
    private readonly route: ActivatedRoute,
    private readonly router: Router
  ) {
    this.login = this.route.snapshot.queryParamMap.get('login')?.trim() ?? '';
    this.successMessage = this.route.snapshot.queryParamMap.get('message')?.trim() ?? '';
  }

  ngOnInit(): void {
    const session = this.authService.getSession();

    if (session) {
      void this.router.navigate([this.authService.getDefaultRoute(session)]);
    }
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
        this.senha = '';
        this.isSubmitting = false;
        void this.router.navigate([this.authService.getDefaultRoute(response)]);
      },
      error: (error: HttpErrorResponse) => {
        this.errorMessage = error.error?.detail ?? 'Não foi possível realizar o login.';
        this.isSubmitting = false;
      }
    });
  }
}
