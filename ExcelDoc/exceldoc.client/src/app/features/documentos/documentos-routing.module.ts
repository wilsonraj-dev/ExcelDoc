import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { DocumentosHomeComponent } from './components/documentos-home/documentos-home.component';

const routes: Routes = [
  { path: '', component: DocumentosHomeComponent }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class DocumentosRoutingModule {}
