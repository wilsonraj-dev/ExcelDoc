import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AuthGuard } from '../../core/guards/auth.guard';
import { AUTH_ROLES } from '../auth/models/auth.models';
import { DocumentoFormComponent } from './components/documento-form/documento-form.component';
import { DocumentoListComponent } from './components/documento-list/documento-list.component';

const routes: Routes = [
  {
    path: '',
    component: DocumentoListComponent,
    canActivate: [AuthGuard],
    data: { roles: [AUTH_ROLES.administrator] }
  },
  {
    path: 'novo',
    component: DocumentoFormComponent,
    canActivate: [AuthGuard],
    data: { roles: [AUTH_ROLES.administrator] }
  },
  {
    path: ':id',
    component: DocumentoFormComponent,
    canActivate: [AuthGuard],
    data: { roles: [AUTH_ROLES.administrator] }
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class DocumentosRoutingModule { }
