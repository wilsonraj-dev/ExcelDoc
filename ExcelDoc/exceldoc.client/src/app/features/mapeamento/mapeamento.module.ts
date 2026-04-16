import { NgModule } from '@angular/core';
import { SharedModule } from '../../shared/shared.module';
import { MapeamentoEditorComponent } from './components/mapeamento-editor/mapeamento-editor.component';
import { MapeamentoListComponent } from './components/mapeamento-list/mapeamento-list.component';
import { MapeamentoRoutingModule } from './mapeamento-routing.module';

@NgModule({
  declarations: [MapeamentoListComponent, MapeamentoEditorComponent],
  imports: [SharedModule, MapeamentoRoutingModule]
})
export class MapeamentoModule {}
