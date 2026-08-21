// Non-httpOnly on purpose — a guest cart id isn't sensitive, and this is read/written from
// client components only (never needed server-side in this phase). 30 days to match
// CartService's own Redis TTL default (RedisOptions.TtlDays) — no point outliving the cart.
const COOKIE_NAME = "cc_guest_cart_id";
const MAX_AGE_SECONDS = 60 * 60 * 24 * 30;

export function getGuestCartId(): string | null {
  if (typeof document === "undefined") return null;
  const match = document.cookie.match(new RegExp(`(?:^|; )${COOKIE_NAME}=([^;]*)`));
  return match ? decodeURIComponent(match[1]) : null;
}

export function setGuestCartId(id: string): void {
  document.cookie = `${COOKIE_NAME}=${encodeURIComponent(id)}; path=/; max-age=${MAX_AGE_SECONDS}; samesite=lax`;
}

export function clearGuestCartId(): void {
  document.cookie = `${COOKIE_NAME}=; path=/; max-age=0; samesite=lax`;
}
