import { Component } from '@angular/core';
import { AUTH_ROLES, AuthService } from '../../core/services/auth.service';

interface MenuItem {
  labelKey: string;
  descriptionKey: string;
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
      labelKey: 'layout.sideMenu.items.dashboard.label',
      descriptionKey: 'layout.sideMenu.items.dashboard.description',
      icon: 'dashboard',
      route: '/dashboard',
      roles: [AUTH_ROLES.administrator, AUTH_ROLES.user],
      exact: true
    },
    {
      labelKey: 'layout.sideMenu.items.companies.label',
      descriptionKey: 'layout.sideMenu.items.companies.description',
      icon: 'domain',
      route: '/empresa',
      roles: [AUTH_ROLES.administrator, AUTH_ROLES.user]
    },
    {
      labelKey: 'layout.sideMenu.items.users.label',
      descriptionKey: 'layout.sideMenu.items.users.description',
      icon: 'group',
      route: '/usuarios',
      roles: [AUTH_ROLES.administrator],
      exact: true
    },
    {
      labelKey: 'layout.sideMenu.items.documents.label',
      descriptionKey: 'layout.sideMenu.items.documents.description',
      icon: 'description',
      route: '/documentos',
      roles: [AUTH_ROLES.administrator],
      exact: true
    },
    {
      labelKey: 'layout.sideMenu.items.collections.label',
      descriptionKey: 'layout.sideMenu.items.collections.description',
      icon: 'folder_copy',
      route: '/colecoes',
      roles: [AUTH_ROLES.administrator, AUTH_ROLES.user],
      exact: true
    },
    {
      labelKey: 'layout.sideMenu.items.mappingProfile.label',
      descriptionKey: 'layout.sideMenu.items.mappingProfile.description',
      icon: 'layers',
      route: '/perfil-mapeamento',
      roles: [AUTH_ROLES.administrator, AUTH_ROLES.user],
      exact: true
    },
    {
      labelKey: 'layout.sideMenu.items.processing.label',
      descriptionKey: 'layout.sideMenu.items.processing.description',
      icon: 'settings_slow_motion',
      route: '/processamento',
      roles: [AUTH_ROLES.administrator, AUTH_ROLES.user]
    }
  ];

  constructor(private readonly authService: AuthService) { }

  get isAuthenticated(): boolean {
    return this.authService.getSession() !== null;
  }

  get availableMenuItems(): MenuItem[] {
    return this.menuItems.filter((item) => this.authService.hasRequiredRoles(item.roles));
  }
}
