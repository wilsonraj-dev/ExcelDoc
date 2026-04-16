import { NgModule } from '@angular/core';
import { SharedModule } from '../../shared/shared.module';
import { ColecoesHomeComponent } from './components/colecoes-home/colecoes-home.component';
import { ColecoesRoutingModule } from './colecoes-routing.module';

@NgModule({
  declarations: [ColecoesHomeComponent],
  imports: [SharedModule, ColecoesRoutingModule]
})
export class ColecoesModule {}
