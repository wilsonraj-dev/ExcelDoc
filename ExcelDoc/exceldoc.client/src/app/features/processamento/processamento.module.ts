import { NgModule } from '@angular/core';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { SharedModule } from '../../shared/shared.module';
import { JsonDialogComponent } from './components/json-dialog/json-dialog.component';
import { ProcessamentoDetalheComponent } from './components/processamento-detalhe/processamento-detalhe.component';
import { ProcessamentoHomeComponent } from './components/processamento-home/processamento-home.component';
import { ProcessamentoListComponent } from './components/processamento-list/processamento-list.component';
import { ProcessamentoUploadComponent } from './components/processamento-upload/processamento-upload.component';
import { ProcessamentoRoutingModule } from './processamento-routing.module';

@NgModule({
  declarations: [
    ProcessamentoHomeComponent,
    ProcessamentoUploadComponent,
    ProcessamentoListComponent,
    ProcessamentoDetalheComponent,
    JsonDialogComponent
  ],
  imports: [
    SharedModule,
    ProcessamentoRoutingModule,
    MatProgressBarModule
  ]
})
export class ProcessamentoModule {}
