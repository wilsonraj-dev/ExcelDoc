import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService, LoginResponse } from '../services/auth.service';
import { CreateCompanyService, EmpresaResponse } from '../services/create-company.service';

@Component({
  selector: 'app-create-company',
  templateUrl: './create-company.component.html',
  styleUrl: './create-company.component.css'
})
export class CreateCompanyComponent implements OnInit {
  readonly form = new FormGroup({
    nomeEmpresa: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(150)] })
  });

  errorMessage = '';
  saving = false;
  successMessage = '';
  readonly session: LoginResponse | null;
  readonly isAdministrator: boolean;

  private readonly destroyRef = inject(DestroyRef);

  constructor(
    private readonly authService: AuthService,
    private readonly createCompanyService: CreateCompanyService,
    private readonly router: Router
  ) {
    this.session = this.authService.getSession();
    this.isAdministrator = this.authService.isAdministrator(this.session);
  }

  get isBusy(): boolean {
    return this.saving;
  }

  get nomeEmpresaControl(): FormControl<string> {
    return this.form.controls.nomeEmpresa;
  }

  ngOnInit(): void {
    if (!this.session) {
      void this.router.navigate(['/login']);
      return;
    }

    if (!this.isAdministrator) {
      void this.router.navigate([this.authService.getDefaultRoute(this.session)]);
      return;
    }
  }

  isInvalid(): boolean {
    return this.nomeEmpresaControl.invalid && (this.nomeEmpresaControl.dirty || this.nomeEmpresaControl.touched);
  }

  onSubmit(): void {
    this.clearFeedback();

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const nomeEmpresa = this.nomeEmpresaControl.getRawValue().trim();
    if (!nomeEmpresa) {
      this.nomeEmpresaControl.setErrors({ required: true });
      this.nomeEmpresaControl.markAsTouched();
      return;
    }

    this.saving = true;
    this.createCompanyService.createEmpresa({ nomeEmpresa })
      .pipe(
        finalize(() => {
          this.saving = false;
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (response: EmpresaResponse) => {
          this.successMessage = `Empresa ${response.nomeEmpresa} criada com sucesso.`;
          this.form.reset({ nomeEmpresa: '' });
        },
        error: (error: HttpErrorResponse) => {
          this.errorMessage = error.error?.detail ?? 'Não foi possível criar a empresa.';
        }
      });
  }

  private clearFeedback(): void {
    this.errorMessage = '';
    this.successMessage = '';
  }
}
