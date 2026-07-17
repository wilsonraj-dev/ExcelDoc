import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, OnInit, ViewChild, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatDialog } from '@angular/material/dialog';
import { ActivatedRoute, Router } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService } from '../../../../core/services/auth.service';
import { NotificationService } from '../../../../core/services/notification.service';
import { TranslateService } from '../../../../core/services/translate.service';
import {
  CloneNameDialogComponent,
  CloneNameDialogResult
} from '../../../../shared/components/clone-name-dialog/clone-name-dialog.component';
import { Documento } from '../../../documentos/models/documento.model';
import { DocumentoService } from '../../../documentos/services/documento.service';
import { PerfilMapeamento, PerfilMapeamentoItem } from '../../../perfil-mapeamento/models/perfil-mapeamento.model';
import { PerfilMapeamentoService } from '../../../perfil-mapeamento/services/perfil-mapeamento.service';
import { Mapeamento } from '../../models/mapeamento.model';
import {
  MapeamentoEditorComponent,
  MapeamentoEditorState
} from '../mapeamento-editor/mapeamento-editor.component';

interface WorkspaceTreeItem {
  item: PerfilMapeamentoItem;
  depth: number;
  hasChildren: boolean;
}

type WorkspaceTab = 'mapping' | 'document';

@Component({
  selector: 'app-mapeamento-list',
  templateUrl: './mapeamento-list.component.html',
  styleUrl: './mapeamento-list.component.css'
})
export class MapeamentoListComponent implements OnInit {
  @ViewChild(MapeamentoEditorComponent) editor?: MapeamentoEditorComponent;

  documentos: Documento[] = [];
  perfis: PerfilMapeamento[] = [];
  treeItems: WorkspaceTreeItem[] = [];
  selectedDocumentoId: number | null = null;
  selectedPerfilId: number | null = null;
  selectedItemId: number | null = null;
  selectedMapeamento: Mapeamento | null = null;
  selectedTab: WorkspaceTab = 'mapping';
  isLoadingDocumentos = false;
  isLoadingPerfis = false;
  isCloning = false;
  loadError = '';
  editorState: MapeamentoEditorState = {
    hasChanges: false,
    hasErrors: false,
    isSaving: false
  };

  private readonly destroyRef = inject(DestroyRef);
  private readonly requestedDocumentoId: number | null;
  private readonly requestedPerfilId: number | null;
  private perfisRequestVersion = 0;

  constructor(
    private readonly authService: AuthService,
    private readonly documentoService: DocumentoService,
    private readonly perfilService: PerfilMapeamentoService,
    private readonly notificationService: NotificationService,
    private readonly translate: TranslateService,
    private readonly dialog: MatDialog,
    private readonly route: ActivatedRoute,
    private readonly router: Router
  ) {
    this.requestedDocumentoId = this.toPositiveNumber(this.route.snapshot.queryParamMap.get('documento'));
    this.requestedPerfilId = this.toPositiveNumber(this.route.snapshot.queryParamMap.get('configuracao'));
  }

  ngOnInit(): void {
    this.loadDocumentos();
  }

  get documentoAtual(): Documento | null {
    return this.documentos.find((documento) => documento.id === this.selectedDocumentoId) ?? null;
  }

  get perfilAtual(): PerfilMapeamento | null {
    return this.perfis.find((perfil) => perfil.id === this.selectedPerfilId) ?? null;
  }

  get itemAtual(): PerfilMapeamentoItem | null {
    return this.perfilAtual?.itens.find((item) => item.id === this.selectedItemId) ?? null;
  }

  get isPadrao(): boolean {
    return this.perfilAtual?.isPadrao ?? false;
  }

  get isLegacySharedMapping(): boolean {
    return !this.isPadrao && (this.itemAtual?.isMapeamentoPadrao ?? false);
  }

  get isReadOnly(): boolean {
    return this.isPadrao || this.isLegacySharedMapping;
  }

  get canClone(): boolean {
    return !!this.perfilAtual && this.authService.getSession()?.empresaId != null;
  }

  get canSave(): boolean {
    return !this.isReadOnly
      && this.editorState.hasChanges
      && !this.editorState.hasErrors
      && !this.editorState.isSaving;
  }

  get collectionCount(): number {
    return this.perfilAtual?.itens.length ?? 0;
  }

  get totalFieldCount(): number {
    return this.perfilAtual?.itens.reduce((total, item) => total + (item.quantidadeCampos ?? 0), 0) ?? 0;
  }

  get selectedLevel(): number {
    return (this.treeItems.find((treeItem) => treeItem.item.id === this.selectedItemId)?.depth ?? 0) + 1;
  }

  onDocumentoChange(documentoId: number): void {
    if (documentoId === this.selectedDocumentoId || !this.canLeaveCurrentMapping()) {
      return;
    }

    this.selectedDocumentoId = documentoId;
    this.selectedPerfilId = null;
    this.clearSelection();
    this.loadPerfis(documentoId);
  }

