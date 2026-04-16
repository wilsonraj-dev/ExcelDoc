import { Component } from '@angular/core';

@Component({
  selector: 'app-dashboard-home',
  templateUrl: './dashboard-home.component.html',
  styleUrl: './dashboard-home.component.css'
})
export class DashboardHomeComponent {
  readonly cards = [
    {
      title: 'Documentos',
      description: 'Acompanhe rapidamente o status dos documentos processados.'
    },
    {
      title: 'Coleções',
      description: 'Centralize os grupos de dados reutilizáveis da aplicação.'
    },
    {
      title: 'Processamento',
      description: 'Monitore as etapas principais do fluxo operacional.'
    }
  ];
}
