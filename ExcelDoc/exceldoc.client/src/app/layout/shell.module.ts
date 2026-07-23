import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { RouterModule } from '@angular/router';
import { LanguageSelectorComponent } from '../shared/components/language-selector/language-selector.component';
import { TranslatePipe } from '../shared/pipes/translate.pipe';
import { SideMenuComponent } from './side-menu/side-menu.component';

/**
 * Dependencias estritamente necessarias para o shell da aplicacao.
 *
 * O SharedModule contem componentes e modulos Material usados pelas telas
 * funcionais. Importa-lo no AppModule fazia com que todo esse codigo entrasse
 * no bundle inicial, mesmo com as funcionalidades carregadas sob demanda.
 */
@NgModule({
  declarations: [LanguageSelectorComponent, SideMenuComponent, TranslatePipe],
  imports: [
    CommonModule,
    RouterModule,
    MatButtonModule,
    MatIconModule,
    MatListModule,
    MatToolbarModule,
    MatTooltipModule
  ],
  exports: [
    MatButtonModule,
    MatToolbarModule,
    LanguageSelectorComponent,
    SideMenuComponent,
    TranslatePipe
  ]
})
export class ShellModule {}
