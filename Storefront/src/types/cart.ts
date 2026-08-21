// Mirrors CartService.Api's CartDto/CartItemDto JSON shapes.
export interface CartItemDto {
  productId: string;
  sku: string;
  name: string;
  unitPrice: number;
  quantity: number;
  lineTotal: number;
}

export interface CartDto {
  id: string;
  userId?: string | null;
  items: CartItemDto[];
  subtotal: number;
  createdAt: string;
  updatedAt: string;
}
