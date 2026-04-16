import { HttpErrorResponse } from '@angular/common/http';
import { AfterViewInit, Component, DestroyRef, OnInit, ViewChild, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { MatPaginator } from '@angular/material/paginator';
import { MatTableDataSource } from '@angular/material/table';
import { ActivatedRoute, Router } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService, LoginResponse } from '../services/auth.service';
import { CompanySettingsService, ConfiguracaoRequest, ConfiguracaoResponse, EmpresaResponse } from '../services/company-settings.service';

interface CompanySettingsFormValue {
  linkServiceLayer: string;
  database: string;
  usuarioBanco: string;
  senhaBanco: string;
  usuarioSAP: string;
  senhaSAP: string;
}

@Component({
  selector: 'app-company-settings',
  templateUrl: './company-settings.component.html',
  styleUrl: './company-settings.component.css'
})
export class CompanySettingsComponent implements OnInit, AfterViewInit {
  readonly displayedColumns = ['nomeEmpresa', 'acoes'];
  readonly pageSizeOptions = [5, 10, 15, 20, 25];
  readonly form = new FormGroup({
    linkServiceLayer: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(500)] }),
    database: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(150)] }),
    usuarioBanco: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(500)] }),
    senhaBanco: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(500)] }),
    usuarioSAP: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(500)] }),
    senhaSAP: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.maxLength(500)] })
  });

  empresas: EmpresaResponse[] = [];
  readonly empresasDataSource = new MatTableDataSource<EmpresaResponse>([]);
  errorMessage = '';
  infoMessage = '';
  isAdministrator = false;
  loadingConfiguration = false;
  loadingEmpresas = false;
  saving = false;
  selectedEmpresa: EmpresaResponse | null = null;
  selectedEmpresaId: number | null = null;
  session: LoginResponse | null;
  successMessage = '';
  hasExistingConfiguration = false;

  private readonly destroyRef = inject(DestroyRef);
  private loadedEmpresaId: number | null = null;

  @ViewChild(MatPaginator)
  set paginator(paginator: MatPaginator | undefined) {
    if (paginator) {
      this.empresasDataSource.paginator = paginator;
    }
  }

  constructor(
    private readonly activatedRoute: ActivatedRoute,
    private readonly authService: AuthService,
    private readonly companySettingsService: CompanySettingsService,
    private readonly router: Router
  ) {
    this.session = this.authService.getSession();
    this.isAdministrator = this.authService.isAdministrator(this.session);
  }

  get canShowForm(): boolean {
    return this.selectedEmpresa !== null;
  }

  get isBusy(): boolean {
    return this.loadingEmpresas || this.loadingConfiguration || this.saving;
  }

  get pageTitle(): string {
    if (!this.selectedEmpresa) {
      return this.isAdministrator ? 'Configurações das empresas' : 'Configuração da empresa';
    }

    return `Configuração da empresa ${this.selectedEmpresa.nomeEmpresa}`;
  }

  get submitLabel(): string {
    if (this.saving) {
      return 'Salvando...';
    }

    return this.hasExistingConfiguration ? 'Salvar alterações' : 'Criar configuração';
  }

  ngAfterViewInit(): void {
    if (this.empresasDataSource.paginator) {
      this.empresasDataSource.paginator.pageSize = this.pageSizeOptions[0];
    }
  }

  ngOnInit(): void {
    if (!this.session) {
      void this.router.navigate(['/login']);
      return;
    }

    if (this.isAdministrator) {
      this.loadEmpresas();

      this.activatedRoute.queryParamMap
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe((params) => {
          this.applySelectedEmpresaId(params.get('empresaId'));
        });
      return;
    }

    if (!this.session.empresaId) {
      this.errorMessage = 'O usuário atual não está vinculado a uma empresa.';
      return;
    }

    this.selectedEmpresaId = this.session.empresaId;
    this.loadEmpresas();
  }

  isInvalid(controlName: string): boolean {
    const control = this.form.get(controlName);
    return !!control && control.invalid && (control.dirty || control.touched);
  }

  onSubmit(): void {
    this.clearFeedback(true);

    if (!this.selectedEmpresaId) {
      this.errorMessage = 'Selecione uma empresa para configurar.';
      return;
    }

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const request: ConfiguracaoRequest = {
      empresaId: this.selectedEmpresaId,
      ...this.form.getRawValue()
    };

    this.saving = true;
    this.companySettingsService.saveConfiguracao(request)
      .pipe(
        finalize(() => {
          this.saving = false;
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (response: ConfiguracaoResponse) => {
          this.loadedEmpresaId = response.empresaId;
          this.hasExistingConfiguration = true;
          this.successMessage = 'Configuração salva com sucesso.';
          this.form.reset({
            linkServiceLayer: response.linkServiceLayer,
            database: response.database,
            usuarioBanco: response.usuarioBanco,
            senhaBanco: response.senhaBanco,
            usuarioSAP: response.usuarioSAP,
            senhaSAP: response.senhaSAP
          });
        },
        error: (error: HttpErrorResponse) => {
          this.errorMessage = error.error?.detail ?? 'Não foi possível salvar a configuração da empresa.';
        }
      });
  }

  visualizarConfiguracao(empresa: EmpresaResponse): void {
    void this.router.navigate(['/configuracoes-empresa'], {
      queryParams: { empresaId: empresa.id }
    });
  }

  voltarParaEmpresas(): void {
    this.clearFeedback(true);
    this.form.reset(this.createEmptyFormValue());
    this.selectedEmpresa = null;
    this.selectedEmpresaId = null;
    this.loadedEmpresaId = null;
    this.hasExistingConfiguration = false;

    void this.router.navigate(['/configuracoes-empresa']);
  }

  private applySelectedEmpresaId(rawEmpresaId: string | null): void {
    this.clearFeedback(true);

    if (!rawEmpresaId) {
      this.selectedEmpresa = null;
      this.selectedEmpresaId = null;
      this.loadedEmpresaId = null;
      this.hasExistingConfiguration = false;
      this.form.reset(this.createEmptyFormValue());
      return;
    }

    const empresaId = Number(rawEmpresaId);
    if (!Number.isInteger(empresaId) || empresaId <= 0) {
      this.selectedEmpresa = null;
      this.selectedEmpresaId = null;
      this.loadedEmpresaId = null;
      this.hasExistingConfiguration = false;
      this.form.reset(this.createEmptyFormValue());
      this.errorMessage = 'Empresa inválida para visualização da configuração.';
      return;
    }

    this.selectedEmpresaId = empresaId;
    this.syncSelectedEmpresa();
  }

  private clearFeedback(clearInfoMessage: boolean): void {
    this.errorMessage = '';
    this.successMessage = '';

    if (clearInfoMessage) {
      this.infoMessage = '';
    }
  }

  private createEmptyFormValue(): CompanySettingsFormValue {
    return {
      linkServiceLayer: '',
      database: '',
      usuarioBanco: '',
      senhaBanco: '',
      usuarioSAP: '',
      senhaSAP: ''
    };
  }

  private loadConfiguracao(empresaId: number): void {
    if (this.loadedEmpresaId === empresaId) {
      return;
    }

    this.clearFeedback(true);
    this.loadingConfiguration = true;
    this.hasExistingConfiguration = false;
    this.form.reset(this.createEmptyFormValue());

    this.companySettingsService.getConfiguracao(empresaId)
      .pipe(
        finalize(() => {
          this.loadingConfiguration = false;
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (response: ConfiguracaoResponse) => {
          this.loadedEmpresaId = response.empresaId;
          this.hasExistingConfiguration = true;
          this.form.reset({
            linkServiceLayer: response.linkServiceLayer,
            database: response.database,
            usuarioBanco: response.usuarioBanco,
            senhaBanco: response.senhaBanco,
            usuarioSAP: response.usuarioSAP,
            senhaSAP: response.senhaSAP
          });
        },
        error: (error: HttpErrorResponse) => {
          if (error.status === 404) {
            this.loadedEmpresaId = empresaId;
            this.infoMessage = 'Nenhuma configuração cadastrada para esta empresa. Preencha os campos para criar uma nova configuração.';
            return;
          }

          this.errorMessage = error.error?.detail ?? 'Não foi possível carregar a configuração da empresa.';
        }
      });
  }

  private loadEmpresas(): void {
    this.loadingEmpresas = true;
    this.companySettingsService.getEmpresas()
      .pipe(
        finalize(() => {
          this.loadingEmpresas = false;
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (empresas: EmpresaResponse[]) => {
          this.empresas = [...empresas].sort((left, right) => left.nomeEmpresa.localeCompare(right.nomeEmpresa));
          this.empresasDataSource.data = this.empresas;
          this.syncSelectedEmpresa();
        },
        error: (error: HttpErrorResponse) => {
          this.errorMessage = error.error?.detail ?? 'Não foi possível carregar as empresas disponíveis.';
        }
      });
  }

  private syncSelectedEmpresa(): void {
    if (!this.selectedEmpresaId) {
      return;
    }

    const empresa = this.empresas.find((item) => item.id === this.selectedEmpresaId) ?? null;
    this.selectedEmpresa = empresa;

    if (!empresa) {
      if (!this.loadingEmpresas && this.empresas.length > 0) {
        this.errorMessage = 'A empresa selecionada não está disponível para o usuário atual.';
      }
      return;
    }

    this.loadConfiguracao(empresa.id);
  }
}
