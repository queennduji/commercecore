import Link from "next/link";
import { Badge } from "@/components/ui/badge";
import { ProductPrice } from "@/components/product/ProductPrice";
import { ORDER_STATUS_VARIANT } from "@/lib/utils/orderStatusVariant";
import { OrderDto } from "@/types/order";

export function OrderListRow({ order }: { order: OrderDto }) {
  return (
    <Link
      href={`/orders/${order.id}`}
      className="flex items-center justify-between gap-4 rounded-xl border border-border p-4 transition-colors hover:bg-muted/50"
    >
      <div className="flex flex-col gap-1">
        <span className="text-sm font-medium">#{order.id}</span>
        <span className="text-xs text-muted-foreground">
          {new Date(order.createdAt).toLocaleDateString(undefined, {
            year: "numeric",
            month: "short",
            day: "numeric",
          })}
        </span>
      </div>
      <Badge variant={ORDER_STATUS_VARIANT[order.status] ?? "outline"}>{order.status}</Badge>
      <ProductPrice price={order.subtotal} />
    </Link>
  );
}
