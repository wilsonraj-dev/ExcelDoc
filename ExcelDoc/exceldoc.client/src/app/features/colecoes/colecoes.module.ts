import { NgModule } from '@angular/core';
import { SharedModule } from '../../shared/shared.module';
import { ColecaoFormComponent } from './components/colecao-form/colecao-form.component';
import { ColecaoListComponent } from './components/colecao-list/colecao-list.component';
import { ColecoesRoutingModule } from './colecoes-routing.module';

@NgModule({
  declarations: [ColecaoListComponent, ColecaoFormComponent],
  imports: [SharedModule, ColecoesRoutingModule]
})
export class ColecoesModule {}
