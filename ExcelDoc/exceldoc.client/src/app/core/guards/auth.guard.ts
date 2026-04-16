import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivate, Router, UrlTree } from '@angular/router';
import { AuthService } from '../services/auth.service';

@Injectable({
  providedIn: 'root'
})
export class AuthGuard implements CanActivate {
  constructor(
    private readonly authService: AuthService,
    private readonly router: Router
  ) {}

  canActivate(route: ActivatedRouteSnapshot): boolean | UrlTree {
    const session = this.authService.getSession();

    if (!session) {
      return this.router.createUrlTree(['/login']);
    }

    const requiredRoles = (route.data['roles'] as string[] | undefined) ?? [];
    if (this.authService.hasRequiredRoles(requiredRoles, session)) {
      return true;
    }

    return this.router.createUrlTree([this.authService.getDefaultRoute(session)]);
  }
}
