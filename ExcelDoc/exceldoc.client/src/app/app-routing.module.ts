import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AuthGuard } from './core/guards/auth.guard';
import { AUTH_ROLES } from './core/services/auth.service';
import { CreateUserComponent } from './features/auth/create-user/create-user.component';
import { ForgotPasswordComponent } from './features/auth/forgot-password/forgot-password.component';
import { LoginComponent } from './features/auth/login/login.component';
import { CompanySettingsComponent } from './features/company-settings/company-settings.component';
import { CreateCompanyComponent } from './features/create-company/create-company.component';

const routes: Routes = [
  { path: '', redirectTo: 'login', pathMatch: 'full' },
  { path: 'login', component: LoginComponent },
  { path: 'criar-usuario', component: CreateUserComponent },
  { path: 'esqueci-a-senha', component: ForgotPasswordComponent },
  {
    path: 'criar-empresa',
    component: CreateCompanyComponent,
    canActivate: [AuthGuard],
    data: { roles: [AUTH_ROLES.administrator] }
  },
  {
    path: 'configuracoes-empresa',
    component: CompanySettingsComponent,
    canActivate: [AuthGuard],
    data: { roles: [AUTH_ROLES.administrator, AUTH_ROLES.user] }
  },
  { path: '**', redirectTo: 'login' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
