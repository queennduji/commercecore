import { cookies } from "next/headers";
import { NextResponse } from "next/server";
import { env } from "@/lib/env";
import { REFRESH_TOKEN_COOKIE, refreshTokenCookieOptions } from "@/lib/auth/cookies";
import { AuthTokens } from "@/types/auth";

export async function POST() {
  const cookieStore = await cookies();
  const refreshToken = cookieStore.get(REFRESH_TOKEN_COOKIE)?.value;

  if (!refreshToken) {
    return NextResponse.json({ error: "No session." }, { status: 401 });
  }

  const upstream = await fetch(`${env.apiBaseUrl}/api/auth/refresh`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ refreshToken }),
  });

  if (!upstream.ok) {
    const response = NextResponse.json({ error: "Session expired." }, { status: 401 });
    response.cookies.delete(REFRESH_TOKEN_COOKIE);
    return response;
  }

  // Refresh ROTATES the token server-side (RefreshTokenCommandHandler revokes the old one and
  // issues a new one) — the cookie must be overwritten every time, never left as-is, or the next
  // refresh attempt would present an already-revoked token and fail.
  const tokens = (await upstream.json()) as AuthTokens;

  const response = NextResponse.json({
    accessToken: tokens.accessToken,
    accessTokenExpiresAt: tokens.accessTokenExpiresAt,
  });
  response.cookies.set(REFRESH_TOKEN_COOKIE, tokens.refreshToken, refreshTokenCookieOptions());
  return response;
}
