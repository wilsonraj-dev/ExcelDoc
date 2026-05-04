import { NgModule } from '@angular/core';
import { NovoMapeamentoDialogComponent } from './components/novo-mapeamento-dialog/novo-mapeamento-dialog.component';
import { SharedModule } from '../../shared/shared.module';
import { MapeamentoEditorComponent } from './components/mapeamento-editor/mapeamento-editor.component';
import { MapeamentoHomeComponent } from './components/mapeamento-home/mapeamento-home.component';
import { MapeamentoListComponent } from './components/mapeamento-list/mapeamento-list.component';
import { MapeamentoRoutingModule } from './mapeamento-routing.module';

@NgModule({
  declarations: [
    MapeamentoListComponent,
    MapeamentoEditorComponent,
    MapeamentoHomeComponent,
    NovoMapeamentoDialogComponent
  ],
  imports: [SharedModule, MapeamentoRoutingModule]
})
export class MapeamentoModule {}
