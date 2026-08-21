"use client";

import Link from "next/link";
import { useCart } from "@/hooks/useCart";
import { useHasMounted } from "@/hooks/useHasMounted";
import { CartItemRow } from "@/components/cart/CartItemRow";
import { CartSummary } from "@/components/cart/CartSummary";
import { Skeleton } from "@/components/ui/skeleton";

// Without this, Next treats this route as static-eligible (no cookies/searchParams/dynamic
// segments) and caches its server-rendered shell - always frozen at whatever loading/empty state
// happened to render first. The real client-side cart state (via useCart()) then resolves against
// that stale shell instead of a fresh one, producing a genuine hydration mismatch (observed
// directly via Next's dev overlay: server showed the loading skeleton, client showed "cart is
// empty"). /checkout and /orders have the same shape and need the same fix.
export const dynamic = "force-dynamic";

export default function CartPage() {
  const { cart, isLoading } = useCart();
  const hasMounted = useHasMounted();

  return (
    <div className="mx-auto flex max-w-3xl flex-col gap-6 px-4 py-10 sm:px-6">
      <h1 className="text-2xl font-semibold tracking-tight">Your cart</h1>

      {!hasMounted || isLoading ? (
        <div className="flex flex-col gap-3">
          <Skeleton className="h-20 w-full" />
          <Skeleton className="h-20 w-full" />
        </div>
      ) : !cart || cart.items.length === 0 ? (
        <div className="flex flex-col items-center gap-3 py-16 text-center">
          <p className="text-muted-foreground">Your cart is empty.</p>
          <Link href="/products" className="text-sm font-medium text-primary hover:underline">
            Browse products
          </Link>
        </div>
      ) : (
        <div className="grid gap-6 lg:grid-cols-[1fr_280px]">
          <div className="rounded-xl border border-border px-6">
            {cart.items.map((item) => (
              <CartItemRow key={item.productId} item={item} />
            ))}
          </div>
          <CartSummary subtotal={cart.subtotal} />
        </div>
      )}
    </div>
  );
}
