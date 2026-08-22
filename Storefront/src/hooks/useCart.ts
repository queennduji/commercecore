"use client";

import { useSyncExternalStore } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useAuth } from "@/components/auth/AuthProvider";
import * as cartApi from "@/lib/api/cart";
import { ApiError } from "@/lib/api/client";
import { getGuestCartId, setGuestCartId } from "@/lib/cart/guestCartId";
import { CartDto } from "@/types/cart";

// Cookies don't emit change events, so there's nothing to subscribe to - a no-op unsubscribe is
// correct here since guestCartId only ever changes via this hook's own mutations, which already
// invalidate the query that reads it.
function subscribeToNothing() {
  return () => {};
}

/**
 * `useSyncExternalStore`, not a plain synchronous `getGuestCartId()` call in the render body -
 * that reads `document.cookie`, which doesn't exist during SSR. Reading it directly in render
 * makes the client's first render disagree with the server's (server always sees "no cookie",
 * client sees the real one) - a genuine hydration mismatch, confirmed directly via Next's dev
 * overlay on /cart, not just a style nit. `useSyncExternalStore` is React's own purpose-built
 * primitive for exactly this - external, non-React state with a distinct SSR-safe snapshot (its
 * third argument) - unlike a `useEffect`+`setState` workaround, which trades the same problem for
 * an extra unnecessary render pass on every mount.
 */
function useGuestCartId(): string | null {
  return useSyncExternalStore(subscribeToNothing, getGuestCartId, () => null);
}

/**
 * The one place that decides "which cart" - authenticated users' cart id is always their user
 * id (CartService's own convention, reachable via GET /me which get-or-creates it); guests carry
 * a random cart id in a cookie, created lazily on first mutation. Both branches converge on
 * `{ id, accessToken? }` so mutations don't need to duplicate this decision.
 */
export function useCart() {
  const { accessToken, userId } = useAuth();
  const queryClient = useQueryClient();

  // Called unconditionally (Rules of Hooks) even though its result is only used when signed
  // out - the ternary below just ignores it otherwise.
  const rawGuestCartId = useGuestCartId();
  const guestCartId = accessToken ? null : rawGuestCartId;

  const queryKey = accessToken ? ["cart", "me", userId] : ["cart", "guest", guestCartId ?? "none"];

  const query = useQuery<CartDto | null>({
    queryKey,
    queryFn: async () => {
      if (accessToken) {
        // GET /me get-or-creates the user's cart server-side - this is also what guarantees the
        // cart exists in Redis before any item mutation below is attempted.
        return cartApi.getMyCart(accessToken);
      }
      if (!guestCartId) return null;
      try {
        return await cartApi.getCart(guestCartId);
      } catch (error) {
        if (error instanceof ApiError && error.status === 404) return null;
        throw error;
      }
    },
  });

  async function resolveCartId(): Promise<string> {
    if (accessToken) {
      // Cheap, idempotent get-or-create - guarantees the cart exists even if this mutation
      // fires before the query above has resolved once (e.g. right after login).
      const cart = await cartApi.getMyCart(accessToken);
      return cart.id;
    }
    const existing = getGuestCartId();
    if (existing) return existing;
    const created = await cartApi.createCart();
    setGuestCartId(created.id);
    return created.id;
  }

  function invalidate() {
    return queryClient.invalidateQueries({ queryKey: ["cart"] });
  }

  const addItem = useMutation({
    mutationFn: async ({ productId, quantity }: { productId: string; quantity: number }) => {
      const cartId = await resolveCartId();
      return cartApi.addItem(cartId, productId, quantity, accessToken ?? undefined);
    },
    onSuccess: invalidate,
  });

  const updateQuantity = useMutation({
    mutationFn: async ({ productId, quantity }: { productId: string; quantity: number }) => {
      const cartId = await resolveCartId();
      // The backend rejects quantity <= 0 on the update endpoint by design - removing is a
      // distinct operation there, not "update to zero".
      return quantity > 0
        ? cartApi.updateItemQuantity(cartId, productId, quantity, accessToken ?? undefined)
        : cartApi.removeItem(cartId, productId, accessToken ?? undefined);
    },
    onSuccess: invalidate,
  });

  const removeItem = useMutation({
    mutationFn: async (productId: string) => {
      const cartId = await resolveCartId();
      return cartApi.removeItem(cartId, productId, accessToken ?? undefined);
    },
    onSuccess: invalidate,
  });

  return {
    cart: query.data ?? null,
    isLoading: query.isLoading,
    itemCount: query.data?.items.reduce((total, item) => total + item.quantity, 0) ?? 0,
    addItem,
    updateQuantity,
    removeItem,
  };
}
