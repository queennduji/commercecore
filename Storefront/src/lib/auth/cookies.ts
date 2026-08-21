export const REFRESH_TOKEN_COOKIE = "refresh_token";

// 7 days - matches AuthenticationService's JwtOptions.RefreshTokenDays exactly, so the cookie
// never outlives (or expires meaningfully before) the token it holds.
const REFRESH_TOKEN_MAX_AGE_SECONDS = 60 * 60 * 24 * 7;

/**
 * httpOnly so client-side JS (and any XSS) can never read the refresh token - only this BFF's
 * own Route Handlers ever see it. `secure` is skipped in development since local dev runs over
 * plain http://localhost.
 */
export function refreshTokenCookieOptions() {
  return {
    httpOnly: true,
    secure: process.env.NODE_ENV === "production",
    sameSite: "lax" as const,
    path: "/",
    maxAge: REFRESH_TOKEN_MAX_AGE_SECONDS,
  };
}
