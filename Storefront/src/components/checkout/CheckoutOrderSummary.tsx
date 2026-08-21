import { ProductPrice } from "@/components/product/ProductPrice";
import { CartItemDto } from "@/types/cart";

export function CheckoutOrderSummary({ items, subtotal }: { items: CartItemDto[]; subtotal: number }) {
  return (
    <div className="flex flex-col gap-4 rounded-xl border border-border p-6">
      <h2 className="text-sm font-semibold">Order summary</h2>
      <ul className="flex flex-col gap-3">
        {items.map((item) => (
          <li key={item.productId} className="flex items-center justify-between gap-3 text-sm">
            <span className="text-muted-foreground">
              {item.name} × {item.quantity}
            </span>
            <ProductPrice price={item.lineTotal} className="font-normal" />
          </li>
        ))}
      </ul>
      <div className="flex items-center justify-between border-t border-border pt-4">
        <span className="text-sm font-medium">Subtotal</span>
        <ProductPrice price={subtotal} className="text-lg" />
      </div>
    </div>
  );
}
