import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AuthGuard } from '../../core/guards/auth.guard';
import { AUTH_ROLES } from '../auth/models/auth.models';
import { PerfilFormComponent } from './components/perfil-form/perfil-form.component';
import { PerfilListComponent } from './components/perfil-list/perfil-list.component';

const routes: Routes = [
  {
    path: '',
    component: PerfilListComponent,
    canActivate: [AuthGuard],
    data: { roles: [AUTH_ROLES.administrator, AUTH_ROLES.user] }
  },
  {
    path: 'novo',
    component: PerfilFormComponent,
    canActivate: [AuthGuard],
    data: { roles: [AUTH_ROLES.administrator, AUTH_ROLES.user] }
  },
  {
    path: ':id',
    component: PerfilFormComponent,
    canActivate: [AuthGuard],
    data: { roles: [AUTH_ROLES.administrator, AUTH_ROLES.user] }
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class PerfilMapeamentoRoutingModule { }
