import { HttpErrorResponse } from '@angular/common/http';
import { AfterViewInit, Component, DestroyRef, OnInit, ViewChild, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { MatPaginator } from '@angular/material/paginator';
import { MatTableDataSource } from '@angular/material/table';
import { ActivatedRoute, Router } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService, LoginResponse } from '../../../../core/services/auth.service';
import { CompanySettingsService, ConfiguracaoRequest, ConfiguracaoResponse, EmpresaResponse } from '../../../../core/services/company-settings.service';
import { TranslateService } from '../../../../core/services/translate.service';

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
  errorMessageKey = '';
  infoMessage = '';
  infoMessageKey = '';
  isAdministrator = false;
  loadingConfiguration = false;
  loadingEmpresas = false;
  saving = false;
  selectedEmpresa: EmpresaResponse | null = null;
  selectedEmpresaId: number | null = null;
  session: LoginResponse | null;
  successMessage = '';
  successMessageKey = '';
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
    private readonly router: Router,
    private readonly translate: TranslateService
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

  get errorFeedback(): string {
    return this.errorMessageKey ? this.translate.instant(this.errorMessageKey) : this.errorMessage;
  }

  get infoFeedback(): string {
    return this.infoMessageKey ? this.translate.instant(this.infoMessageKey) : this.infoMessage;
  }

  get successFeedback(): string {
    return this.successMessageKey ? this.translate.instant(this.successMessageKey) : this.successMessage;
  }

  get pageTitle(): string {
    if (!this.selectedEmpresa) {
      return this.isAdministrator
        ? this.translate.instant('companySettings.pageTitleAdmin')
        : this.translate.instant('companySettings.pageTitleUser');
    }

    return `${this.translate.instant('companySettings.pageTitleSelectedPrefix')} ${this.selectedEmpresa.nomeEmpresa}`;
  }

  get submitLabel(): string {
    if (this.saving) {
      return this.translate.instant('companySettings.saving');
    }

    return this.hasExistingConfiguration
      ? this.translate.instant('companySettings.submitSaveChanges')
      : this.translate.instant('companySettings.submitCreate');
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
      this.setErrorKey('companySettings.errors.noLinkedCompany');
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
      this.setErrorKey('companySettings.errors.selectCompany');
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
          this.setSuccessKey('companySettings.successSaved');
          this.form.reset({
            linkServiceLayer: response.linkServiceLayer,
            database: response.database,
            usuarioSAP: response.usuarioSAP,
            senhaSAP: response.senhaSAP
          });
        },
        error: (error: HttpErrorResponse) => {
          this.setErrorMessage(error.error?.detail, 'companySettings.errors.saveConfiguration');
        }
      });
  }

  visualizarConfiguracao(empresa: EmpresaResponse): void {
    void this.router.navigate(['/empresa/configuracoes'], {
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

    void this.router.navigate(['/empresa/configuracoes']);
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
      this.setErrorKey('companySettings.error.invalidCompany');
      return;
    }

    this.selectedEmpresaId = empresaId;
    this.syncSelectedEmpresa();
  }

  private clearFeedback(clearInfoMessage: boolean): void {
    this.errorMessage = '';
    this.errorMessageKey = '';
    this.successMessage = '';
    this.successMessageKey = '';

    if (clearInfoMessage) {
      this.infoMessage = '';
      this.infoMessageKey = '';
    }
  }

  private setErrorKey(key: string): void {
    this.errorMessage = '';
    this.errorMessageKey = key;
  }

  private setErrorMessage(message: string | undefined | null, fallbackKey: string): void {
    if (message) {
      this.errorMessage = message;
      this.errorMessageKey = '';
      return;
    }

    this.setErrorKey(fallbackKey);
  }

  private setInfoKey(key: string): void {
    this.infoMessage = '';
    this.infoMessageKey = key;
  }

  private setSuccessKey(key: string): void {
    this.successMessage = '';
    this.successMessageKey = key;
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
            usuarioSAP: response.usuarioSAP,
            senhaSAP: response.senhaSAP
          });
        },
        error: (error: HttpErrorResponse) => {
          if (error.status === 404) {
            this.loadedEmpresaId = empresaId;
            this.setInfoKey('companySettings.info.noConfig');
            return;
          }

          this.setErrorMessage(error.error?.detail, 'companySettings.errors.loadConfiguration');
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
          this.setErrorMessage(error.error?.detail, 'companySettings.errors.loadCompanies');
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
        this.setErrorKey('companySettings.errors.selectedUnavailable');
      }
      return;
    }

    this.loadConfiguracao(empresa.id);
  }
}
