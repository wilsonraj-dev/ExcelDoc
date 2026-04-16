import { NgModule } from '@angular/core';
import { SharedModule } from '../../shared/shared.module';
import { DashboardHomeComponent } from './components/dashboard-home/dashboard-home.component';
import { DashboardRoutingModule } from './dashboard-routing.module';

@NgModule({
  declarations: [DashboardHomeComponent],
  imports: [SharedModule, DashboardRoutingModule]
})
export class DashboardModule {}
