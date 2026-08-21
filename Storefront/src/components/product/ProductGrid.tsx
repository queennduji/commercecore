import { ProductCard } from "@/components/product/ProductCard";
import { ProductDto } from "@/types/catalog";

export interface ProductGridItem {
  product: ProductDto;
  available: number;
}

export function ProductGrid({ items }: { items: ProductGridItem[] }) {
  if (items.length === 0) {
    return <p className="py-12 text-center text-sm text-muted-foreground">No products found.</p>;
  }

  return (
    <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-4">
      {items.map(({ product, available }) => (
        <ProductCard key={product.id} product={product} available={available} />
      ))}
    </div>
  );
}
