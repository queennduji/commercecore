"use client";

import Link from "next/link";
import { ShoppingCart } from "lucide-react";
import { useCart } from "@/hooks/useCart";
import { useHasMounted } from "@/hooks/useHasMounted";

export function HeaderCartBadge() {
  const { itemCount } = useCart();
  const hasMounted = useHasMounted();

  return (
    <Link href="/cart" className="relative inline-flex items-center" aria-label="Cart">
      <ShoppingCart className="size-5" />
      {/* Gated on hasMounted, not just itemCount > 0 - this renders on every page, and the cart
          query can resolve fast enough to race hydration itself (see hooks/useHasMounted.ts). */}
      {hasMounted && itemCount > 0 && (
        <span className="absolute -top-2 -right-2 flex size-4 items-center justify-center rounded-full bg-primary text-[10px] font-medium text-primary-foreground">
          {itemCount > 9 ? "9+" : itemCount}
        </span>
      )}
    </Link>
  );
}
