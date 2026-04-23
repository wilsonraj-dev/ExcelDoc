import { Component } from '@angular/core';
import { AUTH_ROLES, AuthService } from '../../core/services/auth.service';

interface MenuItem {
  label: string;
  description: string;
  icon: string;
  route: string;
  roles: string[];
  exact?: boolean;
}

@Component({
  selector: 'app-side-menu',
  templateUrl: './side-menu.component.html',
  styleUrl: './side-menu.component.css'
})
export class SideMenuComponent {
  readonly menuItems: MenuItem[] = [
    {
      label: 'Dashboard',
      description: 'Visão geral inicial',
      icon: 'dashboard',
      route: '/dashboard',
      roles: [AUTH_ROLES.administrator, AUTH_ROLES.user],
      exact: true
    },
    {
      label: 'Empresas',
      description: 'Gerencie cadastros e configurações.',
      icon: 'domain',
      route: '/empresa',
      roles: [AUTH_ROLES.administrator, AUTH_ROLES.user]
    },
    {
      label: 'Documentos',
      description: 'Centralize o fluxo documental.',
      icon: 'description',
      route: '/documentos',
      roles: [AUTH_ROLES.administrator],
      exact: true
    },
    {
      label: 'Coleções',
      description: 'Organize conjuntos reutilizáveis.',
      icon: 'folder_copy',
      route: '/colecoes',
      roles: [AUTH_ROLES.administrator, AUTH_ROLES.user],
      exact: true
    },
    {
      label: 'Mapeamento',
      description: 'Defina regras e relacionamentos.',
      icon: 'map',
      route: '/mapeamento',
      roles: [AUTH_ROLES.administrator, AUTH_ROLES.user],
      exact: true
    },
    {
      label: 'Processamento',
      description: 'Acompanhe execuções e etapas.',
      icon: 'settings_slow_motion',
      route: '/processamento',
      roles: [AUTH_ROLES.administrator, AUTH_ROLES.user]
    }
  ];

  constructor(private readonly authService: AuthService) {}

  get isAuthenticated(): boolean {
    return this.authService.getSession() !== null;
  }

  get availableMenuItems(): MenuItem[] {
    return this.menuItems.filter((item) => this.authService.hasRequiredRoles(item.roles));
  }
}
