/**
 * `next` comes straight from a URL query param, so it's attacker-influenceable (a crafted
 * `/login?next=...` link) — only ever redirect to a same-site relative path. Rejects protocol-
 * relative URLs (`//evil.com`) too, not just absolute ones, since browsers treat those as
 * external as well.
 */
export function isSafeRedirect(next: string | undefined): next is string {
  return Boolean(next) && next!.startsWith("/") && !next!.startsWith("//");
}
