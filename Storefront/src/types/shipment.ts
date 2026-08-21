// Mirrors ShippingService.Application's ShipmentDto. status is a string, same as every other
// service's DTO in this platform.
export type ShipmentStatus = "AwaitingFulfillment" | "Dispatched" | "InTransit" | "Delivered" | "Exception";

export interface ShipmentDto {
  id: string;
  orderId: string;
  userId: string;
  shippingAddress: string;
  status: ShipmentStatus;
  carrierName?: string | null;
  trackingNumber?: string | null;
  exceptionReason?: string | null;
  createdAt: string;
  updatedAt: string;
  dispatchedAt?: string | null;
  deliveredAt?: string | null;
}
