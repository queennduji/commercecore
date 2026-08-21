/** Access token claims Storefront cares about — see AuthenticationService's TokenService. */
interface AccessTokenClaims {
  sub: string;
  email: string;
  exp: number;
  [key: string]: unknown;
}

/**
 * Decodes a JWT's payload client-side WITHOUT verifying the signature — purely for reading
 * `email`/`sub` to display "logged in as ...". Never use this for anything security-sensitive;
 * the signature is only ever checked server-side (by each backend service).
 */
export function decodeJwtPayload(token: string): AccessTokenClaims | null {
  const parts = token.split(".");
  if (parts.length !== 3) {
    return null;
  }

  try {
    const base64 = parts[1].replace(/-/g, "+").replace(/_/g, "/");
    const padded = base64.padEnd(base64.length + ((4 - (base64.length % 4)) % 4), "=");
    const json = atob(padded);
    return JSON.parse(json) as AccessTokenClaims;
  } catch {
    return null;
  }
}
