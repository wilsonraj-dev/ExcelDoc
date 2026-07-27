export const AUTH_ROLES = {
  administrator: 'Administrador',
  user: 'Usuario'
} as const;

export interface SapBase {
  database: string;
  description: string;
}

export interface LoginRequest {
  database: string;
  login: string;
  senha: string;
}

export interface LoginResponse {
  token: string;
  expiresAtUtc: string;
  nomeUsuario: string;
  tipoUsuario: string;
  database: string;
  idioma?: string | null;
}
