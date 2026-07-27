import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService, LoginResponse, SapBase } from '../../../../core/services/auth.service';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})
export class LoginComponent implements OnInit {
  bases: SapBase[] = [];
  basesErrorMessage = '';
  database = '';
  errorMessage = '';
  hidePassword = true;
  isLoadingBases = false;
  isSubmitting = false;
  login = '';
  senha = '';

  constructor(
    private readonly authService: AuthService,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    const session = this.authService.getSession();

    if (session) {
      void this.router.navigate([this.authService.getDefaultRoute(session)]);
      return;
    }

    this.loadBases();
  }

  onSubmit(): void {
    this.errorMessage = '';

    if (!this.database || !this.login.trim() || !this.senha.trim()) {
      this.errorMessage = 'Selecione a base e informe o usuário e a senha do SAP Business One.';
      return;
    }

    this.isSubmitting = true;
    this.authService.login({
      database: this.database,
      login: this.login.trim(),
      senha: this.senha
    }).subscribe({
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

  retryLoadBases(): void {
    this.loadBases();
  }

  private loadBases(): void {
    this.basesErrorMessage = '';
    this.isLoadingBases = true;

    this.authService.getBases()
      .pipe(finalize(() => {
        this.isLoadingBases = false;
      }))
      .subscribe({
        next: (bases: SapBase[]) => {
          this.bases = bases;

          if (bases.length === 1) {
            this.database = bases[0].database;
          }

          if (!bases.length) {
            this.basesErrorMessage = 'Nenhuma base do SAP Business One foi configurada.';
          }
        },
        error: (error: HttpErrorResponse) => {
          this.bases = [];
          this.database = '';
          this.basesErrorMessage = error.error?.detail ?? 'Não foi possível carregar as bases do SAP Business One.';
        }
      });
  }
}
