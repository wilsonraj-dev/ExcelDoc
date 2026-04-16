import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { CompanySettingsComponent } from './components/company-settings/company-settings.component';
import { CreateCompanyComponent } from './components/create-company/create-company.component';
import { AuthGuard } from '../../core/guards/auth.guard';
import { AUTH_ROLES } from '../auth/models/auth.models';

const routes: Routes = [
  { path: '', redirectTo: 'configuracoes', pathMatch: 'full' },
  { path: 'configuracoes', component: CompanySettingsComponent },
  {
    path: 'criar',
    component: CreateCompanyComponent,
    canActivate: [AuthGuard],
    data: { roles: [AUTH_ROLES.administrator] }
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class EmpresaRoutingModule {}
