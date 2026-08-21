"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import { useAuth } from "@/components/auth/AuthProvider";
import { useHasMounted } from "@/hooks/useHasMounted";
import { listMyOrders } from "@/lib/api/orders";
import { OrderListRow } from "@/components/orders/OrderListRow";
import { Skeleton } from "@/components/ui/skeleton";

// See app/cart/page.tsx for why — same shape (no cookies/searchParams/dynamic segments), same
// static-shell hydration mismatch otherwise.
export const dynamic = "force-dynamic";

const PAGE_SIZE = 10;
const pagerButtonClass =
  "inline-flex h-8 items-center justify-center rounded-lg border border-border px-3 text-sm font-medium transition-colors";

export default function OrdersPage() {
  const { accessToken, isLoading: authLoading } = useAuth();
  const hasMounted = useHasMounted();
  const router = useRouter();
  const [page, setPage] = useState(1);

  useEffect(() => {
    if (!authLoading && !accessToken) {
      router.replace("/login?next=/orders");
    }
  }, [authLoading, accessToken, router]);

  const { data, isLoading } = useQuery({
    queryKey: ["orders", "me", page],
    queryFn: () => listMyOrders(accessToken!, page, PAGE_SIZE),
    enabled: Boolean(accessToken),
  });

  if (!hasMounted || authLoading || !accessToken || isLoading) {
    return (
      <div className="mx-auto max-w-2xl px-4 py-10 sm:px-6">
        <Skeleton className="h-64 w-full" />
      </div>
    );
  }

  if (!data || data.items.length === 0) {
    return (
      <div className="mx-auto flex max-w-md flex-col items-center gap-4 px-4 py-24 text-center">
        <h1 className="text-xl font-semibold">No orders yet</h1>
        <Link href="/products" className="text-sm font-medium text-primary hover:underline">
          Browse products
        </Link>
      </div>
    );
  }

  const totalPages = Math.max(1, Math.ceil(data.totalCount / PAGE_SIZE));

  return (
    <div className="mx-auto flex max-w-2xl flex-col gap-6 px-4 py-10 sm:px-6">
      <h1 className="text-2xl font-semibold tracking-tight">Your orders</h1>

      <div className="flex flex-col gap-3">
        {data.items.map((order) => (
          <OrderListRow key={order.id} order={order} />
        ))}
      </div>

      {totalPages > 1 && (
        <nav aria-label="Pagination" className="flex items-center justify-center gap-3">
          <button
            type="button"
            disabled={page <= 1}
            onClick={() => setPage((p) => p - 1)}
            className={`${pagerButtonClass} hover:bg-muted disabled:cursor-not-allowed disabled:text-muted-foreground disabled:opacity-50 disabled:hover:bg-transparent`}
          >
            Previous
          </button>
          <span className="text-sm text-muted-foreground">
            Page {page} of {totalPages}
          </span>
          <button
            type="button"
            disabled={page >= totalPages}
            onClick={() => setPage((p) => p + 1)}
            className={`${pagerButtonClass} hover:bg-muted disabled:cursor-not-allowed disabled:text-muted-foreground disabled:opacity-50 disabled:hover:bg-transparent`}
          >
            Next
          </button>
        </nav>
      )}
    </div>
  );
}