  onPerfilChange(perfilId: number): void {
    if (perfilId === this.selectedPerfilId || !this.canLeaveCurrentMapping()) {
      return;
    }

    this.selectPerfil(perfilId);
  }

  selectCollection(treeItem: WorkspaceTreeItem): void {
    if (treeItem.item.id === this.selectedItemId || !this.canLeaveCurrentMapping()) {
      return;
    }

    this.applyItemSelection(treeItem.item);
    this.updateUrl();
  }

  selectTab(tab: WorkspaceTab): void {
    if (tab !== this.selectedTab && !this.canLeaveCurrentMapping()) {
      return;
    }

    this.selectedTab = tab;
  }

  cloneAndCustomize(): void {
    const perfil = this.perfilAtual;
    if (!perfil || !this.canClone) {
      this.notificationService.showInfo(this.translate.instant('mapeamento.workspace.feedback.companyRequired'));
      return;
    }

    this.dialog.open(CloneNameDialogComponent, {
      width: '420px',
      data: {
        title: this.translate.instant('mapeamento.workspace.cloneDialog.title'),
        label: this.translate.instant('mapeamento.workspace.cloneDialog.label'),
        initialValue: `${perfil.nome} - ${this.translate.instant('mapeamento.workspace.cloneDialog.copySuffix')}`,
        confirmLabel: this.translate.instant('mapeamento.workspace.actions.clone')
      }
    }).afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result?: CloneNameDialogResult) => {
        const nome = result?.nome?.trim();
        if (!nome) {
          return;
        }

        this.isCloning = true;
        this.perfilService.clone(perfil.id, nome)
          .pipe(
            finalize(() => { this.isCloning = false; }),
            takeUntilDestroyed(this.destroyRef)
          )
          .subscribe({
            next: (clone) => {
              this.notificationService.showSuccess(this.translate.instant('mapeamento.workspace.feedback.cloned'));
              this.loadPerfis(perfil.fk_IdDocumento, clone.id);
            },
            error: (error: HttpErrorResponse) => {
              this.notificationService.showError(
                error.error?.detail ?? this.translate.instant('mapeamento.workspace.feedback.cloneError')
              );
            }
          });
      });
  }

  openExcelPreview(): void {
    this.editor?.openExcelPreview();
  }

  saveMapping(): void {
    if (this.canSave) {
      this.editor?.salvar();
    }
  }

  onEditorStateChange(state: MapeamentoEditorState): void {
    this.editorState = state;
  }

  onFieldsChanged(): void {
    const perfilId = this.selectedPerfilId;
    if (this.selectedDocumentoId && perfilId) {
      this.loadPerfis(this.selectedDocumentoId, perfilId, this.selectedItemId);
    }
  }

  getCollectionIcon(treeItem: WorkspaceTreeItem): string {
    const name = treeItem.item.nomeColecao.toLocaleLowerCase('pt-BR');
    if (name.includes('cabe') || name.includes('header')) return 'description';
    if (name.includes('parcela') || name.includes('installment')) return 'calendar_month';
    if (name.includes('anexo') || name.includes('attachment')) return 'attach_file';
    if (treeItem.depth > 0) return 'account_tree';
    return 'table_rows';
  }

  private loadDocumentos(): void {
    this.isLoadingDocumentos = true;
    this.loadError = '';

    this.documentoService.getAll()
      .pipe(
        finalize(() => { this.isLoadingDocumentos = false; }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (documentos) => {
          this.documentos = [...documentos].sort((left, right) =>
            left.nomeDocumento.localeCompare(right.nomeDocumento, 'pt-BR', { sensitivity: 'base' })
          );

          if (!this.documentos.length) {
            this.loadError = this.translate.instant('mapeamento.workspace.empty.noDocuments');
            return;
          }

          const requested = this.documentos.find((item) => item.id === this.requestedDocumentoId);
          this.selectedDocumentoId = requested?.id ?? this.documentos[0].id;
          this.loadPerfis(this.selectedDocumentoId, this.requestedPerfilId ?? undefined);
        },
        error: (error: HttpErrorResponse) => {
          this.loadError = error.error?.detail ?? this.translate.instant('mapeamento.workspace.feedback.loadDocumentsError');
          this.notificationService.showError(this.loadError);
        }
      });
  }

  private loadPerfis(documentoId: number, selectedPerfilId?: number, selectedItemId?: number | null): void {
    const requestVersion = ++this.perfisRequestVersion;
    this.isLoadingPerfis = true;
    this.loadError = '';

    this.perfilService.getByDocumento(documentoId)
      .pipe(
        finalize(() => {
          if (requestVersion === this.perfisRequestVersion) {
            this.isLoadingPerfis = false;
          }
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (perfis) => {
          if (requestVersion !== this.perfisRequestVersion || documentoId !== this.selectedDocumentoId) {
            return;
          }

          this.perfis = [...perfis].sort((left, right) => {
            if (left.isPadrao !== right.isPadrao) return left.isPadrao ? -1 : 1;
            return left.nome.localeCompare(right.nome, 'pt-BR', { sensitivity: 'base' });
          });

          if (!this.perfis.length) {
            this.loadError = this.translate.instant('mapeamento.workspace.empty.noMappings');
            this.selectedPerfilId = null;
            this.clearSelection();
            return;
          }

          const preferredId = selectedPerfilId
            ?? (this.perfis.some((perfil) => perfil.id === this.selectedPerfilId) ? this.selectedPerfilId : null)
            ?? this.perfis.find((perfil) => perfil.isPadrao)?.id
            ?? this.perfis[0].id;

          this.selectPerfil(preferredId, selectedItemId);
        },
        error: (error: HttpErrorResponse) => {
          if (requestVersion !== this.perfisRequestVersion || documentoId !== this.selectedDocumentoId) {
            return;
          }

          this.perfis = [];
          this.clearSelection();
          this.loadError = error.error?.detail ?? this.translate.instant('mapeamento.workspace.feedback.loadMappingsError');
          this.notificationService.showError(this.loadError);
        }
      });
  }

  private selectPerfil(perfilId: number, preferredItemId?: number | null): void {
    const perfil = this.perfis.find((item) => item.id === perfilId) ?? this.perfis[0];
    if (!perfil) {
      this.clearSelection();
      return;
    }

    this.selectedPerfilId = perfil.id;
    this.treeItems = this.buildTree(perfil.itens);

    const preferredItem = perfil.itens.find((item) => item.id === preferredItemId)
      ?? this.treeItems[0]?.item
      ?? null;

    if (preferredItem) {
      this.applyItemSelection(preferredItem);
    } else {
      this.clearSelection(false);
    }

    this.updateUrl();
  }

  private applyItemSelection(item: PerfilMapeamentoItem): void {
    this.selectedItemId = item.id;
    this.selectedMapeamento = {
      id: item.fk_IdMapeamento,
      nome: item.nomeMapeamento,
      fk_IdColecao: item.fk_IdColecao,
      fk_IdEmpresa: this.perfilAtual?.fk_IdEmpresa ?? null,
      isPadrao: item.isMapeamentoPadrao ?? this.isPadrao,
      quantidadeCampos: item.quantidadeCampos ?? 0
    };
    this.editorState = { hasChanges: false, hasErrors: false, isSaving: false };
  }

  private buildTree(items: PerfilMapeamentoItem[]): WorkspaceTreeItem[] {
    const childrenByParent = new Map<number | null, PerfilMapeamentoItem[]>();

    for (const item of items) {
      const parentId = item.fk_IdPerfilMapeamentoItemPai ?? null;
      const children = childrenByParent.get(parentId) ?? [];
      children.push(item);
      childrenByParent.set(parentId, children);
    }

    const sortItems = (values: PerfilMapeamentoItem[]): PerfilMapeamentoItem[] =>
      [...values].sort((left, right) => {
        const leftHeader = this.isHeaderName(left.nomeColecao);
        const rightHeader = this.isHeaderName(right.nomeColecao);
        if (leftHeader !== rightHeader) return leftHeader ? -1 : 1;
        return left.nomeColecao.localeCompare(right.nomeColecao, 'pt-BR', { sensitivity: 'base' });
      });

    const result: WorkspaceTreeItem[] = [];
    const visited = new Set<number>();
    const visit = (item: PerfilMapeamentoItem, depth: number): void => {
      if (visited.has(item.id)) return;
      visited.add(item.id);
      const children = sortItems(childrenByParent.get(item.id) ?? []);
      result.push({ item, depth, hasChildren: children.length > 0 });
      children.forEach((child) => visit(child, depth + 1));
    };

    sortItems(childrenByParent.get(null) ?? []).forEach((root) => visit(root, 0));
    sortItems(items.filter((item) => !visited.has(item.id))).forEach((item) => visit(item, 0));
    return result;
  }

  private canLeaveCurrentMapping(): boolean {
    if (!this.editorState.hasChanges) {
      return true;
    }

    this.notificationService.showInfo(this.translate.instant('mapeamento.workspace.feedback.unsavedChanges'));
    return false;
  }

  private clearSelection(clearTree = true): void {
    if (clearTree) this.treeItems = [];
    this.selectedItemId = null;
    this.selectedMapeamento = null;
    this.editorState = { hasChanges: false, hasErrors: false, isSaving: false };
  }

  private updateUrl(): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        documento: this.selectedDocumentoId,
        configuracao: this.selectedPerfilId
      },
      queryParamsHandling: 'merge',
      replaceUrl: true
    });
  }

  private isHeaderName(name: string): boolean {
    const normalized = name.toLocaleLowerCase('pt-BR');
    return normalized.includes('cabe') || normalized.includes('header');
  }

  private toPositiveNumber(value: string | null): number | null {
    const parsed = Number(value);
    return Number.isInteger(parsed) && parsed > 0 ? parsed : null;
  }
}
