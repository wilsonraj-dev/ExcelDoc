export const AUTH_ROLES = {
  administrator: 'Administrador',
  user: 'Usuario'
} as const;

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
  nomeEmpresa?: string | null;
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
