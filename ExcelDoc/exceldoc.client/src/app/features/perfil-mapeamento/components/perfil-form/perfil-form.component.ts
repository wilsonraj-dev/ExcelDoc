import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, FormControl, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { forkJoin, finalize } from 'rxjs';
import { AuthService } from '../../../../core/services/auth.service';
import { NotificationService } from '../../../../core/services/notification.service';
import { Colecao, resolveTipoColecao } from '../../../colecoes/models/colecao.model';
import { ColecaoService } from '../../../colecoes/services/colecao.service';
import { Documento } from '../../../documentos/models/documento.model';
import { DocumentoService } from '../../../documentos/services/documento.service';
import { Mapeamento } from '../../../mapeamento/models/mapeamento.model';
import { MapeamentoService } from '../../../mapeamento/services/mapeamento.service';
import { PerfilMapeamento, PerfilMapeamentoPayload } from '../../models/perfil-mapeamento.model';
import { PerfilMapeamentoService } from '../../services/perfil-mapeamento.service';

interface ColecaoMapeamentoGroup {
  colecao: Colecao;
  mapeamentos: Mapeamento[];
  isLoadingMapeamentos: boolean;
  control: FormControl<number | null>;
}

@Component({
  selector: 'app-perfil-form',
  templateUrl: './perfil-form.component.html',
  styleUrl: './perfil-form.component.css'
})
export class PerfilFormComponent implements OnInit {
  form!: FormGroup;
  documentos: Documento[] = [];
  colecaoGroups: ColecaoMapeamentoGroup[] = [];
  perfil: PerfilMapeamento | null = null;
  isEditMode = false;
  isReadonly = false;
  isLoading = false;
  isSaving = false;
  isLoadingDocumentos = false;
  isLoadingColecoes = false;

  private readonly destroyRef = inject(DestroyRef);

  constructor(
    private readonly fb: FormBuilder,
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly perfilService: PerfilMapeamentoService,
    private readonly documentoService: DocumentoService,
    private readonly colecaoService: ColecaoService,
    private readonly mapeamentoService: MapeamentoService,
    private readonly notificationService: NotificationService,
    private readonly authService: AuthService
  ) { }

  get isAdministrator(): boolean {
    return this.authService.isAdministrator();
  }

  get empresaId(): number | null {
    return this.authService.getSession()?.empresaId ?? null;
  }

  get isFormValid(): boolean {
    if (!this.form.valid) return false;
    return this.colecaoGroups.every(g => g.control.value !== null);
  }

  get pageTitle(): string {
    if (this.isReadonly) return 'Visualizar Perfil de Mapeamento';
    return this.isEditMode ? 'Editar Perfil de Mapeamento' : 'Novo Perfil de Mapeamento';
  }

  ngOnInit(): void {
    this.form = this.fb.group({
      nome: ['', [Validators.required, Validators.maxLength(150)]],
      documentoId: [null, Validators.required]
    });

    this.form.controls['documentoId'].valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((documentoId) => {
        if (documentoId && !this.isEditMode) {
          this.loadColecoesForDocumento(documentoId as number);
        }
      });

    this.loadDocumentos();
  }

  salvar(): void {
    if (!this.isFormValid || this.isReadonly) return;

    const payload: PerfilMapeamentoPayload = {
      nome: this.form.value.nome,
      fk_IdDocumento: this.form.value.documentoId,
      itens: this.colecaoGroups.map(g => ({
        fk_IdColecao: g.colecao.id,
        fk_IdMapeamento: g.control.value!
      }))
    };

    this.isSaving = true;
    const request$ = this.isEditMode
      ? this.perfilService.update(this.perfil!.id, payload)
      : this.perfilService.create(payload);

    request$
      .pipe(
        finalize(() => { this.isSaving = false; }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: () => {
          this.notificationService.showSuccess(this.isEditMode ? 'Perfil atualizado com sucesso.' : 'Perfil criado com sucesso.');
          void this.router.navigate(['/perfil-mapeamento']);
        },
        error: (err: HttpErrorResponse) => {
          this.notificationService.showError(err.error?.detail ?? 'Erro ao salvar perfil.');
        }
      });
  }

