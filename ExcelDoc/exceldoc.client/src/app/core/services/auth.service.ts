import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, tap } from 'rxjs';
import {
  AUTH_ROLES,
  ForgotPasswordRequest,
  LoginRequest,
  LoginResponse,
  MessageResponse,
  RegisterRequest,
  RegisterResponse,
  ResetPasswordRequest
} from '../../features/auth/models/auth.models';

export {
  AUTH_ROLES,
  type ForgotPasswordRequest,
  type LoginRequest,
  type LoginResponse,
  type MessageResponse,
  type RegisterRequest,
  type RegisterResponse,
  type ResetPasswordRequest
} from '../../features/auth/models/auth.models';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly apiUrl = '/api/auth';
  private readonly tokenStorageKey = 'exceldoc.jwt';
  private readonly sessionStorageKey = 'exceldoc.session';

  constructor(private readonly http: HttpClient) {}

  login(request: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.apiUrl}/login`, request).pipe(
      tap((response: LoginResponse) => {
        localStorage.setItem(this.tokenStorageKey, response.token);
        localStorage.setItem(this.sessionStorageKey, JSON.stringify(response));
      })
    );
  }

  register(request: RegisterRequest): Observable<RegisterResponse> {
    return this.http.post<RegisterResponse>(`${this.apiUrl}/register`, request);
  }

  forgotPassword(request: ForgotPasswordRequest): Observable<MessageResponse> {
    return this.http.post<MessageResponse>(`${this.apiUrl}/forgot-password`, request);
  }

  resetPassword(request: ResetPasswordRequest): Observable<MessageResponse> {
    return this.http.post<MessageResponse>(`${this.apiUrl}/reset-password`, request);
  }

  logout(): void {
    localStorage.removeItem(this.tokenStorageKey);
    localStorage.removeItem(this.sessionStorageKey);
  }

  getToken(): string | null {
    return localStorage.getItem(this.tokenStorageKey);
  }

  getSession(): LoginResponse | null {
    const rawValue = localStorage.getItem(this.sessionStorageKey);

    if (!rawValue) {
      return null;
    }

    try {
      return JSON.parse(rawValue) as LoginResponse;
    } catch {
      this.logout();
      return null;
    }
  }

  isAdministrator(session: LoginResponse | null = this.getSession()): boolean {
    return session?.tipoUsuario === AUTH_ROLES.administrator;
  }

  hasRequiredRoles(requiredRoles: readonly string[] = [], session: LoginResponse | null = this.getSession()): boolean {
    if (!session) {
      return false;
    }

    if (!requiredRoles.length) {
      return true;
    }

    return requiredRoles.includes(session.tipoUsuario);
  }

  getDefaultRoute(session: LoginResponse | null = this.getSession()): string {
    if (this.hasRequiredRoles([AUTH_ROLES.administrator, AUTH_ROLES.user], session)) {
      return '/dashboard';
    }

    return '/login';
  }
}
