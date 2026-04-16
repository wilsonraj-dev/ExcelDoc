import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { MapeamentoHomeComponent } from './components/mapeamento-home/mapeamento-home.component';

const routes: Routes = [
  { path: '', component: MapeamentoHomeComponent }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class MapeamentoRoutingModule {}
