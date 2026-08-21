"use client";

import { useState } from "react";
import { toast } from "sonner";
import { useCart } from "@/hooks/useCart";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";

export function AddToCartForm({
  productId,
  productName,
  available,
}: {
  productId: string;
  productName: string;
  available: number;
}) {
  const { addItem } = useCart();
  const [quantity, setQuantity] = useState(1);
  const outOfStock = available <= 0;

  async function handleAdd() {
    try {
      await addItem.mutateAsync({ productId, quantity });
      toast.success(`Added ${productName} to cart`);
      setQuantity(1);
    } catch {
      toast.error("Couldn't add that to your cart. Please try again.");
    }
  }

  return (
    <div className="flex items-center gap-3">
      <Input
        type="number"
        min={1}
        max={Math.max(available, 1)}
        value={quantity}
        disabled={outOfStock}
        onChange={(e) => setQuantity(Math.max(1, Number(e.target.value) || 1))}
        className="w-20"
        aria-label="Quantity"
      />
      <Button onClick={handleAdd} disabled={outOfStock || addItem.isPending}>
        {outOfStock ? "Out of stock" : addItem.isPending ? "Adding…" : "Add to cart"}
      </Button>
    </div>
  );
}
