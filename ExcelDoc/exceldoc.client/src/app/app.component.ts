import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService, LoginResponse } from './auth/auth.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent {
  title = 'ExcelDoc';

  constructor(
    private readonly authService: AuthService,
    private readonly router: Router
  ) {}

  get session(): LoginResponse | null {
    return this.authService.getSession();
  }

  get isAuthenticated(): boolean {
    return this.session !== null;
  }

  get companyLabel(): string {
    const session = this.session;

    if (!session) {
      return '';
    }

    return session.nomeEmpresa?.trim() || (this.authService.isAdministrator(session) ? 'Todas as empresas' : 'Não vinculada');
  }

  logout(): void {
    this.authService.logout();
    void this.router.navigate(['/login']);
  }
}
