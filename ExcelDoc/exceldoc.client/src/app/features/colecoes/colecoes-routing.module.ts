import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AuthGuard } from '../../core/guards/auth.guard';
import { AUTH_ROLES } from '../auth/models/auth.models';
import { ColecaoFormComponent } from './components/colecao-form/colecao-form.component';
import { ColecaoListComponent } from './components/colecao-list/colecao-list.component';

const routes: Routes = [
  {
    path: '',
    component: ColecaoListComponent,
    canActivate: [AuthGuard],
    data: { roles: [AUTH_ROLES.administrator, AUTH_ROLES.user] }
  },
  {
    path: 'nova',
    component: ColecaoFormComponent,
    canActivate: [AuthGuard],
    data: { roles: [AUTH_ROLES.administrator, AUTH_ROLES.user] }
  },
  {
    path: ':id',
    component: ColecaoFormComponent,
    canActivate: [AuthGuard],
    data: { roles: [AUTH_ROLES.administrator, AUTH_ROLES.user] }
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class ColecoesRoutingModule {}
