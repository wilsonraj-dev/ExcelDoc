import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, OnInit, ViewChild, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { MatPaginator, PageEvent } from '@angular/material/paginator';
import { finalize } from 'rxjs';
import { CompanySettingsService, EmpresaResponse } from '../../../../core/services/company-settings.service';
import { Usuario } from '../../models/usuario.models';
import { UsuariosService } from '../../services/usuarios.service';

@Component({
  selector: 'app-usuarios',
  templateUrl: './usuarios.component.html',
  styleUrl: './usuarios.component.css'
})
export class UsuariosComponent implements OnInit {
  readonly displayedColumns = ['nomeUsuario', 'email', 'tipoUsuario', 'empresa', 'status', 'acoes'];
  readonly pageSizeOptions = [5, 10, 20];
  readonly form = new FormGroup({
    nomeUsuario: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(150)] }),
    email: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.email, Validators.maxLength(200)] }),
    senha: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.minLength(6), Validators.maxLength(200)] }),
    confirmacaoSenha: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.minLength(6), Validators.maxLength(200)] }),
    empresaId: new FormControl<number | null>(null)
  });

  empresas: EmpresaResponse[] = [];
  errorMessage = '';
  isLoadingEmpresas = false;
  isLoadingUsuarios = false;
  isSubmitting = false;
  linkingUsuarioId: number | null = null;
  searchTerm = '';
  successMessage = '';
  totalCount = 0;
  usuarios: Usuario[] = [];

  private readonly destroyRef = inject(DestroyRef);
  public pageNumber = 1;
  public pageSize = this.pageSizeOptions[0];
  selectedEmpresaIds: Record<number, number | null> = {};

  @ViewChild(MatPaginator) paginator?: MatPaginator;

  constructor(
    private readonly companySettingsService: CompanySettingsService,
    private readonly usuariosService: UsuariosService
  ) { }

  get hasData(): boolean {
    return this.usuarios.length > 0;
  }

  get isBusy(): boolean {
    return this.isLoadingEmpresas || this.isLoadingUsuarios || this.isSubmitting || this.linkingUsuarioId !== null;
  }

  ngOnInit(): void {
    this.loadEmpresas();
    this.loadUsuarios();
  }

  buscarUsuarios(): void {
    this.goToFirstPage();
    this.loadUsuarios(1, this.pageSize);
  }

  isCreateControlInvalid(controlName: string): boolean {
    const control = this.form.get(controlName);
    return !!control && control.invalid && (control.dirty || control.touched);
  }

  onPageChange(event: PageEvent): void {
    this.pageNumber = event.pageIndex + 1;
    this.pageSize = event.pageSize;
    this.loadUsuarios(this.pageNumber, this.pageSize);
  }

  onSubmit(): void {
    this.clearFeedback();

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { confirmacaoSenha, email, empresaId, nomeUsuario, senha } = this.form.getRawValue();
    if (senha !== confirmacaoSenha) {
      this.errorMessage = 'A confirmação da senha não confere.';
      return;
    }

    this.isSubmitting = true;
    this.usuariosService.create({
      nomeUsuario: nomeUsuario.trim(),
      email: email.trim(),
      senha,
      empresaId: empresaId ?? undefined
    })
      .pipe(
        finalize(() => {
          this.isSubmitting = false;
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (response) => {
          this.successMessage = `Usuário ${response.nomeUsuario} criado com sucesso.`;
          this.form.reset({
            nomeUsuario: '',
            email: '',
            senha: '',
            confirmacaoSenha: '',
            empresaId: null
          });
          this.goToFirstPage();
          this.loadUsuarios(1, this.pageSize);
        },
        error: (error: HttpErrorResponse) => {
          this.errorMessage = error.error?.detail ?? 'Não foi possível criar o usuário.';
        }
      });
  }

  podeVincular(usuario: Usuario): boolean {
    return usuario.tipoUsuario !== 'Administrador';
  }

  vincularEmpresa(usuario: Usuario): void {
    this.clearFeedback();

    if (!this.podeVincular(usuario)) {
      this.errorMessage = 'Usuários administradores não podem ser vinculados por esta funcionalidade.';
      return;
    }

    const empresaId = this.selectedEmpresaIds[usuario.id];
    if (!empresaId) {
      this.errorMessage = 'Selecione uma empresa para vincular o usuário.';
      return;
    }

    this.linkingUsuarioId = usuario.id;
    this.usuariosService.vincularEmpresa(usuario.id, { empresaId })
      .pipe(
        finalize(() => {
          this.linkingUsuarioId = null;
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (response) => {
          this.successMessage = `Usuário ${response.nomeUsuario} vinculado com sucesso.`;
          this.loadUsuarios(this.pageNumber, this.pageSize);
        },
        error: (error: HttpErrorResponse) => {
          this.errorMessage = error.error?.detail ?? 'Não foi possível vincular o usuário à empresa.';
        }
      });
  }

  private bindSelectedEmpresas(): void {
    this.selectedEmpresaIds = this.usuarios.reduce<Record<number, number | null>>((accumulator, usuario) => {
      accumulator[usuario.id] = usuario.empresaId ?? null;
      return accumulator;
    }, {});
  }

  private clearFeedback(): void {
    this.errorMessage = '';
    this.successMessage = '';
  }

  private goToFirstPage(): void {
    this.pageNumber = 1;
    this.paginator?.firstPage();
  }

  private loadEmpresas(): void {
    this.isLoadingEmpresas = true;
    this.companySettingsService.getEmpresas()
      .pipe(
        finalize(() => {
          this.isLoadingEmpresas = false;
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (empresas: EmpresaResponse[]) => {
          this.empresas = empresas;
        },
        error: (error: HttpErrorResponse) => {
          this.errorMessage = error.error?.detail ?? 'Não foi possível carregar as empresas disponíveis.';
        }
      });
  }

  private loadUsuarios(pageNumber = this.pageNumber, pageSize = this.pageSize): void {
    this.isLoadingUsuarios = true;
    this.pageNumber = pageNumber;
    this.pageSize = pageSize;

    this.usuariosService.getPaged({
      termo: this.searchTerm,
      pageNumber,
      pageSize
    })
      .pipe(
        finalize(() => {
          this.isLoadingUsuarios = false;
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (response) => {
          this.usuarios = response.items;
          this.totalCount = response.totalCount;
          this.pageNumber = response.pageNumber;
          this.pageSize = response.pageSize;
          this.bindSelectedEmpresas();
        },
        error: (error: HttpErrorResponse) => {
          this.errorMessage = error.error?.detail ?? 'Não foi possível carregar os usuários.';
        }
      });
  }
}
