import { AuthUser } from '../models/auth';

const CLAIMS = {
  id: ['sid', 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/sid'],
  name: ['given_name', 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname'],
  email: ['email', 'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'],
  role: ['role', 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'],
} as const;

/** Decodes the payload of a JWT without validating it (the API is the one that validates tokens). */
export function decodeJwtPayload(token: string): Record<string, unknown> | null {
  const parts = token.split('.');

  if (parts.length < 2) {
    return null;
  }

  try {
    const base64 = parts[1].replace(/-/g, '+').replace(/_/g, '/');
    const padded = base64 + '='.repeat((4 - (base64.length % 4)) % 4);
    const json = decodeURIComponent(
      Array.from(atob(padded), (c) => '%' + c.charCodeAt(0).toString(16).padStart(2, '0')).join(''),
    );

    return JSON.parse(json) as Record<string, unknown>;
  } catch {
    return null;
  }
}

function firstClaim(payload: Record<string, unknown>, keys: readonly string[]): string {
  for (const key of keys) {
    const value = payload[key];

    if (Array.isArray(value)) {
      return String(value[0] ?? '');
    }

    if (value !== undefined && value !== null) {
      return String(value);
    }
  }

  return '';
}

export function userFromJwt(token: string): AuthUser | null {
  const payload = decodeJwtPayload(token);

  if (!payload) {
    return null;
  }

  return {
    id: firstClaim(payload, CLAIMS.id),
    name: firstClaim(payload, CLAIMS.name),
    email: firstClaim(payload, CLAIMS.email),
    role: firstClaim(payload, CLAIMS.role).toLowerCase(),
  };
}

/** Returns true when the "exp" claim of the token is in the past. */
export function isJwtExpired(token: string, now: number = Date.now()): boolean {
  const payload = decodeJwtPayload(token);
  const exp = payload?.['exp'];

  if (typeof exp !== 'number') {
    return false;
  }

  return exp * 1000 <= now;
}