  cancelar(): void {
    void this.router.navigate(['/perfil-mapeamento']);
  }

  getTipoColecaoLabel(colecao: Colecao): string {
    return resolveTipoColecao(colecao.tipoColecao);
  }

  getMapeamentoBadgeClass(mapeamento: Mapeamento): string {
    return mapeamento.isPadrao ? 'mapping-chip mapping-chip--padrao' : 'mapping-chip mapping-chip--empresa';
  }

  getMapeamentoBadgeLabel(mapeamento: Mapeamento): string {
    return mapeamento.isPadrao ? 'Padrão' : 'Empresa';
  }

  private loadDocumentos(): void {
    this.isLoadingDocumentos = true;
    this.documentoService.getAll()
      .pipe(
        finalize(() => { this.isLoadingDocumentos = false; }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (docs) => {
          this.documentos = docs;
          this.checkEditMode();
        },
        error: () => {
          this.notificationService.showError('Erro ao carregar documentos.');
        }
      });
  }

  private checkEditMode(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id && id !== 'novo') {
      this.isEditMode = true;
      this.loadPerfil(+id);
    }
  }

  private loadPerfil(id: number): void {
    this.isLoading = true;
    this.perfilService.getById(id)
      .pipe(
        finalize(() => { this.isLoading = false; }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (perfil) => {
          this.perfil = perfil;
          this.isReadonly = perfil.isPadrao && !this.isAdministrator;

          this.form.patchValue({
            nome: perfil.nome,
            documentoId: perfil.fk_IdDocumento
          });

          if (this.isReadonly) {
            this.form.disable();
          }

          this.loadColecoesForDocumento(perfil.fk_IdDocumento, perfil);
        },
        error: () => {
          this.notificationService.showError('Erro ao carregar perfil.');
          void this.router.navigate(['/perfil-mapeamento']);
        }
      });
  }

  private loadColecoesForDocumento(documentoId: number, perfil?: PerfilMapeamento): void {
    this.isLoadingColecoes = true;
    this.colecaoGroups = [];

    this.colecaoService.getAll()
      .pipe(
        finalize(() => { this.isLoadingColecoes = false; }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (colecoes) => {
          const doc = this.documentos.find(d => d.id === documentoId);
          const docColecaoIds = doc?.colecaoIds ?? doc?.colecoes?.map(c => c.id) ?? [];

          const colecoesDoDocumento = colecoes.filter(c => docColecaoIds.includes(c.id));

          this.colecaoGroups = colecoesDoDocumento.map(colecao => {
            const existingItem = perfil?.itens?.find(i => i.fk_IdColecao === colecao.id);
            const control = new FormControl<number | null>(existingItem?.fk_IdMapeamento ?? null, Validators.required);

            if (this.isReadonly) {
              control.disable();
            }

            const group: ColecaoMapeamentoGroup = {
              colecao,
              mapeamentos: [],
              isLoadingMapeamentos: true,
              control
            };

            this.mapeamentoService.getMapeamentosByColecao(colecao.id)
              .pipe(takeUntilDestroyed(this.destroyRef))
              .subscribe({
                next: (mapeamentos) => {
                  group.mapeamentos = mapeamentos.filter(m => this.isMapeamentoVisible(m));
                  group.isLoadingMapeamentos = false;

                  // Pré-selecionar mapeamento padrão se nenhum selecionado
                  if (!group.control.value && !perfil) {
                    const padrao = group.mapeamentos.find(m => m.isPadrao);
                    if (padrao) {
                      group.control.setValue(padrao.id);
                    }
                  }
                },
                error: () => {
                  group.isLoadingMapeamentos = false;
                  this.notificationService.showError(`Erro ao carregar mapeamentos da coleção ${colecao.nomeColecao}.`);
                }
              });

            return group;
          });
        },
        error: () => {
          this.notificationService.showError('Erro ao carregar coleções.');
        }
      });
  }

  private isMapeamentoVisible(mapeamento: Mapeamento): boolean {
    if (this.isAdministrator) return true;
    if (mapeamento.isPadrao) return true;
    return mapeamento.fk_IdEmpresa === this.empresaId;
  }
}
