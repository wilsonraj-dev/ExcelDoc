import { NgModule } from '@angular/core';
import { SharedModule } from '../../shared/shared.module';
import { ProcessamentoHomeComponent } from './components/processamento-home/processamento-home.component';
import { ProcessamentoRoutingModule } from './processamento-routing.module';

@NgModule({
  declarations: [ProcessamentoHomeComponent],
  imports: [SharedModule, ProcessamentoRoutingModule]
})
export class ProcessamentoModule {}
