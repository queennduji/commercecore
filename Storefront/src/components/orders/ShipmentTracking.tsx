"use client";

import { useQuery } from "@tanstack/react-query";
import { ApiError } from "@/lib/api/client";
import { getShipmentByOrder } from "@/lib/api/shipments";
import { Skeleton } from "@/components/ui/skeleton";
import { OrderStatus } from "@/types/order";

// ShippingService only creates a shipment reactively (consuming order.paid.v1), so a
// Pending/Cancelled/Refunded order will never have one — skip the fetch entirely for those
// rather than rendering a 404 as if it were meaningful.
const SHIPPABLE_STATUSES: readonly OrderStatus[] = ["Paid", "Shipped", "Delivered"];

export function ShipmentTracking({
  orderId,
  orderStatus,
  accessToken,
}: {
  orderId: string;
  orderStatus: OrderStatus;
  accessToken: string;
}) {
  const shouldFetch = SHIPPABLE_STATUSES.includes(orderStatus);

  const {
    data: shipment,
    isLoading,
    error,
  } = useQuery({
    queryKey: ["shipment", orderId],
    queryFn: () => getShipmentByOrder(accessToken, orderId),
    enabled: shouldFetch,
  });

  if (!shouldFetch) {
    return null;
  }

  return (
    <div className="flex flex-col gap-3 rounded-xl border border-border p-6">
      <h2 className="text-sm font-semibold">Shipping</h2>
      {isLoading ? (
        <Skeleton className="h-5 w-40" />
      ) : error instanceof ApiError && error.status === 404 ? (
        <p className="text-sm text-muted-foreground">Preparing your order for shipment.</p>
      ) : error || !shipment ? (
        <p className="text-sm text-muted-foreground">Couldn&apos;t load tracking info.</p>
      ) : (
        <dl className="flex flex-col gap-1.5 text-sm">
          <div className="flex justify-between">
            <dt className="text-muted-foreground">Status</dt>
            <dd>{shipment.status}</dd>
          </div>
          {shipment.carrierName && (
            <div className="flex justify-between">
              <dt className="text-muted-foreground">Carrier</dt>
              <dd>{shipment.carrierName}</dd>
            </div>
          )}
          {shipment.trackingNumber && (
            <div className="flex justify-between">
              <dt className="text-muted-foreground">Tracking #</dt>
              <dd>{shipment.trackingNumber}</dd>
            </div>
          )}
          {shipment.deliveredAt && (
            <div className="flex justify-between">
              <dt className="text-muted-foreground">Delivered</dt>
              <dd>{new Date(shipment.deliveredAt).toLocaleDateString()}</dd>
            </div>
          )}
          {shipment.exceptionReason && <p className="text-destructive">{shipment.exceptionReason}</p>}
        </dl>
      )}
    </div>
  );
}
