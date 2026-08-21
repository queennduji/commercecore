import { Session } from "@/types/auth";

// Calls to Storefront's OWN Route Handlers (app/api/auth/*), not the ApiGateway - these are the
// BFF layer that keeps the refresh token in an httpOnly cookie the client never sees. Relative
// paths (not API_BASE_URL) since this always runs same-origin, browser-only.

async function parseSessionOrThrow(response: Response): Promise<Session> {
  if (!response.ok) {
    const body = await response.json().catch(() => undefined);
    throw new Error(body?.error ?? "Request failed");
  }
  return response.json() as Promise<Session>;
}

export async function login(email: string, password: string): Promise<Session> {
  const response = await fetch("/api/auth/login", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email, password }),
  });
  return parseSessionOrThrow(response);
}

export async function register(email: string, password: string, phoneNumber?: string): Promise<Session> {
  const response = await fetch("/api/auth/register", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email, password, phoneNumber: phoneNumber || undefined }),
  });
  return parseSessionOrThrow(response);
}

/** Returns null (rather than throwing) on a missing/expired session - this is the "silent" part. */
export async function refresh(): Promise<Session | null> {
  const response = await fetch("/api/auth/refresh", { method: "POST" });
  if (!response.ok) return null;
  return response.json() as Promise<Session>;
}

export async function logout(): Promise<void> {
  await fetch("/api/auth/logout", { method: "POST" });
}
