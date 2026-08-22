"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import nextDynamic from "next/dynamic";
import { useRouter } from "next/navigation";
import { useQueryClient } from "@tanstack/react-query";
import { useAuth } from "@/components/auth/AuthProvider";
import { useCart } from "@/hooks/useCart";
import { useHasMounted } from "@/hooks/useHasMounted";
import { CheckoutOrderSummary } from "@/components/checkout/CheckoutOrderSummary";
import { ShippingAddressForm } from "@/components/checkout/ShippingAddressForm";
import { Skeleton } from "@/components/ui/skeleton";
import { OrderDto } from "@/types/order";

// PaymentForm reads NEXT_PUBLIC_STRIPE_PUBLISHABLE_KEY at render time - ssr:false keeps it out
// of any server render pass entirely, so a missing key never fails a build/SSR that has nothing
// to do with checkout. loading mirrors PaymentForm's own bordered-card shape to avoid layout
// shift while the chunk loads.
const PaymentForm = nextDynamic(() => import("@/components/checkout/PaymentForm").then((m) => m.PaymentForm), {
  ssr: false,
  loading: () => <Skeleton className="h-48 w-full rounded-xl" />,
});

// See app/cart/page.tsx for why - same shape (no cookies/searchParams/dynamic segments), same
// static-shell hydration mismatch otherwise.
export const dynamic = "force-dynamic";

export default function CheckoutPage() {
  const { accessToken, isLoading: authLoading } = useAuth();
  const { cart, isLoading: cartLoading } = useCart();
  const hasMounted = useHasMounted();
  const router = useRouter();
  const queryClient = useQueryClient();
  const [order, setOrder] = useState<OrderDto | null>(null);
  const [cancelled, setCancelled] = useState(false);

  useEffect(() => {
    if (!authLoading && !accessToken) {
      router.replace("/login?next=/checkout");
    }
  }, [authLoading, accessToken, router]);

  function handleOrderCreated(newOrder: OrderDto) {
    // The order is created only after CheckoutCommandHandler reserves stock and clears the cart
    // server-side - refetch now so the header badge and this page's own summary don't show a
    // now-stale cart if the shopper navigates back.
    queryClient.invalidateQueries({ queryKey: ["cart"] });
    setOrder(newOrder);
  }

  function handlePaid(paidOrder: OrderDto) {
    router.push(`/orders/${paidOrder.id}`);
  }

  if (!hasMounted || authLoading || !accessToken) {
    return (
      <div className="mx-auto max-w-2xl px-4 py-10 sm:px-6">
        <Skeleton className="h-64 w-full" />
      </div>
    );
  }

  if (cancelled) {
    return (
      <div className="mx-auto flex max-w-md flex-col items-center gap-4 px-4 py-24 text-center">
        <h1 className="text-xl font-semibold">Order cancelled</h1>
        <p className="text-sm text-muted-foreground">Any reserved stock has been released.</p>
        <Link href="/products" className="text-sm font-medium text-primary hover:underline">
          Browse products
        </Link>
      </div>
    );
  }

  if (order) {
    return (
      <div className="mx-auto flex max-w-2xl flex-col gap-6 px-4 py-10 sm:px-6">
        <h1 className="text-2xl font-semibold tracking-tight">Checkout</h1>
        {/* onCancelled hands back the cancelled OrderDto (order/[id]/page.tsx uses it to update
            its own cache in place) - this page shows a dedicated "cancelled" screen instead, so
            it has no use for the value. */}
        <PaymentForm order={order} onPaid={handlePaid} onCancelled={() => setCancelled(true)} />
      </div>
    );
  }

  if (cartLoading) {
    return (
      <div className="mx-auto max-w-2xl px-4 py-10 sm:px-6">
        <Skeleton className="h-64 w-full" />
      </div>
    );
  }

  if (!cart || cart.items.length === 0) {
    return (
      <div className="mx-auto flex max-w-md flex-col items-center gap-4 px-4 py-24 text-center">
        <h1 className="text-xl font-semibold">Your cart is empty</h1>
        <Link href="/products" className="text-sm font-medium text-primary hover:underline">
          Browse products
        </Link>
      </div>
    );
  }

  return (
    <div className="mx-auto grid max-w-4xl gap-8 px-4 py-10 sm:px-6 lg:grid-cols-[1fr_320px]">
      <div className="flex flex-col gap-6">
        <h1 className="text-2xl font-semibold tracking-tight">Checkout</h1>
        <ShippingAddressForm onOrderCreated={handleOrderCreated} />
      </div>
      <CheckoutOrderSummary items={cart.items} subtotal={cart.subtotal} />
    </div>
  );
}
