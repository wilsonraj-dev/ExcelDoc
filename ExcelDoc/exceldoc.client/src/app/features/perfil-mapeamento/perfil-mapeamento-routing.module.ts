import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

const routes: Routes = [
  {
    path: '',
    redirectTo: '/mapeamento',
    pathMatch: 'full'
  },
  {
    path: '**',
    redirectTo: '/mapeamento'
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class PerfilMapeamentoRoutingModule { }
