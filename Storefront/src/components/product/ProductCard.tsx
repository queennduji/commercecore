import Image from "next/image";
import Link from "next/link";
import { Card, CardContent } from "@/components/ui/card";
import { ProductImagePlaceholder } from "@/components/product/ProductImagePlaceholder";
import { ProductPrice } from "@/components/product/ProductPrice";
import { StockBadge } from "@/components/product/StockBadge";
import { ProductDto } from "@/types/catalog";

function primaryImage(product: ProductDto) {
  return (
    product.images.find((image) => image.isPrimary) ??
    [...product.images].sort((a, b) => a.sortOrder - b.sortOrder)[0]
  );
}

export function ProductCard({ product, available }: { product: ProductDto; available: number }) {
  const image = primaryImage(product);

  return (
    <Link href={`/products/${product.id}`} className="group">
      <Card className="h-full transition-shadow group-hover:shadow-md">
        <div className="relative aspect-square w-full overflow-hidden bg-muted">
          {image ? (
            <Image
              src={image.url}
              alt={product.name}
              fill
              sizes="(min-width: 1024px) 25vw, (min-width: 640px) 33vw, 50vw"
              className="object-cover transition-transform group-hover:scale-[1.02]"
            />
          ) : (
            <ProductImagePlaceholder />
          )}
        </div>
        <CardContent className="flex flex-col gap-2">
          <h3 className="line-clamp-2 text-sm font-medium">{product.name}</h3>
          <div className="flex items-center justify-between gap-2">
            <ProductPrice price={product.price} />
            <StockBadge available={available} />
          </div>
        </CardContent>
      </Card>
    </Link>
  );
}
