import { NgModule } from '@angular/core';
import { SharedModule } from '../../shared/shared.module';
import { MapeamentoHomeComponent } from './components/mapeamento-home/mapeamento-home.component';
import { MapeamentoRoutingModule } from './mapeamento-routing.module';

@NgModule({
  declarations: [MapeamentoHomeComponent],
  imports: [SharedModule, MapeamentoRoutingModule]
})
export class MapeamentoModule {}
