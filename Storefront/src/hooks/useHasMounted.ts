"use client";

import { useSyncExternalStore } from "react";

function subscribeToNothing() {
  return () => {};
}

/**
 * `false` on the server and on the client's hydration-matching render, `true` on every render
 * after that - `useSyncExternalStore`'s distinct server/client snapshots guarantee this exact
 * split (see hooks/useCart.ts's useGuestCartId for the same pattern applied to cookies).
 *
 * Needed for /cart, /checkout, and /orders specifically: their TanStack Query calls can resolve
 * essentially synchronously (e.g. a guest with no cart cookie yet - the queryFn returns `null`
 * with no real network round trip), fast enough that the client's post-hydration state update can
 * land inside the same pass React uses to verify hydration, racing ahead of the server-rendered
 * loading-state HTML. Confirmed directly via Next's dev overlay diff on /cart (server: loading
 * skeleton; client: already-resolved "cart is empty") - `export const dynamic = "force-dynamic"`
 * on those pages did NOT fix this on its own, since it addresses response caching, not this
 * same-request rendering race. Gating data-dependent branches on `useHasMounted()` guarantees the
 * first client render matches the server's (both show the loading state) regardless of how fast
 * the query resolves.
 */
export function useHasMounted(): boolean {
  return useSyncExternalStore(
    subscribeToNothing,
    () => true,
    () => false,
  );
}
