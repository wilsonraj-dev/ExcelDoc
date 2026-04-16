import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AuthGuard } from './core/guards/auth.guard';
import { AUTH_ROLES } from './features/auth/models/auth.models';

const routes: Routes = [
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
  {
    path: 'login',
    loadChildren: () => import('./features/auth/auth.module').then((module) => module.AuthModule)
  },
  { path: 'criar-usuario', redirectTo: 'login/criar-usuario', pathMatch: 'full' },
  { path: 'esqueci-a-senha', redirectTo: 'login/esqueci-a-senha', pathMatch: 'full' },
  {
    path: 'dashboard',
    loadChildren: () => import('./features/dashboard/dashboard.module').then((module) => module.DashboardModule),
    canActivate: [AuthGuard],
    data: { roles: [AUTH_ROLES.administrator, AUTH_ROLES.user] }
  },
  {
    path: 'empresa',
    loadChildren: () => import('./features/empresa/empresa.module').then((module) => module.EmpresaModule),
    canActivate: [AuthGuard],
    data: { roles: [AUTH_ROLES.administrator, AUTH_ROLES.user] }
  },
  {
    path: 'documentos',
    loadChildren: () => import('./features/documentos/documentos.module').then((module) => module.DocumentosModule),
    canActivate: [AuthGuard],
    data: { roles: [AUTH_ROLES.administrator, AUTH_ROLES.user] }
  },
  {
    path: 'colecoes',
    loadChildren: () => import('./features/colecoes/colecoes.module').then((module) => module.ColecoesModule),
    canActivate: [AuthGuard],
    data: { roles: [AUTH_ROLES.administrator, AUTH_ROLES.user] }
  },
  {
    path: 'mapeamento',
    loadChildren: () => import('./features/mapeamento/mapeamento.module').then((module) => module.MapeamentoModule),
    canActivate: [AuthGuard],
    data: { roles: [AUTH_ROLES.administrator, AUTH_ROLES.user] }
  },
  {
    path: 'processamento',
    loadChildren: () => import('./features/processamento/processamento.module').then((module) => module.ProcessamentoModule),
    canActivate: [AuthGuard],
    data: { roles: [AUTH_ROLES.administrator, AUTH_ROLES.user] }
  },
  { path: 'criar-empresa', redirectTo: 'empresa/criar', pathMatch: 'full' },
  { path: 'configuracoes-empresa', redirectTo: 'empresa/configuracoes', pathMatch: 'full' },
  { path: '**', redirectTo: 'login' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
