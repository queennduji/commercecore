import Link from "next/link";
import { Button } from "@/components/ui/button";
import { ProductPrice } from "@/components/product/ProductPrice";

export function CartSummary({ subtotal }: { subtotal: number }) {
  return (
    <div className="flex flex-col gap-4 rounded-xl border border-border p-6">
      <div className="flex items-center justify-between">
        <span className="text-sm text-muted-foreground">Subtotal</span>
        <ProductPrice price={subtotal} className="text-lg" />
      </div>
      {/* nativeButton=false: the render target is a <Link> (an <a>), not a real <button> - Base
          UI's Button defaults to assuming a native button element and warns otherwise. */}
      <Button className="w-full" nativeButton={false} render={<Link href="/checkout" />}>
        Checkout
      </Button>
    </div>
  );
}
