import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ColecoesHomeComponent } from './components/colecoes-home/colecoes-home.component';

const routes: Routes = [
  { path: '', component: ColecoesHomeComponent }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class ColecoesRoutingModule {}
