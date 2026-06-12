import { NgModule } from '@angular/core';
import { SharedModule } from '../../shared/shared.module';
import { UsuariosComponent } from './components/usuarios/usuarios.component';
import { UsuariosRoutingModule } from './usuarios-routing.module';

@NgModule({
  declarations: [UsuariosComponent],
  imports: [SharedModule, UsuariosRoutingModule]
})
export class UsuariosModule { }
