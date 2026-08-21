// Mirrors OrderService.Application's OrderDto/OrderItemDto JSON shapes. Verified directly against
// OrderDto.cs (not assumed) — status is a `string`, unlike ProductDto.status which is also a
// string despite looking like it should be numeric; the two services just happen to agree here.
export type OrderStatus = "Pending" | "Paid" | "Shipped" | "Delivered" | "Cancelled" | "Refunded";

export interface OrderItemDto {
  productId: string;
  sku: string;
  name: string;
  unitPrice: number;
  quantity: number;
  lineTotal: number;
  locationId: string;
}

export interface OrderDto {
  id: string;
  userId: string;
  status: OrderStatus;
  shippingAddress: string;
  items: OrderItemDto[];
  subtotal: number;
  createdAt: string;
  updatedAt: string;
}
