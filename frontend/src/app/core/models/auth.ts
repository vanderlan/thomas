export interface LoginRequest {
  email: string;
  password: string;
}

export interface RefreshTokenRequest {
  token: string;
  refreshToken: string;
}

/** Tokens returned by the Auth endpoints. */
export interface AuthTokens {
  token: string;
  refreshToken: string;
  expiration: string;
}

export interface AuthUser {
  id: string;
  name: string;
  email: string;
  role: string;
}

export const ROLE_ADMIN = 'admin';
