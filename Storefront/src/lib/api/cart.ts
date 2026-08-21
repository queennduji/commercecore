import { apiFetch } from "@/lib/api/client";
import { CartDto } from "@/types/cart";

// Every function here is called directly from client components straight to the ApiGateway
// (not proxied through Storefront's own server, unlike auth) — that's what the gateway's new
// CORS policy exists for. Cart reads/writes are anonymous by design (guest carts); `accessToken`
// is only passed for the two `[Authorize]`-gated /me routes.
function authHeader(accessToken?: string): HeadersInit | undefined {
  return accessToken ? { Authorization: `Bearer ${accessToken}` } : undefined;
}

export async function createCart(): Promise<CartDto> {
  return apiFetch<CartDto>("/api/carts", { method: "POST" });
}

export async function getCart(id: string): Promise<CartDto> {
  return apiFetch<CartDto>(`/api/carts/${id}`);
}

export async function addItem(cartId: string, productId: string, quantity: number): Promise<CartDto> {
  return apiFetch<CartDto>(`/api/carts/${cartId}/items`, {
    method: "POST",
    body: JSON.stringify({ productId, quantity }),
  });
}

export async function updateItemQuantity(cartId: string, productId: string, quantity: number): Promise<CartDto> {
  return apiFetch<CartDto>(`/api/carts/${cartId}/items/${productId}`, {
    method: "PUT",
    body: JSON.stringify({ quantity }),
  });
}

export async function removeItem(cartId: string, productId: string): Promise<CartDto> {
  return apiFetch<CartDto>(`/api/carts/${cartId}/items/${productId}`, { method: "DELETE" });
}

export async function getMyCart(accessToken: string): Promise<CartDto> {
  return apiFetch<CartDto>("/api/carts/me", { headers: authHeader(accessToken) });
}

export async function mergeCart(accessToken: string, sourceCartId: string): Promise<CartDto> {
  return apiFetch<CartDto>("/api/carts/me/merge", {
    method: "POST",
    headers: authHeader(accessToken),
    body: JSON.stringify({ sourceCartId }),
  });
}
