import { decodeJwtPayload, isJwtExpired, userFromJwt } from './jwt';

function createJwt(payload: Record<string, unknown>): string {
  const encode = (value: unknown) =>
    btoa(JSON.stringify(value)).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');

  return `${encode({ alg: 'HS512', typ: 'JWT' })}.${encode(payload)}.signature`;
}

describe('jwt', () => {
  it('decodes the payload of a token', () => {
    const token = createJwt({ sub: '123', exp: 1700000000 });

    expect(decodeJwtPayload(token)).toEqual({ sub: '123', exp: 1700000000 });
  });

  it('returns null for malformed tokens', () => {
    expect(decodeJwtPayload('not-a-jwt')).toBeNull();
    expect(decodeJwtPayload('a.%%%.c')).toBeNull();
  });

  it('maps the short claim names used by the API', () => {
    const token = createJwt({
      sid: 'user-id',
      given_name: 'Thomas Admin User',
      email: 'adm@thomas.com',
      role: 'Admin',
    });

    expect(userFromJwt(token)).toEqual({
      id: 'user-id',
      name: 'Thomas Admin User',
      email: 'adm@thomas.com',
      role: 'admin',
    });
  });

  it('maps the long (XML schema) claim names as well', () => {
    const token = createJwt({
      'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/sid': 'user-id',
      'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname': 'Name',
      'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress': 'a@b.com',
      'http://schemas.microsoft.com/ws/2008/06/identity/claims/role': ['operator'],
    });

    expect(userFromJwt(token)).toEqual({
      id: 'user-id',
      name: 'Name',
      email: 'a@b.com',
      role: 'operator',
    });
  });

  it('detects expired tokens', () => {
    const now = 1_700_000_000_000;

    expect(isJwtExpired(createJwt({ exp: 1_699_999_999 }), now)).toBe(true);
    expect(isJwtExpired(createJwt({ exp: 1_700_000_001 }), now)).toBe(false);
    expect(isJwtExpired(createJwt({}), now)).toBe(false);
  });
});
