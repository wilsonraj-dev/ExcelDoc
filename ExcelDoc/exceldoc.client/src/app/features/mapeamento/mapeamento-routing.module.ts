import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AuthGuard } from '../../core/guards/auth.guard';
import { AUTH_ROLES } from '../auth/models/auth.models';
import { MapeamentoEditorComponent } from './components/mapeamento-editor/mapeamento-editor.component';
import { MapeamentoListComponent } from './components/mapeamento-list/mapeamento-list.component';

const routes: Routes = [
  {
    path: '',
    component: MapeamentoListComponent,
    canActivate: [AuthGuard],
    data: { roles: [AUTH_ROLES.administrator, AUTH_ROLES.user] }
  },
  {
    path: ':colecaoId',
    component: MapeamentoEditorComponent,
    canActivate: [AuthGuard],
    data: { roles: [AUTH_ROLES.administrator, AUTH_ROLES.user] }
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class MapeamentoRoutingModule { }
