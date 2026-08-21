// Mirrors InventoryService.Api's InventoryItemDto. One row per (productId, locationId) —
// there is no single "total available" field, so the storefront sums `available` itself.
// See lib/api/inventory.ts's sumAvailable().
export interface InventoryItemDto {
  id: string;
  productId: string;
  locationId: string;
  onHand: number;
  reserved: number;
  available: number;
  createdAt: string;
  updatedAt: string;
}
