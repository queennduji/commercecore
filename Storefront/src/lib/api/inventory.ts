import { apiFetch } from "@/lib/api/client";
import { InventoryItemDto } from "@/types/inventory";

// Stock is the figure most likely to go stale in a way that annoys a real shopper
// (showing "in stock" for something that just sold out elsewhere), so it gets a much
// shorter revalidate window than product/category data.
const INVENTORY_REVALIDATE_SECONDS = 10;

/** Per-location stock rows for a product — InventoryService returns no aggregate field. */
export async function getInventory(productId: string): Promise<InventoryItemDto[]> {
  return apiFetch<InventoryItemDto[]>(`/api/inventory/${productId}`, {
    next: { revalidate: INVENTORY_REVALIDATE_SECONDS },
  });
}

/** Sums `available` across every location — the storefront's own "in stock" check. */
export function sumAvailable(items: InventoryItemDto[]): number {
  return items.reduce((total, item) => total + item.available, 0);
}
