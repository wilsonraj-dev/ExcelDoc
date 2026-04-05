import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, tap } from 'rxjs';

export interface LoginRequest {
  login: string;
  senha: string;
}

export interface LoginResponse {
  token: string;
  expiresAtUtc: string;
  usuarioId: number;
  nomeUsuario: string;
  tipoUsuario: string;
  empresaId?: number | null;
}

export interface RegisterRequest {
  nomeUsuario: string;
  email: string;
  senha: string;
}

export interface RegisterResponse {
  usuarioId: number;
  nomeUsuario: string;
  email: string;
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ResetPasswordRequest {
  email: string;
  codigo: string;
  novaSenha: string;
}

export interface MessageResponse {
  message: string;
}

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
}
