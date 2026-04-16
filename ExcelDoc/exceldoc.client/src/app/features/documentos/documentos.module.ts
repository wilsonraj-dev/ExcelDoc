import { NgModule } from '@angular/core';
import { SharedModule } from '../../shared/shared.module';
import { DocumentosHomeComponent } from './components/documentos-home/documentos-home.component';
import { DocumentosRoutingModule } from './documentos-routing.module';

@NgModule({
  declarations: [DocumentosHomeComponent],
  imports: [SharedModule, DocumentosRoutingModule]
})
export class DocumentosModule {}
