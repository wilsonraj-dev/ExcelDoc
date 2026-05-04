import { NgModule } from '@angular/core';
import { SharedModule } from '../../shared/shared.module';
import { PerfilFormComponent } from './components/perfil-form/perfil-form.component';
import { PerfilListComponent } from './components/perfil-list/perfil-list.component';
import { PerfilMapeamentoRoutingModule } from './perfil-mapeamento-routing.module';

@NgModule({
  declarations: [
    PerfilListComponent,
    PerfilFormComponent
  ],
  imports: [
    SharedModule,
    PerfilMapeamentoRoutingModule
  ]
})
export class PerfilMapeamentoModule {}
