// AuthenticationService's AuthTokens record - returned by both the gateway's
// /api/auth/{login,register,refresh} and, in a trimmed form, by Storefront's own
// /api/auth/* BFF routes (which strip refreshToken before handing this to the client).
export interface AuthTokens {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAt: string;
}

/** What Storefront's BFF routes return to the client - refreshToken never leaves the server. */
export interface Session {
  accessToken: string;
  accessTokenExpiresAt: string;
}
