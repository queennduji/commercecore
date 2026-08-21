import { cookies } from "next/headers";
import { NextResponse } from "next/server";
import { env } from "@/lib/env";
import { REFRESH_TOKEN_COOKIE } from "@/lib/auth/cookies";

export async function POST() {
  const cookieStore = await cookies();
  const refreshToken = cookieStore.get(REFRESH_TOKEN_COOKIE)?.value;

  if (refreshToken) {
    // Best-effort — the cookie gets cleared regardless of whether the backend revoke succeeds,
    // since the user's intent ("log me out of this browser") is satisfied either way.
    await fetch(`${env.apiBaseUrl}/api/auth/revoke`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ refreshToken }),
    }).catch(() => undefined);
  }

  const response = new NextResponse(null, { status: 204 });
  response.cookies.delete(REFRESH_TOKEN_COOKIE);
  return response;
}
