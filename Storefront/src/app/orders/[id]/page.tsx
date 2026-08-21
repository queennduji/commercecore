"use client";

import { use, useEffect } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import { useAuth } from "@/components/auth/AuthProvider";
import { useHasMounted } from "@/hooks/useHasMounted";
import { ApiError } from "@/lib/api/client";
import { getOrder } from "@/lib/api/orders";
import { ORDER_STATUS_VARIANT } from "@/lib/utils/orderStatusVariant";
import { Badge } from "@/components/ui/badge";
import { ShipmentTracking } from "@/components/orders/ShipmentTracking";
import { ProductPrice } from "@/components/product/ProductPrice";
import { Skeleton } from "@/components/ui/skeleton";

export default function OrderDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params);
  const { accessToken, isLoading: authLoading } = useAuth();
  const hasMounted = useHasMounted();
  const router = useRouter();

  useEffect(() => {
    if (!authLoading && !accessToken) {
      router.replace(`/login?next=/orders/${id}`);
    }
  }, [authLoading, accessToken, id, router]);

  const { data: order, isLoading, error } = useQuery({
    queryKey: ["order", id],
    queryFn: () => getOrder(accessToken!, id),
    enabled: Boolean(accessToken),
  });

  if (!hasMounted || authLoading || !accessToken || isLoading) {
    return (
      <div className="mx-auto max-w-2xl px-4 py-10 sm:px-6">
        <Skeleton className="h-64 w-full" />
      </div>
    );
  }

  if (error instanceof ApiError && error.status === 404) {
    return (
      <div className="mx-auto flex max-w-md flex-col items-center gap-4 px-4 py-24 text-center">
        <h1 className="text-xl font-semibold">Order not found</h1>
        <p className="text-sm text-muted-foreground">
          This order doesn&apos;t exist, or isn&apos;t associated with your account.
        </p>
        <Link href="/products" className="text-sm font-medium text-primary hover:underline">
          Browse products
        </Link>
      </div>
    );
  }

  if (error || !order) {
    return (
      <div className="mx-auto flex max-w-md flex-col items-center gap-4 px-4 py-24 text-center">
        <h1 className="text-xl font-semibold">Something went wrong</h1>
        <p className="text-sm text-muted-foreground">Couldn&apos;t load this order. Please try again.</p>
      </div>
    );
  }

  return (
    <div className="mx-auto flex max-w-2xl flex-col gap-6 px-4 py-10 sm:px-6">
      <div className="flex items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Order #{order.id}</h1>
          <p className="text-sm text-muted-foreground">
            Placed{" "}
            {new Date(order.createdAt).toLocaleDateString(undefined, {
              year: "numeric",
              month: "short",
              day: "numeric",
            })}
          </p>
        </div>
        <Badge variant={ORDER_STATUS_VARIANT[order.status] ?? "outline"}>{order.status}</Badge>
      </div>

      <div className="flex flex-col gap-4 rounded-xl border border-border p-6">
        <h2 className="text-sm font-semibold">Items</h2>
        <ul className="flex flex-col gap-3">
          {order.items.map((item) => (
            <li key={item.productId} className="flex items-center justify-between gap-3 text-sm">
              <span className="text-muted-foreground">
                {item.name} × {item.quantity}
              </span>
              <ProductPrice price={item.lineTotal} className="font-normal" />
            </li>
          ))}
        </ul>
        <div className="flex items-center justify-between border-t border-border pt-4">
          <span className="text-sm font-medium">Subtotal</span>
          <ProductPrice price={order.subtotal} className="text-lg" />
        </div>
      </div>

      <div className="flex flex-col gap-1 rounded-xl border border-border p-6">
        <h2 className="text-sm font-semibold">Shipping address</h2>
        <p className="whitespace-pre-line text-sm text-muted-foreground">{order.shippingAddress}</p>
      </div>

      <ShipmentTracking orderId={order.id} orderStatus={order.status} accessToken={accessToken} />

      <div className="flex items-center justify-between text-sm font-medium">
        <Link href="/orders" className="text-primary hover:underline">
          View all orders
        </Link>
        <Link href="/products" className="text-primary hover:underline">
          Continue shopping
        </Link>
      </div>
    </div>
  );
}
