import { apiFetch } from "@/lib/api/client";
import { ShipmentDto } from "@/types/shipment";

/**
 * 404s if no shipment exists yet for this order - ShippingService only creates one reactively,
 * consuming order.paid.v1, so a Pending/just-Paid order legitimately has none. Callers should
 * only invoke this once an order's own status is Paid/Shipped/Delivered, and should treat a 404
 * as "not shipped yet", not an error.
 */
export async function getShipmentByOrder(accessToken: string, orderId: string): Promise<ShipmentDto> {
  return apiFetch<ShipmentDto>(`/api/shipments/order/${orderId}`, {
    headers: { Authorization: `Bearer ${accessToken}` },
  });
}
