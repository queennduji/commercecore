import { apiFetch, toQueryString } from "@/lib/api/client";
import { PagedResult } from "@/types/catalog";
import { OrderDto } from "@/types/order";

// Every call here requires auth (OrderService's whole controller is [Authorize]) and goes
// directly from the client to the ApiGateway, same pattern as lib/api/cart.ts.
function authHeader(accessToken: string): HeadersInit {
  return { Authorization: `Bearer ${accessToken}` };
}

export async function checkout(accessToken: string, shippingAddress: string): Promise<OrderDto> {
  return apiFetch<OrderDto>("/api/orders/checkout", {
    method: "POST",
    headers: authHeader(accessToken),
    body: JSON.stringify({ shippingAddress }),
  });
}

export async function getOrder(accessToken: string, id: string): Promise<OrderDto> {
  return apiFetch<OrderDto>(`/api/orders/${id}`, { headers: authHeader(accessToken) });
}

export async function payOrder(accessToken: string, id: string, paymentMethodId: string): Promise<OrderDto> {
  return apiFetch<OrderDto>(`/api/orders/${id}/pay`, {
    method: "POST",
    headers: authHeader(accessToken),
    body: JSON.stringify({ paymentMethodId }),
  });
}

export async function cancelOrder(accessToken: string, id: string): Promise<OrderDto> {
  return apiFetch<OrderDto>(`/api/orders/${id}/cancel`, {
    method: "POST",
    headers: authHeader(accessToken),
  });
}

export async function listMyOrders(
  accessToken: string,
  page = 1,
  pageSize = 10,
): Promise<PagedResult<OrderDto>> {
  const qs = toQueryString({ page, pageSize });
  return apiFetch<PagedResult<OrderDto>>(`/api/orders/me${qs}`, { headers: authHeader(accessToken) });
}
