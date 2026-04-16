import { NgModule } from '@angular/core';
import { SharedModule } from '../../shared/shared.module';
import { DocumentoFormComponent } from './components/documento-form/documento-form.component';
import { DocumentoListComponent } from './components/documento-list/documento-list.component';
import { DocumentosRoutingModule } from './documentos-routing.module';

@NgModule({
  declarations: [DocumentoListComponent, DocumentoFormComponent],
  imports: [SharedModule, DocumentosRoutingModule]
})
export class DocumentosModule { }
