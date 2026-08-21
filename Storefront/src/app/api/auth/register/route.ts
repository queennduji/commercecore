import { NextResponse } from "next/server";
import { env } from "@/lib/env";
import { REFRESH_TOKEN_COOKIE, refreshTokenCookieOptions } from "@/lib/auth/cookies";
import { AuthTokens } from "@/types/auth";

export async function POST(request: Request) {
  const { email, password, phoneNumber } = await request.json();

  const upstream = await fetch(`${env.apiBaseUrl}/api/auth/register`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email, password, phoneNumber }),
  });

  if (!upstream.ok) {
    const problem = await upstream.json().catch(() => undefined);
    const message = problem?.errors?.[0] ?? "Could not create an account.";
    return NextResponse.json({ error: message }, { status: upstream.status });
  }

  const tokens = (await upstream.json()) as AuthTokens;

  const response = NextResponse.json({
    accessToken: tokens.accessToken,
    accessTokenExpiresAt: tokens.accessTokenExpiresAt,
  });
  response.cookies.set(REFRESH_TOKEN_COOKIE, tokens.refreshToken, refreshTokenCookieOptions());
  return response;
}
