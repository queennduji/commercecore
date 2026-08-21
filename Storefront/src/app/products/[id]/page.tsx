import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { ApiError } from "@/lib/api/client";
import { getInventory, sumAvailable } from "@/lib/api/inventory";
import { getProduct } from "@/lib/api/products";
import { AddToCartForm } from "@/components/cart/AddToCartForm";
import { ProductGallery } from "@/components/product/ProductGallery";
import { ProductPrice } from "@/components/product/ProductPrice";
import { StockBadge } from "@/components/product/StockBadge";

type ProductPageParams = { id: string };

async function loadProduct(id: string) {
  try {
    return await getProduct(id);
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) {
      notFound();
    }
    throw error;
  }
}

export async function generateMetadata({
  params,
}: {
  params: Promise<ProductPageParams>;
}): Promise<Metadata> {
  const { id } = await params;
  const product = await loadProduct(id);
  return { title: product.name };
}

export default async function ProductDetailPage({ params }: { params: Promise<ProductPageParams> }) {
  const { id } = await params;
  const [product, inventory] = await Promise.all([loadProduct(id), getInventory(id)]);
  const available = sumAvailable(inventory);

  return (
    <div className="mx-auto grid max-w-6xl gap-10 px-4 py-10 sm:px-6 lg:grid-cols-2">
      <ProductGallery images={product.images} alt={product.name} />

      <div className="flex flex-col gap-4">
        <h1 className="text-2xl font-semibold tracking-tight">{product.name}</h1>
        <p className="text-sm text-muted-foreground">SKU: {product.sku}</p>
        <div className="flex items-center gap-3">
          <ProductPrice price={product.price} className="text-2xl" />
          <StockBadge available={available} />
        </div>
        {product.description && (
          <p className="whitespace-pre-line text-sm leading-relaxed text-muted-foreground">
            {product.description}
          </p>
        )}
        <AddToCartForm productId={product.id} productName={product.name} available={available} />
      </div>
    </div>
  );
}
