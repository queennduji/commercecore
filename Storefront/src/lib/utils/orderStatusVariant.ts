import { OrderStatus } from "@/types/order";

/** Shared between the order list and order detail pages so a status always reads the same way. */
export const ORDER_STATUS_VARIANT: Record<OrderStatus, "default" | "secondary" | "destructive" | "outline"> = {
  Pending: "outline",
  Paid: "secondary",
  Shipped: "secondary",
  Delivered: "default",
  Cancelled: "destructive",
  Refunded: "destructive",
};
