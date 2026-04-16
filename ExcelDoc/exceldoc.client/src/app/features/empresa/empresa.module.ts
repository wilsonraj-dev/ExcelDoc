import { NgModule } from '@angular/core';
import { SharedModule } from '../../shared/shared.module';
import { EmpresaRoutingModule } from './empresa-routing.module';
import { CompanySettingsComponent } from './components/company-settings/company-settings.component';
import { CreateCompanyComponent } from './components/create-company/create-company.component';

@NgModule({
  declarations: [CompanySettingsComponent, CreateCompanyComponent],
  imports: [SharedModule, EmpresaRoutingModule]
})
export class EmpresaModule {}
