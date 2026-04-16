import { HttpErrorResponse, HttpEvent, HttpHandler, HttpInterceptor, HttpRequest } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, catchError, throwError } from 'rxjs';
import { NotificationService } from '../services/notification.service';
import { AuthService } from '../services/auth.service';

@Injectable()
export class ErrorInterceptor implements HttpInterceptor {
  constructor(
    private readonly authService: AuthService,
    private readonly notificationService: NotificationService,
    private readonly router: Router
  ) {}

  intercept(request: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    return next.handle(request).pipe(
      catchError((error: HttpErrorResponse) => {
        if (error.status === 401 && this.authService.getSession()) {
          this.authService.logout();
          this.notificationService.showInfo('Sua sessão expirou. Faça login novamente.');
          void this.router.navigate(['/login']);
        }

        if (error.status === 500) {
          this.notificationService.showError('Ocorreu um erro interno. Tente novamente em instantes.');
        }

        return throwError(() => error);
      })
    );
  }
}
