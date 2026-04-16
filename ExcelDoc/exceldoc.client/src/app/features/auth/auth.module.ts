import { NgModule } from '@angular/core';
import { SharedModule } from '../../shared/shared.module';
import { AuthRoutingModule } from './auth-routing.module';
import { CreateUserComponent } from './components/create-user/create-user.component';
import { ForgotPasswordComponent } from './components/forgot-password/forgot-password.component';
import { LoginComponent } from './components/login/login.component';

@NgModule({
  declarations: [LoginComponent, CreateUserComponent, ForgotPasswordComponent],
  imports: [SharedModule, AuthRoutingModule]
})
export class AuthModule {}
