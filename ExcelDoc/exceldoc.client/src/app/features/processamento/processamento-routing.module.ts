import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ProcessamentoDetalheComponent } from './components/processamento-detalhe/processamento-detalhe.component';
import { ProcessamentoListComponent } from './components/processamento-list/processamento-list.component';
import { ProcessamentoUploadComponent } from './components/processamento-upload/processamento-upload.component';

const routes: Routes = [
  { path: '', component: ProcessamentoListComponent },
  { path: 'upload', component: ProcessamentoUploadComponent },
  { path: ':id', component: ProcessamentoDetalheComponent }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class ProcessamentoRoutingModule {}
