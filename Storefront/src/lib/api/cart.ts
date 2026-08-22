import { apiFetch } from "@/lib/api/client";
import { CartDto } from "@/types/cart";

// Every function here is called directly from client components straight to the ApiGateway
// (not proxied through Storefront's own server, unlike auth) - that's what the gateway's new
// CORS policy exists for. These endpoints are `[AllowAnonymous]` (guest carts never authenticate
// at all), but CartService now also accepts an *optional* Bearer token on all of them to check
// ownership when the cart being touched belongs to a signed-in user (see CartsController's
// `GetCallerUserIdOrNull()`). So `accessToken` here is optional, not unused-for-these-routes:
// omit it for guest-cart calls, pass it whenever the caller is signed in, even though the route
// itself never requires it.
function authHeader(accessToken?: string): HeadersInit | undefined {
  return accessToken ? { Authorization: `Bearer ${accessToken}` } : undefined;
}

export async function createCart(): Promise<CartDto> {
  return apiFetch<CartDto>("/api/carts", { method: "POST" });
}

export async function getCart(id: string): Promise<CartDto> {
  return apiFetch<CartDto>(`/api/carts/${id}`);
}

export async function addItem(
  cartId: string,
  productId: string,
  quantity: number,
  accessToken?: string,
): Promise<CartDto> {
  return apiFetch<CartDto>(`/api/carts/${cartId}/items`, {
    method: "POST",
    headers: authHeader(accessToken),
    body: JSON.stringify({ productId, quantity }),
  });
}

export async function updateItemQuantity(
  cartId: string,
  productId: string,
  quantity: number,
  accessToken?: string,
): Promise<CartDto> {
  return apiFetch<CartDto>(`/api/carts/${cartId}/items/${productId}`, {
    method: "PUT",
    headers: authHeader(accessToken),
    body: JSON.stringify({ quantity }),
  });
}

export async function removeItem(cartId: string, productId: string, accessToken?: string): Promise<CartDto> {
  return apiFetch<CartDto>(`/api/carts/${cartId}/items/${productId}`, {
    method: "DELETE",
    headers: authHeader(accessToken),
  });
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
