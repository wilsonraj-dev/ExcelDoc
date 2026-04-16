import { Component } from '@angular/core';
import { AUTH_ROLES, AuthService } from '../services/auth.service';

interface MenuItem {
  label: string;
  description: string;
  icon: string;
  route: string;
  roles: string[];
}

@Component({
  selector: 'app-side-menu',
  templateUrl: './side-menu.component.html',
  styleUrl: './side-menu.component.css'
})
export class SideMenuComponent {
  readonly menuItems: MenuItem[] = [
    {
      label: 'Empresas',
      description: '',
      icon: 'domain',
      route: '/configuracoes-empresa',
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
