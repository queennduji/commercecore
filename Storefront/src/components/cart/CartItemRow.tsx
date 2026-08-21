"use client";

import { toast } from "sonner";
import { useCart } from "@/hooks/useCart";
import { Button } from "@/components/ui/button";
import { ProductPrice } from "@/components/product/ProductPrice";
import { CartItemDto } from "@/types/cart";

export function CartItemRow({ item }: { item: CartItemDto }) {
  const { updateQuantity, removeItem } = useCart();

  async function handleQuantityChange(nextQuantity: number) {
    try {
      await updateQuantity.mutateAsync({ productId: item.productId, quantity: nextQuantity });
    } catch {
      toast.error("Couldn't update that item.");
    }
  }

  async function handleRemove() {
    try {
      await removeItem.mutateAsync(item.productId);
    } catch {
      toast.error("Couldn't remove that item.");
    }
  }

  const isBusy = updateQuantity.isPending || removeItem.isPending;

  return (
    // flex-col below sm, not one cramped row at every width - four columns (name/sku/price,
    // quantity, line total, remove) squeezed into a ~370px phone width left the name column at
    // ~79px, wrapping "Ceramic Dinnerware Set" onto several lines (confirmed directly at a 375px
    // viewport). Name/price get their own full-width row; quantity/total/remove share a second
    // row, spread out instead of squeezed together.
    <div className="flex flex-col gap-3 border-b border-border py-4 last:border-b-0 sm:flex-row sm:items-center sm:gap-4">
      <div className="sm:min-w-0 sm:flex-1">
        <p className="text-sm font-medium">{item.name}</p>
        <p className="text-xs text-muted-foreground">SKU: {item.sku}</p>
        <ProductPrice price={item.unitPrice} className="text-sm font-normal text-muted-foreground" />
      </div>

      <div className="flex items-center justify-between gap-3 sm:justify-end sm:gap-4">
        <div className="flex items-center gap-2">
          <Button
            variant="outline"
            size="icon-sm"
            disabled={isBusy}
            onClick={() => handleQuantityChange(item.quantity - 1)}
            aria-label="Decrease quantity"
          >
            −
          </Button>
          <span className="w-6 text-center text-sm tabular-nums">{item.quantity}</span>
          <Button
            variant="outline"
            size="icon-sm"
            disabled={isBusy}
            onClick={() => handleQuantityChange(item.quantity + 1)}
            aria-label="Increase quantity"
          >
            +
          </Button>
        </div>

        <ProductPrice price={item.lineTotal} className="w-16 text-right sm:w-20" />

        <Button variant="ghost" size="sm" disabled={isBusy} onClick={handleRemove}>
          Remove
        </Button>
      </div>
    </div>
  );
}
